using ScottPlot.Palettes;

using System;
using System.Collections.Generic;
using System.Text;

namespace FaceRacer.Settings
{
    public class AppSettings
    {
        public string TelegramBotToken { get; set; }
        public long TelegramChatId { get; set; }
        public int TrackId { get; set; } = 1417;
        public int KartId { get; set; } = 282;


        // Not in appsettings.json, set by arguments or environment variables
        public bool BotDisabled { get; set; }
        public bool RunOnce { get; set; }
        public int Pause { get; set; } = 60;

        // Max position to notify about (1-100)
        public int TelegramMaxPosition { get; set; } = 10; 

        public string SessionUrl { get; set; }

        internal string GetSessionUrl(string username, string sessionId)
        {
            return SessionUrl == null ? null : string.Format(SessionUrl, username, sessionId);
        }
    }
}
