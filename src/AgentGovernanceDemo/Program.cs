// EN: Composes the web host, validated options, governance services, audit pipeline, security middleware, and Blazor endpoints.
// JA: Web ホスト、検証済み設定、ガバナンスサービス、監査パイプライン、セキュリティミドルウェア、Blazor エンドポイントを構成します。

using System.Globalization;
using AgentGovernanceDemo.Audit;
using AgentGovernanceDemo.Components;
using AgentGovernanceDemo.Configuration;
using AgentGovernanceDemo.Governance;
using AgentGovernanceDemo.Integration;
using AgentGovernanceDemo.Localization;
using AgentGovernanceDemo.Telemetry;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// EN: Register presentation, configuration, governance, storage, and observability dependencies before the host is built.
// JA: ホストをビルドする前に、表示、構成、ガバナンス、ストレージ、可観測性の依存関係を登録します。
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<UiLocalizer>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = UiLocalizer.SupportedCultures
        .Select(CultureInfo.GetCultureInfo)
        .ToArray();
    options.DefaultRequestCulture = new RequestCulture(UiLocalizer.DefaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider()
    ];
});
builder.Services.AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection(DemoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.AccountUri, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps,
        "Storage:AccountUri must be an absolute HTTPS URI.")
    .Validate(
        options => options.AuditContainerName.Length is >= 3 and <= 63
            && options.AuditContainerName.All(
                character => char.IsAsciiLetterOrDigit(character) || character == '-')
            && options.AuditContainerName == options.AuditContainerName.ToLowerInvariant()
            && !options.AuditContainerName.StartsWith('-')
            && !options.AuditContainerName.EndsWith('-')
            && !options.AuditContainerName.Contains("--", StringComparison.Ordinal),
        "Storage:AuditContainerName must be a valid lowercase Azure Blob container name.")
    .ValidateOnStart();
builder.Services.AddAgentGovernanceDemoTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<TokenCredential>(_ =>
{
    if (builder.Environment.IsDevelopment())
    {
        return new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true
            });
    }

    var managedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"];
    return string.IsNullOrWhiteSpace(managedIdentityClientId)
        ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
        : new ManagedIdentityCredential(
            ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
});
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
    return new BlobAuditOptions
    {
        AccountUri = new Uri(options.AccountUri, UriKind.Absolute),
        ContainerName = options.AuditContainerName
    };
});
builder.Services.AddSingleton<IAuditBlobClient, AzureAppendBlobAuditClient>();
builder.Services.AddSingleton<IAuditSanitizer, AuditSanitizer>();
builder.Services.AddSingleton<PersistedAuditEventHub>();
builder.Services.AddSingleton<IPersistedAuditEventHub>(
    serviceProvider => serviceProvider.GetRequiredService<PersistedAuditEventHub>());
builder.Services.AddSingleton<BlobAuditSink>();
builder.Services.AddSingleton<BlobAuditReader>();
builder.Services.AddSingleton<StorageHealthMonitor>();
builder.Services.AddSingleton(
    _ => new GovernanceDemoService(
        Path.Combine(builder.Environment.ContentRootPath, "policies", "default.yaml")));
builder.Services.AddSingleton<IDemoToolExecutor, DeterministicDemoToolExecutor>();
builder.Services.AddSingleton<DemoExecutionEventHub>();
builder.Services.AddSingleton<DemoRunRateLimiter>();
builder.Services.AddSingleton<IGovernanceDemoEventSink>(
    serviceProvider => serviceProvider.GetRequiredService<DemoExecutionEventHub>());
builder.Services.AddSingleton<DemoRunCoordinator>();
builder.Services.AddHostedService<GovernanceAuditPersistenceWorker>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// EN: Production-only exception handling and HSTS avoid leaking diagnostics while enforcing HTTPS on subsequent requests.
// JA: 本番専用の例外処理と HSTS により、診断情報の漏えいを防ぎつつ後続要求で HTTPS を強制します。
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRequestLocalization();
// EN: Add defense-in-depth response headers before serving static files or interactive components.
// JA: 静的ファイルや対話型コンポーネントを提供する前に、多層防御のレスポンスヘッダーを追加します。
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; connect-src 'self' wss: ws:; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'";
    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/culture/set", (
    string culture,
    string? returnUrl,
    HttpContext context) =>
{
    var selectedCulture = UiLocalizer.SupportedCultures.FirstOrDefault(
        item => string.Equals(item, culture, StringComparison.OrdinalIgnoreCase))
        ?? UiLocalizer.DefaultCulture;
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(selectedCulture)),
        new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps
        });

    var localReturnUrl = !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith("/", StringComparison.Ordinal)
        && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? returnUrl
            : "/";
    return Results.LocalRedirect(localReturnUrl);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
