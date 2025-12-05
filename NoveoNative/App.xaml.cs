namespace NoveoNative
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set theme on startup
            if (SettingsManager.IsDarkMode)
                Application.Current!.UserAppTheme = AppTheme.Dark;
            else
                Application.Current!.UserAppTheme = AppTheme.Light;

            MainPage = new ChatListPage();
        }
    }
}
