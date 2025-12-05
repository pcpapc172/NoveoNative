namespace NoveoNative;

public static class SettingsManager
{
    // Save Credentials
    public static void SaveSession(string userId, string token, string username)
    {
        Preferences.Set("auth_userid", userId);
        Preferences.Set("auth_token", token);
        Preferences.Set("auth_username", username);
    }

    // Get Credentials (returns tuple)
    public static (string UserId, string Token, string Username) GetSession()
    {
        return (
            Preferences.Get("auth_userid", string.Empty),
            Preferences.Get("auth_token", string.Empty),
            Preferences.Get("auth_username", string.Empty)
        );
    }

    public static void ClearSession()
    {
        Preferences.Remove("auth_userid");
        Preferences.Remove("auth_token");
        Preferences.Remove("auth_username");
    }

    // Theme Settings - NOW PERSISTS
    public static bool IsDarkMode
    {
        get => Preferences.Get("is_dark_mode", false);
        set => Preferences.Set("is_dark_mode", value);
    }

    // Save dark mode explicitly
    public static void SaveDarkMode(bool isDarkMode)
    {
        Preferences.Set("is_dark_mode", isDarkMode);
    }

    // Load dark mode on startup
    public static bool LoadDarkMode()
    {
        return Preferences.Get("is_dark_mode", false);
    }
}
