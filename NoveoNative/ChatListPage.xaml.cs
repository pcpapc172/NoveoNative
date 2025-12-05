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

    private bool _isListEmpty = true;
    public bool IsListEmpty { get => _isListEmpty; set { _isListEmpty = value; OnPropertyChanged(); } }

    private bool _isLoginVisible = true;
    public bool IsLoginVisible { get => _isLoginVisible; set { _isLoginVisible = value; OnPropertyChanged(); } }

    private bool _isRegisterMode = false;
    public string LoginBtnText => _isRegisterMode ? "Register" : "Login";
    public string ToggleBtnText => _isRegisterMode ? "Already have an account? Login" : "Don't have an account? Register";

    public ChatListPage()
    {
        InitializeComponent();
        BindingContext = this;

        // LOAD SAVED DARK MODE
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

        // FIX: Refresh colors on startup for desktop
        Loaded += (s, e) =>
        {
            if (IsDesktop)
            {
                DesktopChatView.RefreshColors();
            }
        };
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
        SettingsManager.SaveDarkMode(SettingsManager.IsDarkMode);
        MessageViewModel.IsDarkMode = SettingsManager.IsDarkMode;

        if (SettingsManager.IsDarkMode)
        {
            Application.Current!.UserAppTheme = AppTheme.Dark;
        }
        else
        {
            Application.Current!.UserAppTheme = AppTheme.Light;
        }

        OnPropertyChanged(nameof(MainBgColor));
        OnPropertyChanged(nameof(TextColor));
        foreach (var chat in Chats) chat.RefreshColor();
        if (IsDesktop) DesktopChatView.RefreshColors();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        // Close menu first
        MenuDrawer.IsVisible = false;
        MenuBackdrop.IsVisible = false;

        await Navigation.PushAsync(new SettingsPage());
    }

    private void OnCreateChannel(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CreateChannelPage());
    }

    private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ChatViewModel selectedChat)
        {
            string recipientId = selectedChat.IsPrivate ? selectedChat.OtherUserId : null;

            System.Diagnostics.Debug.WriteLine($"Opening chat: {selectedChat.ChatId}, Recipient: {recipientId}, Name: {selectedChat.DisplayName}");

            if (IsDesktop)
            {
                DesktopChatView.LoadChat(selectedChat.ChatId, recipientId);
            }
            else
            {
                await Navigation.PushAsync(new MobileChatPage(selectedChat.ChatId, selectedChat.DisplayName, recipientId));
            }

            ChatListCollectionView.SelectedItem = null;
        }
    }

    // HAMBURGER MENU METHODS
    private void OnMenuClicked(object sender, EventArgs e)
    {
        // Toggle menu visibility
        bool isOpen = MenuDrawer.IsVisible;
        MenuDrawer.IsVisible = !isOpen;
        MenuBackdrop.IsVisible = !isOpen;
    }

    private void OnMenuBackdropTapped(object sender, EventArgs e)
    {
        // Close menu when backdrop is tapped
        MenuDrawer.IsVisible = false;
        MenuBackdrop.IsVisible = false;
    }

    private void OnLogout(object sender, EventArgs e)
    {
        SettingsManager.ClearSession();
        Application.Current!.MainPage = new NavigationPage(new ChatListPage());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
