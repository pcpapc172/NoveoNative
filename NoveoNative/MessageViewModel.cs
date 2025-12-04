using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace NoveoNative
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        public static bool IsDarkMode { get; set; } = false;

        public string MessageId { get; set; } = "";
        public string SenderId { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string SenderLetter { get; set; } = "";

        public string AvatarUrl { get; set; } = "";
        public bool HasAvatar { get; set; }
        public bool HasNoAvatar { get; set; }

        // --- NEW: Dynamic Avatar Color for Bubbles ---
        public Color AvatarBgColor
        {
            get
            {
                if (string.IsNullOrEmpty(SenderName)) return Colors.Gray;
                int hash = Math.Abs(SenderName.GetHashCode());
                string[] colors = new[] { "#ef4444", "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4", "#3b82f6", "#8b5cf6", "#d946ef", "#ec4899" };
                return Color.FromArgb(colors[hash % colors.Length]);
            }
        }
        // ---------------------------------------------

        public string Text { get; set; } = "";
        public bool HasText { get; set; }

        private FormattedString _messageFormatted = new FormattedString();
        public FormattedString MessageFormatted
        {
            get => _messageFormatted;
            set { _messageFormatted = value; OnPropertyChanged(); }
        }

        public string Time { get; set; } = "";
        public bool IsMine { get; set; }

        // Forwarding / Reply
        public bool IsForwarded { get; set; }
        public string ForwardedFrom { get; set; } = "";
        public string ForwardedLabel => $"↪ Forwarded from {ForwardedFrom}";

        public bool IsReply { get; set; }
        public string ReplyToName { get; set; } = "";
        public string ReplyToText { get; set; } = "";

        // Media
        public bool IsTheme { get; set; }
        public string ThemeName { get; set; } = "";
        public bool IsFile { get; set; }
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }
        public bool IsAudio { get; set; }
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public bool ShowGenericFile => IsFile && !IsImage && !IsVideo && !IsAudio;

        // Colors
        public Color BubbleColor => IsMine ? Color.FromArgb("#3b82f6") : (IsDarkMode ? Color.FromArgb("#1f2937") : Colors.White);
        public Color TextColor => IsMine ? Colors.White : (IsDarkMode ? Colors.White : Colors.Black);
        public Color SenderColor => IsDarkMode ? Color.FromArgb("#60a5fa") : Colors.Orange;
        public Color ReplyQuoteColor => IsMine ? Color.FromArgb("#55FFFFFF") : (IsDarkMode ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#22000000"));
        public Color AttachmentBgColor => IsMine ? Color.FromArgb("#33FFFFFF") : (IsDarkMode ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#11000000"));

        // Events & Commands
        public Action<MessageViewModel>? OnMenuRequest;
        public Action<string>? OnOpenDMRequest;

        public ICommand OpenFileCommand { get; }
        public ICommand ViewMediaCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand ReplyCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenMenuCommand { get; }
        public ICommand OpenDMCommand { get; }

        public MessageViewModel()
        {
            OpenMenuCommand = new Command(() => OnMenuRequest?.Invoke(this));
            OpenDMCommand = new Command(() => OnOpenDMRequest?.Invoke(SenderId));

            CopyCommand = new Command(async () => await Clipboard.SetTextAsync(Text));
            ReplyCommand = new Command(() => OnMenuRequest?.Invoke(this));
            ForwardCommand = new Command(() => OnMenuRequest?.Invoke(this));
            DeleteCommand = new Command(() => OnMenuRequest?.Invoke(this));

            OpenFileCommand = new Command(async () => {
                if (!string.IsNullOrEmpty(FileUrl)) try { await Launcher.OpenAsync(new Uri(FileUrl)); } catch { }
            });

            ViewMediaCommand = new Command(async () => {
                if (IsImage) await Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(new MediaViewerPage(FileUrl));
                else if (!string.IsNullOrEmpty(FileUrl)) await Launcher.OpenAsync(new Uri(FileUrl));
            });
        }

        public void ProcessMentions()
        {
            var fs = new FormattedString();

            if (string.IsNullOrEmpty(Text))
            {
                MessageFormatted = fs;
                return;
            }

            string[] parts = Regex.Split(Text, @"(\s+)");

            foreach (var part in parts)
            {
                var span = new Span { Text = part, TextColor = TextColor };

                if (part.Trim().StartsWith("@") && part.Length > 1)
                {
                    span.TextColor = IsMine ? Color.FromArgb("#ADD8E6") : Colors.DeepSkyBlue;
                    span.FontAttributes = FontAttributes.Bold;
                    span.TextDecorations = TextDecorations.Underline;

                    var handle = part.Trim();
                    var gesture = new TapGestureRecognizer
                    {
                        Command = new Command(async () =>
                        {
                            await ChatListPage.Client.GetChannelByHandle(handle);
                        })
                    };
                    span.GestureRecognizers.Add(gesture);
                }

                fs.Spans.Add(span);
            }
            MessageFormatted = fs;
        }

        public void RefreshColors()
        {
            OnPropertyChanged(nameof(BubbleColor)); OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(SenderColor)); OnPropertyChanged(nameof(ReplyQuoteColor));
            OnPropertyChanged(nameof(AttachmentBgColor));
            ProcessMentions();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}