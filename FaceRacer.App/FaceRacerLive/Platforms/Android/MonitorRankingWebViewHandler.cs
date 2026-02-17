#if ANDROID
using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace FaceRacerLive.Platforms.Android;

internal static class MonitorRankingWebViewHandler
{
    public static void EnableZoom()
    {
        WebViewHandler.Mapper.AppendToMapping("Zoom", (handler, view) =>
        {
            if (handler.PlatformView is global::Android.Webkit.WebView native)
            {
                native.Settings.JavaScriptEnabled = true;
                native.Settings.BuiltInZoomControls = true;
                native.Settings.DisplayZoomControls = false;
                native.Settings.SetSupportZoom(true);
            }
        });
    }
}
#endif
