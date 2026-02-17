namespace FaceRacerLive;

public partial class MonitorRankingPage : ContentPage
{
    public MonitorRankingPage()
    {
        InitializeComponent();

        var url = (AppSettings.MonitorRankingUrl ?? string.Empty).Trim();
        RankingWebView.Source = string.IsNullOrWhiteSpace(url)
            ? "about:blank"
            : new UrlWebViewSource { Url = url };
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        try
        {
            RankingWebView.Reload();
        }
        catch
        {
            // Best-effort fallback: re-assign current URL source.
            if (RankingWebView.Source is UrlWebViewSource { Url: { Length: > 0 } url })
            {
                RankingWebView.Source = new UrlWebViewSource { Url = url };
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var url = (AppSettings.MonitorRankingUrl ?? string.Empty).Trim();
        RankingWebView.Source = string.IsNullOrWhiteSpace(url)
            ? "about:blank"
            : new UrlWebViewSource { Url = url };
    }
}
