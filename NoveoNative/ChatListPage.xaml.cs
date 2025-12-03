using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoveoNative
{
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

        // Login Logic
        private bool _isLoginVisible = true;
        public bool IsLoginVisible { get => _isLoginVisible; set { _isLoginVisible = value; OnPropertyChanged(); } }

        private bool _isRegisterMode = false;
        public string LoginBtnText => _isRegisterMode ? "Register" : "Login";
        public string ToggleBtnText => _isRegisterMode ? "Already have an account? Login" : "Don't have an account? Register";

        public ChatListPage()
        {
            InitializeComponent();
            BindingContext = this;

            // Initialize Theme & Session
            MessageViewModel.IsDarkMode = SettingsManager.IsDarkMode;
            var session = SettingsManager.GetSession();
            if (!string.IsNullOrEmpty(session.Token)) IsLoginVisible = false;

            ChatListCollectionView.ItemsSource = Chats;

            Client.OnLog += (msg) => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = msg);
            Client.OnLoginSuccess += () => MainThread.BeginInvokeOnMainThread(() => { IsLoginVisible = false; LoadChats(); });
            Client.OnLoginFailed += () => MainThread.BeginInvokeOnMainThread(() => {
                StatusLabel.Text = "Auth failed.";
                SettingsManager.ClearSession();
                IsLoginVisible = true;
            });
            Client.OnChatListUpdated += LoadChats;

            if (!IsLoginVisible) CheckAutoLogin();
        }

        private async void CheckAutoLogin()
        {
            var session = SettingsManager.GetSession();
            if (!string.IsNullOrEmpty(session.Token))
            {
                StatusLabel.Text = "Welcome back...";
                await Client.Reconnect(session.UserId, session.Token);
            }
        }

        private void LoadChats()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Client.AllChats == null || Client.AllChats.Count == 0) { IsListEmpty = true; return; }
                IsListEmpty = false;

                foreach (var c in Client.AllChats)
                {
                    string name = c.ChatName ?? "";
                    string avatarUrl = Client.GetFullUrl(c.AvatarUrl);
                    if (c.ChatType == "private" && c.Members != null)
                    {
                        var otherId = c.Members.FirstOrDefault(m => m != Client.CurrentUserId);
                        name = Client.GetUserName(otherId);
                        avatarUrl = Client.GetUserAvatar(otherId);
                    }
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

                    var existing = Chats.FirstOrDefault(x => x.ChatId == c.ChatId);
                    if (existing != null)
                    {
                        if (existing.DisplayName != name) existing.DisplayName = name;
                        if (existing.LastMessagePreview != preview) existing.LastMessagePreview = preview;
                        if (existing.AvatarUrl != avatarUrl) existing.AvatarUrl = avatarUrl;
                    }
                    else
                    {
                        Chats.Add(new ChatViewModel
                        {
                            ChatId = c.ChatId,
                            DisplayName = name,
                            AvatarUrl = avatarUrl,
                            AvatarLetter = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper(),
                            LastMessagePreview = preview
                        });
                    }
                }
                // Remove deleted
                for (int i = Chats.Count - 1; i >= 0; i--)
                {
                    if (!Client.AllChats.Any(c => c.ChatId == Chats[i].ChatId)) Chats.RemoveAt(i);
                }
            });
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserEntry.Text) || string.IsNullOrEmpty(PassEntry.Text)) return;
            StatusLabel.Text = "Connecting...";
            if (_isRegisterMode) await Client.ConnectAndRegister(UserEntry.Text, PassEntry.Text);
            else await Client.ConnectAndLogin(UserEntry.Text, PassEntry.Text);
        }

        private void OnToggleLoginMode(object sender, EventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;
            OnPropertyChanged(nameof(LoginBtnText));
            OnPropertyChanged(nameof(ToggleBtnText));
        }

        private void OnToggleTheme(object sender, EventArgs e)
        {
            SettingsManager.IsDarkMode = !SettingsManager.IsDarkMode;
            MessageViewModel.IsDarkMode = SettingsManager.IsDarkMode;
            OnPropertyChanged(nameof(MainBgColor));
            OnPropertyChanged(nameof(TextColor));
            foreach (var chat in Chats) chat.RefreshColor();
            if (IsDesktop) DesktopChatView.RefreshColors();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ChatViewModel selectedChat)
            {
                if (IsDesktop) DesktopChatView.LoadChat(selectedChat.ChatId);
                else await Navigation.PushAsync(new MobileChatPage(selectedChat.ChatId, selectedChat.DisplayName));
                ChatListCollectionView.SelectedItem = null;
            }
        }
    }
}