namespace FaceRacerLive
{
    public static class SpeakHelper
    {
        private static Locale _locale;

        /// <summary>
        /// Initializes the locale settings asynchronously using the specified locale identifier.
        /// </summary>
        /// <param name="locale">The locale identifier to use for initialization. This value determines the language and regional settings to be applied. Cannot be null or empty.</param>
        public static async Task Initialize(string locale)
        {
            _locale = await GetLocale(locale);
        }

        public static async Task Speak(string text)
        {
            var options = new SpeechOptions { Volume = 1.0f, Locale = _locale };

            await TextToSpeech.Default.SpeakAsync(text, options);
        }

        private static async Task<Locale> GetLocale(string locale)
        {
            // get all available locales
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            // try to find Spanish (Mexico)
            var localeCountry = locales.FirstOrDefault(l => l.Language.Equals(locale, StringComparison.OrdinalIgnoreCase));

            // fallback if not found
            var selectedLocale = localeCountry ?? locales.FirstOrDefault(l => l.Language.StartsWith(locale.Split("-")[0], StringComparison.OrdinalIgnoreCase));

            return selectedLocale;
        }
    }
}
