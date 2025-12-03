using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        public string Text { get; set; } = "";
        public bool HasText { get; set; }
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

        // --- COLORS ---
        public Color BubbleColor => IsMine ? Color.FromArgb("#3b82f6") : (IsDarkMode ? Color.FromArgb("#1f2937") : Colors.White);
        public Color TextColor => IsMine ? Colors.White : (IsDarkMode ? Colors.White : Colors.Black);
        public Color SenderColor => IsDarkMode ? Color.FromArgb("#60a5fa") : Colors.Orange;
        public Color ReplyQuoteColor => IsMine ? Color.FromArgb("#55FFFFFF") : (IsDarkMode ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#22000000"));

        // FIX: Dynamic Attachment Background
        public Color AttachmentBgColor => IsMine
            ? Color.FromArgb("#33FFFFFF")
            : (IsDarkMode ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#11000000"));

        // --- COMMANDS ---
        public Action<MessageViewModel>? OnMenuRequest;

        public ICommand OpenFileCommand { get; }
        public ICommand ViewMediaCommand { get; }

        // Context Menu Commands
        public ICommand CopyCommand { get; }
        public ICommand ReplyCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenMenuCommand { get; }

        public MessageViewModel()
        {
            OpenMenuCommand = new Command(() => OnMenuRequest?.Invoke(this));

            // Logic for context menu items
            CopyCommand = new Command(async () => await Clipboard.SetTextAsync(Text));
            ReplyCommand = new Command(() => OnMenuRequest?.Invoke(this)); // Fallback to menu for now or trigger reply directly
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

        public void RefreshColors()
        {
            OnPropertyChanged(nameof(BubbleColor)); OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(SenderColor)); OnPropertyChanged(nameof(ReplyQuoteColor));
            OnPropertyChanged(nameof(AttachmentBgColor));
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}