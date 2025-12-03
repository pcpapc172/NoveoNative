using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace NoveoNative;

public partial class ChatView : ContentView, INotifyPropertyChanged
{
    private string _currentChatId = "";
    private string? _replyToId;
    private object? _attachedFileObj;
    private MessageViewModel? _forwardMsg;

    public ObservableCollection<MessageViewModel> Messages { get; set; } = new ObservableCollection<MessageViewModel>();

    public Color MainBgColor => MessageViewModel.IsDarkMode ? Color.FromArgb("#111827") : Color.FromArgb("#E5DDD5");
    public Color InputBgColor => MessageViewModel.IsDarkMode ? Color.FromArgb("#1f2937") : Colors.White;
    public Color EntryBgColor => MessageViewModel.IsDarkMode ? Color.FromArgb("#374151") : Color.FromArgb("#f0f0f0");
    public Color EntryTextColor => MessageViewModel.IsDarkMode ? Colors.White : Colors.Black;

    public bool IsPreviewActive => IsReplying || IsFileAttached || IsUploading || IsForwarding;
    private bool _isReplying; public bool IsReplying { get => _isReplying; set { _isReplying = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPreviewActive)); } }
    private string _replyText = ""; public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged(); } }

    private bool _isForwarding; public bool IsForwarding { get => _isForwarding; set { _isForwarding = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPreviewActive)); } }
    private string _forwardText = ""; public string ForwardText { get => _forwardText; set { _forwardText = value; OnPropertyChanged(); } }

    private bool _isFileAttached; public bool IsFileAttached { get => _isFileAttached; set { _isFileAttached = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPreviewActive)); } }
    private string _attachedFileName = ""; public string AttachedFileName { get => _attachedFileName; set { _attachedFileName = value; OnPropertyChanged(); } }
    private bool _isUploading; public bool IsUploading { get => _isUploading; set { _isUploading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPreviewActive)); } }
    private double _uploadProgress; public double UploadProgress { get => _uploadProgress; set { _uploadProgress = value; OnPropertyChanged(); } }
    private bool _isLoading; public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    public ICommand SendCommand { get; private set; }

    // Context Menu Commands
    public ICommand CtxReplyCommand { get; }
    public ICommand CtxForwardCommand { get; }
    public ICommand CtxCopyCommand { get; }
    public ICommand CtxDeleteCommand { get; }

    public ChatView()
    {
        InitializeComponent();
        BindingContext = this;

        var selector = (MessageDataTemplateSelector)Resources["MessageSelector"];
        selector.IncomingTemplate = (DataTemplate)Resources["IncomingMessageTemplate"];
        selector.OutgoingTemplate = (DataTemplate)Resources["OutgoingMessageTemplate"];
        MessagesList.ItemsSource = Messages;

        SendCommand = new Command(() => OnSendClicked(this, EventArgs.Empty));

        // Initialize Commands for Windows Right Click
        CtxReplyCommand = new Command<MessageViewModel>(vm => { _replyToId = vm.MessageId; ReplyText = $"Replying to {vm.SenderName}"; IsReplying = true; IsForwarding = false; });
        CtxForwardCommand = new Command<MessageViewModel>(vm => { _forwardMsg = vm; ForwardText = "Forwarding message..."; IsForwarding = true; IsReplying = false; });
        CtxCopyCommand = new Command<MessageViewModel>(async vm => await Clipboard.SetTextAsync(vm.Text));
        CtxDeleteCommand = new Command<MessageViewModel>(async vm => await ChatListPage.Client.DeleteMessage(_currentChatId, vm.MessageId));

        ChatListPage.Client.OnMessageReceived += OnNewMessage;
        ChatListPage.Client.OnMessageDeleted += OnMessageDeleted;
        ChatListPage.Client.OnUploadProgress += (p) => MainThread.BeginInvokeOnMainThread(() => UploadProgress = p);
    }

    private void OnEntryCompleted(object sender, EventArgs e) => OnSendClicked(this, EventArgs.Empty);

    public async void LoadChat(string chatId)
    {
        if (_currentChatId == chatId) return;
        _currentChatId = chatId;
        IsLoading = true;
        Messages.Clear();
        var chat = ChatListPage.Client.AllChats.FirstOrDefault(c => c.ChatId == _currentChatId);
        if (chat?.Messages != null)
        {
            var vms = await Task.Run(() => {
                var list = new List<MessageViewModel>();
                foreach (var m in chat.Messages) list.Add(CreateMessageVM(m, chat));
                return list;
            });
            foreach (var vm in vms) Messages.Add(vm);
        }
        IsLoading = false;
        await Task.Delay(100);
        ScrollToBottom();
    }

    private void OnNewMessage(ServerMessage? msg)
    {
        if (msg != null && msg.ChatId == _currentChatId)
            MainThread.BeginInvokeOnMainThread(() => {
                var chat = ChatListPage.Client.AllChats.FirstOrDefault(c => c.ChatId == _currentChatId);
                Messages.Add(CreateMessageVM(msg, chat));
                ScrollToBottom();
            });
    }

    private void OnMessageDeleted(string msgId, string chatId)
    {
        if (chatId == _currentChatId)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                var item = Messages.FirstOrDefault(m => m.MessageId == msgId);
                if (item != null) Messages.Remove(item);
            });
        }
    }

    private MessageViewModel CreateMessageVM(ServerMessage msg, Chat? chatContext)
    {
        var parsed = ChatListPage.Client.ParseMessageContent(msg.Content);
        if (parsed.Text.Trim().StartsWith("{") && parsed.Text.Contains("\"text\"")) parsed = ChatListPage.Client.ParseMessageContent(parsed.Text);

        bool isMine = msg.SenderId == ChatListPage.Client.CurrentUserId;
        string avatar = ChatListPage.Client.GetUserAvatar(msg.SenderId);
        string name = ChatListPage.Client.GetUserName(msg.SenderId);

        string replyName = "", replyText = "";
        bool isReply = !string.IsNullOrEmpty(msg.ReplyToId);

        if (isReply && chatContext?.Messages != null)
        {
            var original = chatContext.Messages.FirstOrDefault(m => m.MessageId == msg.ReplyToId);
            if (original != null)
            {
                replyName = ChatListPage.Client.GetUserName(original.SenderId);
                var origParsed = ChatListPage.Client.ParseMessageContent(original.Content);
                replyText = origParsed.IsFile ? "📎 Attachment" : origParsed.Text;
            }
        }

        var vm = new MessageViewModel
        {
            MessageId = msg.MessageId ?? "",
            SenderName = name,
            SenderLetter = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper(),
            AvatarUrl = avatar,
            HasAvatar = !string.IsNullOrEmpty(avatar),
            HasNoAvatar = string.IsNullOrEmpty(avatar),
            Text = parsed.Text,
            HasText = !string.IsNullOrWhiteSpace(parsed.Text),
            IsTheme = parsed.IsTheme,
            ThemeName = parsed.ThemeName,
            IsFile = parsed.IsFile,
            IsImage = parsed.IsImage,
            IsVideo = parsed.IsVideo,
            IsAudio = parsed.IsAudio,
            FileName = parsed.FileName,
            FileUrl = parsed.FileUrl,
            IsForwarded = parsed.IsForwarded,
            ForwardedFrom = parsed.ForwardedFrom,
            IsReply = isReply,
            ReplyToName = replyName,
            ReplyToText = replyText,
            Time = DateTimeOffset.FromUnixTimeSeconds(msg.Timestamp).LocalDateTime.ToShortTimeString(),
            IsMine = isMine
        };

        vm.OnMenuRequest += ShowMessageOptions;
        return vm;
    }

    private async void ShowMessageOptions(MessageViewModel vm)
    {
        string[] options = vm.IsMine ? new[] { "Reply", "Forward", "Copy", "Delete" } : new[] { "Reply", "Forward", "Copy" };
        string action = await Application.Current!.Windows[0].Page!.DisplayActionSheet("Options", "Cancel", null, options);
        if (action == "Reply") { _replyToId = vm.MessageId; ReplyText = $"Replying to {vm.SenderName}"; IsReplying = true; IsForwarding = false; }
        else if (action == "Forward") { _forwardMsg = vm; ForwardText = "Forwarding..."; IsForwarding = true; IsReplying = false; }
        else if (action == "Copy") { await Clipboard.SetTextAsync(vm.Text); }
        else if (action == "Delete") { await ChatListPage.Client.DeleteMessage(_currentChatId, vm.MessageId); }
    }

    private async void OnAttachClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.PickAsync();
        if (result != null)
        {
            IsUploading = true; UploadProgress = 0;
            _attachedFileObj = await ChatListPage.Client.UploadFile(result);
            IsUploading = false;
            if (_attachedFileObj != null) { AttachedFileName = result.FileName; IsFileAttached = true; }
            else await Application.Current!.Windows[0].Page!.DisplayAlert("Error", "Upload failed", "OK");
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MsgEntry.Text) || IsFileAttached || IsForwarding)
        {
            object? forwardInfo = null;
            if (IsForwarding && _forwardMsg != null)
            {
                forwardInfo = new { from = _forwardMsg.SenderName, originalTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
                if (string.IsNullOrWhiteSpace(MsgEntry.Text)) MsgEntry.Text = _forwardMsg.Text;
            }
            await ChatListPage.Client.SendMessage(_currentChatId, MsgEntry.Text, _attachedFileObj, _replyToId, forwardInfo);
            MsgEntry.Text = ""; OnCancelPreview(null, null);
        }
    }

    private void OnCancelPreview(object sender, EventArgs e) { IsReplying = false; _replyToId = null; IsFileAttached = false; _attachedFileObj = null; IsUploading = false; IsForwarding = false; _forwardMsg = null; }
    public void RefreshColors() { OnPropertyChanged(nameof(MainBgColor)); OnPropertyChanged(nameof(InputBgColor)); OnPropertyChanged(nameof(EntryBgColor)); OnPropertyChanged(nameof(EntryTextColor)); foreach (var m in Messages) m.RefreshColors(); }
    private void ScrollToBottom() { if (Messages.Count > 0) try { MessagesList.ScrollTo(Messages.Last(), null, ScrollToPosition.End, false); } catch { } }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected new void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}