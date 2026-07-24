using AgentGovernanceDemo.Localization;

namespace AgentGovernanceDemo.Tests;

public sealed class UiLocalizerTests
{
    [Fact]
    public void Supported_cultures_translate_shared_navigation_text()
    {
        var localizer = new UiLocalizer();
        var expected = new Dictionary<string, string>
        {
            ["en-US"] = "Language",
            ["ja-JP"] = "言語",
            ["zh-TW"] = "語言",
            ["zh-CN"] = "语言",
            ["zh-HK"] = "語言",
            ["ko-KR"] = "언어"
        };

        foreach (var culture in UiLocalizer.SupportedCultures)
        {
            Assert.True(localizer.SetCulture(culture) || localizer.Culture == culture);
            Assert.Equal(culture, localizer.Culture);
            Assert.Equal(expected[culture], localizer["Language"]);
        }
    }

    [Fact]
    public void Unsupported_culture_does_not_replace_the_current_selection()
    {
        var localizer = new UiLocalizer();
        localizer.SetCulture("ko-KR");

        Assert.False(localizer.SetCulture("fr-FR"));
        Assert.Equal("ko-KR", localizer.Culture);
    }
}
