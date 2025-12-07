using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace NoveoNative;

public class MessageViewModel : INotifyPropertyChanged
{
    public static bool IsDarkMode = false;

    public string MessageId { get; set; } = "";
    public string SenderId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public bool HasAvatar => !string.IsNullOrEmpty(AvatarUrl);
    public bool HasNoAvatar => string.IsNullOrEmpty(AvatarUrl);
    public string SenderLetter => string.IsNullOrEmpty(SenderName) ? "?" : SenderName.Substring(0, 1).ToUpper();
    public Color AvatarBgColor => Color.FromArgb("#3b82f6");

    private string _text = "";
    public string Text { get => _text; set { _text = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasText)); OnPropertyChanged(nameof(MessageFormatted)); } }

    public bool HasText => !string.IsNullOrEmpty(_text);

    // ✅ NEW: Can edit only text messages without files/themes
    public bool CanEdit => !IsTheme && !IsImage && !IsVideo && !IsAudio && !ShowGenericFile;

    // ✅ NEW: Parse username with tag format: "Username [#COLOR, "TAG"]"
    public FormattedString SenderNameFormatted
    {
        get
        {
            var formatted = new FormattedString();
            if (string.IsNullOrEmpty(SenderName))
            {
                formatted.Spans.Add(new Span { Text = "Unknown", TextColor = SenderColor });
                return formatted;
            }

            // Regex to match: "Username [#COLOR, "TAG"]"
            var tagRegex = new Regex(@"^(.*?)\s*\[\s*#([0-9a-fA-F]{3,6})\s*,\s*""([^""]+)""\s*\]$");
            var match = tagRegex.Match(SenderName);

            if (match.Success)
            {
                // Display name
                var displayName = match.Groups[1].Value.Trim();
                formatted.Spans.Add(new Span
                {
                    Text = displayName + " ",
                    TextColor = SenderColor,
                    FontAttributes = FontAttributes.Bold
                });

                // Tag with color
                var colorHex = "#" + match.Groups[2].Value;
                var tagText = match.Groups[3].Value;

                try
                {
                    var tagColor = Color.FromArgb(colorHex);
                    formatted.Spans.Add(new Span
                    {
                        Text = tagText,
                        TextColor = Colors.White,
                        BackgroundColor = tagColor,
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold
                    });
                }
                catch
                {
                    // Fallback if color is invalid
                    formatted.Spans.Add(new Span
                    {
                        Text = tagText,
                        TextColor = Colors.White,
                        BackgroundColor = Color.FromArgb("#3b82f6"),
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold
                    });
                }
            }
            else
            {
                // No tag, just display name
                formatted.Spans.Add(new Span
                {
                    Text = SenderName,
                    TextColor = SenderColor,
                    FontAttributes = FontAttributes.Bold
                });
            }

            return formatted;
        }
    }

    // FormattedString with @mention detection
    public FormattedString MessageFormatted
    {
        get
        {
            var formatted = new FormattedString();
            if (string.IsNullOrEmpty(_text)) return formatted;

            var words = _text.Split(' ');
            foreach (var word in words)
            {
                if (word.StartsWith("@") && word.Length > 1)
                {
                    var span = new Span
                    {
                        Text = word + " ",
                        TextColor = Color.FromArgb("#3b82f6"),
                        TextDecorations = TextDecorations.Underline
                    };
                    span.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            Application.Current?.MainPage?.Navigation.PushAsync(new ChannelPreviewPage(word.Substring(1)));
                        })
                    });
                    formatted.Spans.Add(span);
                }
                else
                {
                    formatted.Spans.Add(new Span { Text = word + " ", TextColor = TextColor });
                }
            }

            return formatted;
        }
    }

    public string Time { get; set; } = "";
    public bool IsOutgoing { get; set; }
    public bool IsIncoming => !IsOutgoing;

    public Color BubbleColor => IsDarkMode ? Color.FromArgb("#374151") : Color.FromArgb("#e5e7eb");
    public Color TextColor => IsDarkMode ? Colors.White : Colors.Black;
    public Color SenderColor => Color.FromArgb("#f97316");
    public Color ReplyQuoteColor => IsDarkMode ? Color.FromArgb("#1f2937") : Color.FromArgb("#d1d5db");
    public Color AttachmentBgColor => IsDarkMode ? Color.FromArgb("#1f2937") : Color.FromArgb("#f3f4f6");

    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public bool IsImage { get; set; }
    public bool IsVideo { get; set; }
    public bool IsAudio { get; set; }
    public bool ShowGenericFile { get; set; }

    public bool IsTheme { get; set; }
    public string? ThemeName { get; set; }

    public bool IsReply { get; set; }
    public string? ReplyToId { get; set; }
    public string? ReplyToName { get; set; }
    public string? ReplyToText { get; set; }

    public bool IsForwarded { get; set; }
    public string? ForwardedFrom { get; set; }
    // ✅ FIXED: Show username instead of UUID (issue #7)
    public string ForwardedLabel => IsForwarded ? $"Forwarded from {ForwardedFrom}" : "";

    // Seen status
    private List<string> _seenBy = new List<string>();
    public List<string> SeenBy
    {
        get => _seenBy;
        set
        {
            _seenBy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SeenStatus));
            OnPropertyChanged(nameof(SeenCheckColor));
            OnPropertyChanged(nameof(ShowSeenCheckmarks));
        }
    }

    // Checkmark display
    public string SeenStatus
    {
        get
        {
            if (!IsOutgoing) return "";

            if (SeenBy != null && SeenBy.Count > 0)
            {
                return "✓✓";
            }
            else
            {
                return "✓";
            }
        }
    }

    public Color SeenCheckColor => (SeenBy != null && SeenBy.Count > 0)
        ? Color.FromArgb("#ffffff")  // White for seen (visible on blue bubble)
        : Color.FromArgb("#b8d4ff");  // Light blue for unseen

    public bool ShowSeenCheckmarks => IsOutgoing;

    public ICommand OpenDMCommand { get; }
    public ICommand ViewMediaCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenMenuCommand { get; }

    public MessageViewModel()
    {
        OpenDMCommand = new Command(OnOpenDM);
        ViewMediaCommand = new Command(OnViewMedia);
        OpenFileCommand = new Command(OnOpenFile);
        OpenMenuCommand = new Command(OnOpenMenu);
    }

    public static MessageViewModel FromServerMessage(ServerMessage msg, NoveoClient client)
    {
        var vm = new MessageViewModel
        {
            MessageId = msg.MessageId ?? "",
            SenderId = msg.SenderId ?? "",
            SenderName = client.GetUserName(msg.SenderId),
            AvatarUrl = client.GetUserAvatar(msg.SenderId),
            Time = DateTimeOffset.FromUnixTimeMilliseconds(msg.Timestamp).ToLocalTime().ToString("HH:mm"),
            IsOutgoing = msg.SenderId == client.CurrentUserId,
            SeenBy = msg.SeenBy ?? new List<string>()
        };

        var parsed = client.ParseMessageContent(msg.Content);
        vm.Text = parsed.Text;
        vm.IsImage = parsed.IsImage;
        vm.IsVideo = parsed.IsVideo;
        vm.IsAudio = parsed.IsAudio;
        vm.ShowGenericFile = parsed.IsFile && !parsed.IsImage && !parsed.IsVideo && !parsed.IsAudio;
        vm.FileUrl = parsed.FileUrl;
        vm.FileName = parsed.FileName;
        vm.IsTheme = parsed.IsTheme;
        vm.ThemeName = parsed.ThemeName;

        if (!string.IsNullOrEmpty(msg.ReplyToId))
        {
            vm.IsReply = true;
            vm.ReplyToId = msg.ReplyToId;
            var replyMsg = client.AllChats.SelectMany(c => c.Messages ?? new List<ServerMessage>())
                .FirstOrDefault(m => m.MessageId == msg.ReplyToId);
            if (replyMsg != null)
            {
                vm.ReplyToName = client.GetUserName(replyMsg.SenderId);
                var replyParsed = client.ParseMessageContent(replyMsg.Content);
                vm.ReplyToText = replyParsed.Text;
            }
        }

        // ✅ FIXED: Get username from userId (issue #7)
        if (parsed.IsForwarded)
        {
            vm.IsForwarded = true;
            vm.ForwardedFrom = client.GetUserName(parsed.ForwardedFrom);
        }

        return vm;
    }

    public void RefreshColors()
    {
        OnPropertyChanged(nameof(BubbleColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(ReplyQuoteColor));
        OnPropertyChanged(nameof(AttachmentBgColor));
        OnPropertyChanged(nameof(MessageFormatted));
        OnPropertyChanged(nameof(SenderNameFormatted));
    }

    private void OnOpenDM()
    {
        if (IsOutgoing) return;
        var client = ChatListPage.Client;
        var ids = new List<string> { client.CurrentUserId, SenderId };
        ids.Sort();
        string chatId = string.Join("_", ids);

        if (DeviceInfo.Idiom != DeviceIdiom.Desktop)
        {
            Application.Current?.MainPage?.Navigation.PushAsync(new MobileChatPage(chatId, SenderName, SenderId));
        }
    }

    private void OnViewMedia()
    {
        if (!string.IsNullOrEmpty(FileUrl))
        {
            Application.Current?.MainPage?.Navigation.PushAsync(new MediaViewerPage(FileUrl));
        }
    }

    private async void OnOpenFile()
    {
        if (!string.IsNullOrEmpty(FileUrl))
        {
            try
            {
                await Launcher.OpenAsync(new Uri(FileUrl));
            }
            catch { }
        }
    }

    private void OnOpenMenu() { }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}