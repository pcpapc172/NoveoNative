using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoveoNative;

public partial class ChatListPage : ContentPage, INotifyPropertyChanged
{
    public static NoveoClient Client = new NoveoClient();
    public ObservableCollection<ChatViewModel> Chats { get; set; } = new ObservableCollection<ChatViewModel>();
    public bool IsDesktop => DeviceInfo.Idiom == DeviceIdiom.Desktop;
    public GridLength ListColumnWidth => IsDesktop ? new GridLength(300) : GridLength.Star;
    public GridLength ChatColumnWidth => IsDesktop ? GridLength.Star : new GridLength(0);
    public Color MainBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#111827") : Colors.White;
    public Color TextColor => SettingsManager.IsDarkMode ? Colors.White : Colors.Black;
    public Color BorderColor => SettingsManager.IsDarkMode ? Color.FromArgb("#374151") : Color.FromArgb("#e5e7eb");

    private bool _isListEmpty = true;
    public bool IsListEmpty { get => _isListEmpty; set { _isListEmpty = value; OnPropertyChanged(); } }

    private bool _isLoginVisible = true;
    public bool IsLoginVisible { get => _isLoginVisible; set { _isLoginVisible = value; OnPropertyChanged(); } }

    private bool _isRegisterMode = false;
    public string LoginBtnText => _isRegisterMode ? "Register" : "Login";
    public string ToggleBtnText => _isRegisterMode ? "Already have an account? Login" : "Don't have an account? Register";

    private bool _isMenuOpen = false;

    public ChatListPage()
    {
        InitializeComponent();
        BindingContext = this;
        MessageViewModel.IsDarkMode = SettingsManager.IsDarkMode;

        var session = SettingsManager.GetSession();
        if (!string.IsNullOrEmpty(session.Token)) IsLoginVisible = false;

        ChatListCollectionView.ItemsSource = Chats;

        Client.OnLog += (msg) => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = msg);
        Client.OnLoginSuccess += () => MainThread.BeginInvokeOnMainThread(() => { IsLoginVisible = false; LoadChats(); });
        Client.OnLoginFailed += () => MainThread.BeginInvokeOnMainThread(() => { StatusLabel.Text = "Auth failed."; SettingsManager.ClearSession(); IsLoginVisible = true; });
        Client.OnChatListUpdated += LoadChats;
        Client.OnNewChat += (chat) => LoadChats();
        Client.OnChannelInfo += (chat) => LoadChats();

