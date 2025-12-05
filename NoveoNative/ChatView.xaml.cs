using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace NoveoNative;

public partial class ChatView : ContentView, INotifyPropertyChanged
{
    public ObservableCollection<MessageViewModel> Messages { get; set; } = new ObservableCollection<MessageViewModel>();

    private string? _currentChatId;
    private string? _currentRecipientId;
    private Dictionary<string, System.Timers.Timer> _typingTimers = new Dictionary<string, System.Timers.Timer>();

    private bool _isLoading = false;
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    // Header Bar Properties
    private bool _hasActiveChat = false;
    public bool HasActiveChat { get => _hasActiveChat; set { _hasActiveChat = value; OnPropertyChanged(); } }

    private string _chatName = "";
    public string ChatName { get => _chatName; set { _chatName = value; OnPropertyChanged(); } }

    private string _chatSubtitle = "";
    public string ChatSubtitle { get => _chatSubtitle; set { _chatSubtitle = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowSubtitle)); } }

    public bool ShowSubtitle => !string.IsNullOrEmpty(_chatSubtitle);

    private string _chatAvatarUrl = "";
    public string ChatAvatarUrl { get => _chatAvatarUrl; set { _chatAvatarUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAvatarImage)); OnPropertyChanged(nameof(ShowAvatarLetter)); } }

    private string _chatAvatarLetter = "#";
    public string ChatAvatarLetter { get => _chatAvatarLetter; set { _chatAvatarLetter = value; OnPropertyChanged(); } }

    public Color ChatAvatarBgColor => Color.FromArgb("#3b82f6");
    public bool ShowAvatarLetter => string.IsNullOrEmpty(_chatAvatarUrl);
    public bool ShowAvatarImage => !string.IsNullOrEmpty(_chatAvatarUrl);
    public Color HeaderBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#1f2937") : Color.FromArgb("#f0f0f0");

    // NEW: Typing Indicator
    private string _typingStatus = "";
    public string TypingStatus { get => _typingStatus; set { _typingStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowTypingIndicator)); } }
    public bool ShowTypingIndicator => !string.IsNullOrEmpty(_typingStatus);
    private HashSet<string> _currentlyTyping = new HashSet<string>();

    // Other properties
    private bool _isReadOnlyChannel = false;
    public bool IsReadOnlyChannel { get => _isReadOnlyChannel; set { _isReadOnlyChannel = value; OnPropertyChanged(); } }

    private bool _canSend = true;
    public bool CanSend { get => _canSend; set { _canSend = value; OnPropertyChanged(); } }

    public Color MainBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#111827") : Colors.White;
    public Color InputBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#1f2937") : Color.FromArgb("#f9f9f9");
    public Color EntryBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#374151") : Colors.White;
    public Color EntryTextColor => SettingsManager.IsDarkMode ? Colors.White : Colors.Black;
    public Color TextColor => SettingsManager.IsDarkMode ? Colors.White : Colors.Black;

    private MessageViewModel? _replyToMessage;
    private MessageViewModel? _forwardMessage;
    private FileResult? _attachedFile;

    public bool IsPreviewActive => _replyToMessage != null || _forwardMessage != null || _attachedFile != null || _uploadProgress > 0;
    public bool IsReplying => _replyToMessage != null;
    public bool IsForwarding => _forwardMessage != null;
    public bool IsFileAttached => _attachedFile != null;
    public string ReplyText => _replyToMessage != null ? $"Replying to {_replyToMessage.SenderName}: {_replyToMessage.Text}" : "";
    public string ForwardText => _forwardMessage != null ? $"Forwarding message from {_forwardMessage.SenderName}" : "";
    public string AttachedFileName => _attachedFile?.FileName ?? "";

    private double _uploadProgress = 0;
    public double UploadProgress { get => _uploadProgress; set { _uploadProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsUploading)); OnPropertyChanged(nameof(IsPreviewActive)); } }
    public bool IsUploading => _uploadProgress > 0 && _uploadProgress < 1;

    public ICommand CtxReplyCommand { get; }
    public ICommand CtxForwardCommand { get; }
    public ICommand CtxCopyCommand { get; }
    public ICommand CtxDeleteCommand { get; }
    public ICommand SendCommand { get; }

    public ChatView()
    {
        InitializeComponent();
        BindingContext = this;
        MessagesList.ItemsSource = Messages;

        CtxReplyCommand = new Command<MessageViewModel>((msg) => OnReply(msg));
        CtxForwardCommand = new Command<MessageViewModel>((msg) => OnForward(msg));
        CtxCopyCommand = new Command<MessageViewModel>((msg) => OnCopy(msg));
        CtxDeleteCommand = new Command<MessageViewModel>((msg) => OnDelete(msg));
        SendCommand = new Command(() => OnSendClicked(null, EventArgs.Empty));

        ChatListPage.Client.OnMessageReceived += (msg) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (msg?.ChatId == _currentChatId)
            {
                var vm = MessageViewModel.FromServerMessage(msg, ChatListPage.Client);
                Messages.Add(vm);

                // NEW: Auto-mark as seen if chat is open
                if (msg.SenderId != ChatListPage.Client.CurrentUserId)
                {
                    ChatListPage.Client.MarkMessageAsSeen(msg.ChatId ?? "", msg.MessageId ?? "");
                }
            }
            ScrollToBottom();
        });

        ChatListPage.Client.OnMessageDeleted += (msgId, chatId) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (chatId == _currentChatId)
            {
                var toRemove = Messages.FirstOrDefault(m => m.MessageId == msgId);
                if (toRemove != null) Messages.Remove(toRemove);
            }
        });

        // NEW: Typing indicator
        ChatListPage.Client.OnUserTyping += (chatId, userId) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (chatId != _currentChatId || userId == ChatListPage.Client.CurrentUserId) return;

            _currentlyTyping.Add(userId);
            UpdateTypingStatus();

            // Remove after 3 seconds
            if (_typingTimers.ContainsKey(userId))
            {
                _typingTimers[userId].Stop();
                _typingTimers[userId].Dispose();
            }

            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentlyTyping.Remove(userId);
                    UpdateTypingStatus();
                    timer.Dispose();
                    _typingTimers.Remove(userId);
                });
            };
            timer.Start();
            _typingTimers[userId] = timer;
        });

        // NEW: Seen status update
        ChatListPage.Client.OnMessageSeen += (chatId, messageId, userId) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (chatId != _currentChatId) return;

            var msg = Messages.FirstOrDefault(m => m.MessageId == messageId);
            if (msg != null)
            {
                if (!msg.SeenBy.Contains(userId))
                {
                    msg.SeenBy.Add(userId);
                    msg.OnPropertyChanged(nameof(msg.SeenBy));
                }
            }
        });

        // NEW: Send typing indicator when user types (throttled)
        var typingTimer = new System.Timers.Timer(1000);
        bool hasTyped = false;

        MsgEntry.TextChanged += async (s, e) =>
        {
            if (!string.IsNullOrEmpty(_currentChatId) && !hasTyped)
            {
                hasTyped = true;
                await ChatListPage.Client.SendTyping(_currentChatId);

                typingTimer.Stop();
                typingTimer.Start();
            }
        };

        typingTimer.Elapsed += (s, e) =>
        {
            hasTyped = false;
            typingTimer.Stop();
        };
    }

    private void UpdateTypingStatus()
    {
        if (_currentlyTyping.Count == 0)
        {
            TypingStatus = "";
        }
        else if (_currentlyTyping.Count == 1)
        {
            var userId = _currentlyTyping.First();
            var name = ChatListPage.Client.GetUserName(userId);
            TypingStatus = $"{name} is typing...";
        }
        else if (_currentlyTyping.Count == 2)
        {
            var names = _currentlyTyping.Select(id => ChatListPage.Client.GetUserName(id)).ToList();
            TypingStatus = $"{names[0]} and {names[1]} are typing...";
        }
        else
        {
            TypingStatus = $"{_currentlyTyping.Count} people are typing...";
        }
    }

    public void LoadChat(string chatId, string? recipientId = null)
    {
        _currentChatId = chatId;
        _currentRecipientId = recipientId;
        Messages.Clear();
        _currentlyTyping.Clear();
        TypingStatus = "";
        IsLoading = true;
        HasActiveChat = true;

        // Try to find existing chat
        var chat = ChatListPage.Client.AllChats.FirstOrDefault(c => c.ChatId == chatId);

        // Handle public lobby
        if (chatId == ChatListPage.Client.PublicChatId)
        {
            ChatName = "Public Lobby";
            ChatAvatarUrl = "";
            ChatAvatarLetter = "🌍";

            int totalOnline = ChatListPage.Client.Users.Values.Count(u => u.IsOnline);
            ChatSubtitle = $"{totalOnline} users online";

            IsReadOnlyChannel = false;
            CanSend = true;

            if (chat?.Messages != null)
            {
                foreach (var msg in chat.Messages)
                {
                    var vm = MessageViewModel.FromServerMessage(msg, ChatListPage.Client);
                    Messages.Add(vm);

                    // Mark unseen messages as seen
                    if (msg.SenderId != ChatListPage.Client.CurrentUserId &&
                        (msg.SeenBy == null || !msg.SeenBy.Contains(ChatListPage.Client.CurrentUserId)))
                    {
                        ChatListPage.Client.MarkMessageAsSeen(chatId, msg.MessageId ?? "");
                    }
                }
            }
        }
        else if (chat != null)
        {
            IsReadOnlyChannel = chat.ChatType == "channel" && chat.OwnerId != ChatListPage.Client.CurrentUserId;
            CanSend = !IsReadOnlyChannel;

            if (chat.ChatType == "private" && !string.IsNullOrEmpty(recipientId))
            {
                ChatName = ChatListPage.Client.GetUserName(recipientId);
                ChatAvatarUrl = ChatListPage.Client.GetUserAvatar(recipientId);
                ChatAvatarLetter = ChatName.Substring(0, 1).ToUpper();
                ChatSubtitle = ChatListPage.Client.IsUserOnline(recipientId) ? "online" : "offline";
            }
            else if (chat.ChatType == "channel")
            {
                ChatName = chat.ChatName ?? "Channel";
                ChatAvatarUrl = ChatListPage.Client.GetFullUrl(chat.AvatarUrl);
                ChatAvatarLetter = ChatName.Substring(0, 1).ToUpper();

                int memberCount = chat.Members?.Count ?? 0;
                int onlineCount = chat.Members?.Count(m => ChatListPage.Client.IsUserOnline(m)) ?? 0;
                ChatSubtitle = $"{memberCount} members, {onlineCount} online";
            }
            else if (chat.ChatType == "public")
            {
                ChatName = "Public Lobby";
                ChatAvatarUrl = "";
                ChatAvatarLetter = "🌍";

                int totalOnline = ChatListPage.Client.Users.Values.Count(u => u.IsOnline);
                ChatSubtitle = $"{totalOnline} users online";
            }

            if (chat.Messages != null)
            {
                foreach (var msg in chat.Messages)
                {
                    var vm = MessageViewModel.FromServerMessage(msg, ChatListPage.Client);
                    Messages.Add(vm);

                    // NEW: Mark unseen messages as seen
                    if (msg.SenderId != ChatListPage.Client.CurrentUserId &&
                        (msg.SeenBy == null || !msg.SeenBy.Contains(ChatListPage.Client.CurrentUserId)))
                    {
                        ChatListPage.Client.MarkMessageAsSeen(chatId, msg.MessageId ?? "");
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(recipientId))
        {
            // New DM
            ChatName = ChatListPage.Client.GetUserName(recipientId);
            ChatAvatarUrl = ChatListPage.Client.GetUserAvatar(recipientId);
            ChatAvatarLetter = ChatName.Substring(0, 1).ToUpper();
            ChatSubtitle = ChatListPage.Client.IsUserOnline(recipientId) ? "online" : "offline";

            IsReadOnlyChannel = false;
            CanSend = true;
        }

        IsLoading = false;
        ScrollToBottom();
    }

    public void RefreshColors()
    {
        OnPropertyChanged(nameof(MainBgColor));
        OnPropertyChanged(nameof(InputBgColor));
        OnPropertyChanged(nameof(EntryBgColor));
        OnPropertyChanged(nameof(EntryTextColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(HeaderBgColor));

        foreach (var msg in Messages)
        {
            msg.RefreshColors();
        }
    }

    private async void OnAttachClicked(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync();
        if (file != null)
        {
            _attachedFile = file;
            OnPropertyChanged(nameof(IsFileAttached));
            OnPropertyChanged(nameof(AttachedFileName));
            OnPropertyChanged(nameof(IsPreviewActive));
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentChatId)) return;

        string text = MsgEntry.Text?.Trim() ?? "";
        object? fileObj = null;

        if (_attachedFile != null)
        {
            UploadProgress = 0.1;
            var uploadResult = await ChatListPage.Client.UploadFile(_attachedFile);
            if (uploadResult != null)
            {
                fileObj = uploadResult;
            }
            UploadProgress = 1.0;
            await Task.Delay(300);
            UploadProgress = 0;
        }

        if (!string.IsNullOrEmpty(text) || fileObj != null)
        {
            object? forwardInfo = null;
            if (_forwardMessage != null)
            {
                forwardInfo = new { from = _forwardMessage.SenderId, messageId = _forwardMessage.MessageId };
            }

            await ChatListPage.Client.SendMessage(_currentChatId, text, fileObj, _replyToMessage?.MessageId, forwardInfo, _currentRecipientId);

            MsgEntry.Text = "";
            _attachedFile = null;
            _replyToMessage = null;
            _forwardMessage = null;
            OnPropertyChanged(nameof(IsPreviewActive));
            OnPropertyChanged(nameof(IsReplying));
            OnPropertyChanged(nameof(IsForwarding));
            OnPropertyChanged(nameof(IsFileAttached));
        }
    }

    private void OnEntryCompleted(object sender, EventArgs e) => OnSendClicked(sender, e);

    private void OnCancelPreview(object sender, EventArgs e)
    {
        _replyToMessage = null;
        _forwardMessage = null;
        _attachedFile = null;
        UploadProgress = 0;
        OnPropertyChanged(nameof(IsPreviewActive));
        OnPropertyChanged(nameof(IsReplying));
        OnPropertyChanged(nameof(IsForwarding));
        OnPropertyChanged(nameof(IsFileAttached));
    }

    private void OnReply(MessageViewModel msg)
    {
        _replyToMessage = msg;
        _forwardMessage = null;
        OnPropertyChanged(nameof(IsPreviewActive));
        OnPropertyChanged(nameof(IsReplying));
        OnPropertyChanged(nameof(IsForwarding));
        OnPropertyChanged(nameof(ReplyText));
        MsgEntry.Focus();
    }

    private void OnForward(MessageViewModel msg)
    {
        _forwardMessage = msg;
        _replyToMessage = null;
        OnPropertyChanged(nameof(IsPreviewActive));
        OnPropertyChanged(nameof(IsReplying));
        OnPropertyChanged(nameof(IsForwarding));
        OnPropertyChanged(nameof(ForwardText));
    }

    private async void OnCopy(MessageViewModel msg)
    {
        if (!string.IsNullOrEmpty(msg.Text))
        {
            await Clipboard.SetTextAsync(msg.Text);
        }
    }

    private async void OnDelete(MessageViewModel msg)
    {
        bool confirm = await Application.Current!.MainPage!.DisplayAlert("Delete", "Delete this message?", "Yes", "No");
        if (confirm && _currentChatId != null)
        {
            await ChatListPage.Client.DeleteMessage(_currentChatId, msg.MessageId);
        }
    }

    private void ScrollToBottom()
    {
        if (Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    MessagesList.ScrollTo(Messages[Messages.Count - 1], position: ScrollToPosition.End, animate: false);
                }
                catch { }
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
