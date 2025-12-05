namespace NoveoNative;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // LOAD SAVED DARK MODE ON STARTUP
        bool isDarkMode = SettingsManager.LoadDarkMode();

        if (isDarkMode)
        {
            UserAppTheme = AppTheme.Dark;
        }
        else
        {
            UserAppTheme = AppTheme.Light;
        }

        MainPage = new NavigationPage(new ChatListPage());
    }
}