        if (!IsLoginVisible) CheckAutoLogin();
    }

    private async void CheckAutoLogin()
    {
        var session = SettingsManager.GetSession();
        if (!string.IsNullOrEmpty(session.Token)) { StatusLabel.Text = "Welcome back..."; await Client.Reconnect(session.UserId, session.Token); }
    }

    private void LoadChats()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsListEmpty = false;
            var newItems = new List<ChatViewModel>();
            var existingChatIds = new HashSet<string>();

            // 1. Add Active Chats (History exists)
            foreach (var c in Client.AllChats)
            {
                string name = c.ChatName ?? "";
                string avatarUrl = Client.GetFullUrl(c.AvatarUrl);
                string otherId = "";
                bool isOnline = false;

                if (c.ChatType == "private" && c.Members != null)
                {
                    otherId = c.Members.FirstOrDefault(m => m != Client.CurrentUserId) ?? "";
                    if (!string.IsNullOrEmpty(otherId))
                    {
                        name = Client.GetUserName(otherId);
                        avatarUrl = Client.GetUserAvatar(otherId);
                        if (Client.Users.ContainsKey(otherId)) isOnline = Client.Users[otherId].IsOnline;
                    }
                }

                if (string.IsNullOrEmpty(name) && c.ChatType == "channel") name = "Channel";
                if (string.IsNullOrEmpty(name)) name = "Unknown Chat";

                string preview = "No messages";
                if (c.Messages != null && c.Messages.Count > 0)
                {
                    var lastMsg = c.Messages.Last();
                    var parsed = Client.ParseMessageContent(lastMsg.Content);
                    if (parsed.IsTheme) preview = "🎨 Theme";
                    else if (parsed.IsFile) preview = "📎 File";
                    else preview = parsed.Text;
                }

                existingChatIds.Add(c.ChatId);
                newItems.Add(new ChatViewModel
                {
                    ChatId = c.ChatId,
                    DisplayName = name,
                    AvatarUrl = avatarUrl,
                    AvatarLetter = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper(),
                    LastMessagePreview = preview,
                    IsChannel = c.ChatType == "channel",
                    IsPrivate = c.ChatType == "private",
                    OtherUserId = otherId,
                    IsOnline = isOnline
                });
            }

            // 2. Add Missing Users (DMs with no history)
            foreach (var user in Client.Users.Values)
            {
                if (user.UserId == Client.CurrentUserId) continue;

                var ids = new List<string> { Client.CurrentUserId, user.UserId };
                ids.Sort();
                string expectedId = string.Join("_", ids);

                if (!existingChatIds.Contains(expectedId))
                {
                    newItems.Add(new ChatViewModel
                    {
                        ChatId = expectedId,
                        DisplayName = user.Username,
                        AvatarUrl = Client.GetFullUrl(user.AvatarUrl),
                        AvatarLetter = user.Username.Substring(0, 1).ToUpper(),
                        LastMessagePreview = user.IsOnline ? "Online" : "Offline",
                        IsPrivate = true,
                        OtherUserId = user.UserId,
                        IsOnline = user.IsOnline
                    });
                }
            }

            var sorted = newItems.OrderByDescending(x => x.LastMessagePreview != "Online" && x.LastMessagePreview != "Offline")
                                 .ThenByDescending(x => x.IsOnline)
                                 .ToList();

            Chats.Clear();
            foreach (var item in sorted) Chats.Add(item);
            if (Chats.Count == 0) IsListEmpty = true;
        });
    }

    private void OnToggleLoginMode(object sender, EventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        OnPropertyChanged(nameof(LoginBtnText));
        OnPropertyChanged(nameof(ToggleBtnText));
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(UserEntry.Text) || string.IsNullOrEmpty(PassEntry.Text)) return;
        StatusLabel.Text = "Connecting...";
        if (_isRegisterMode) await Client.ConnectAndRegister(UserEntry.Text, PassEntry.Text);
        else await Client.ConnectAndLogin(UserEntry.Text, PassEntry.Text);
    }

    private void OnToggleTheme(object sender, EventArgs e)
    {
        SettingsManager.IsDarkMode = !SettingsManager.IsDarkMode;

        // Set app-wide theme
        if (SettingsManager.IsDarkMode)
            Application.Current!.UserAppTheme = AppTheme.Dark;
        else
            Application.Current!.UserAppTheme = AppTheme.Light;

        MessageViewModel.IsDarkMode = SettingsManager.IsDarkMode;
        OnPropertyChanged(nameof(MainBgColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(BorderColor));

        foreach (var chat in Chats) chat.RefreshColor();
        if (IsDesktop) DesktopChatView.RefreshColors();

        // Close menu after toggling
        if (_isMenuOpen)
        {
            _isMenuOpen = false;
            MenuBackdrop.IsVisible = false;
            MenuDrawer.IsVisible = false;
        }
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        _isMenuOpen = !_isMenuOpen;

        if (_isMenuOpen)
        {
            MenuBackdrop.IsVisible = true;
            MenuDrawer.IsVisible = true;
            MenuDrawer.TranslationX = 0;
            await MenuDrawer.TranslateTo(0, 0, 300, Easing.CubicOut);
        }
        else
        {
            await MenuDrawer.TranslateTo(-250, 0, 300, Easing.CubicOut);
            MenuBackdrop.IsVisible = false;
            MenuDrawer.IsVisible = false;
        }
    }

    private async void OnMenuBackdropTapped(object sender, TappedEventArgs e)
    {
        if (_isMenuOpen)
        {
            _isMenuOpen = false;
            await MenuDrawer.TranslateTo(-250, 0, 300, Easing.CubicOut);
            MenuBackdrop.IsVisible = false;
            MenuDrawer.IsVisible = false;
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        // Close menu
        if (_isMenuOpen)
        {
            _isMenuOpen = false;
            await MenuDrawer.TranslateTo(-250, 0, 300, Easing.CubicOut);
            MenuBackdrop.IsVisible = false;
            MenuDrawer.IsVisible = false;
        }

        await Navigation.PushModalAsync(new SettingsPage());
    }

    private void OnCreateChannel(object sender, EventArgs e)
    {
        Navigation.PushModalAsync(new CreateChannelPage());
    }

    private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ChatViewModel selectedChat)
        {
            string recipientId = selectedChat.IsPrivate ? selectedChat.OtherUserId : null;

            if (IsDesktop) DesktopChatView.LoadChat(selectedChat.ChatId, recipientId);
            else await Navigation.PushModalAsync(new MobileChatPage(selectedChat.ChatId, selectedChat.DisplayName, recipientId));

            ChatListCollectionView.SelectedItem = null;
        }
    }

    private async void OnLogout(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            SettingsManager.ClearSession();
            Application.Current!.MainPage = new ChatListPage();
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected new void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
