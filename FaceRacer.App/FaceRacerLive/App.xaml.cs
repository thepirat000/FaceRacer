using Microsoft.Extensions.DependencyInjection;

namespace FaceRacerLive
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Force dark theme for the app
            Application.Current!.UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}