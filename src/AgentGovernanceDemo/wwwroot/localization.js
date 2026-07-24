window.agentGovernanceLocalization = {
    setCulture: function (culture) {
        const returnUrl = window.location.pathname + window.location.search;
        window.location.assign(
            "/culture/set?culture="
            + encodeURIComponent(culture)
            + "&returnUrl="
            + encodeURIComponent(returnUrl));
    }
};
