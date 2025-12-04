using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoveoNative
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public string ChatId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string AvatarLetter { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public string LastMessagePreview { get; set; } = "";

        // Logic Flags
        public bool IsPrivate { get; set; }
        public bool IsGroup { get; set; }
        public bool IsChannel { get; set; }

        public string OtherUserId { get; set; } = "";

        // --- ONLINE STATUS LOGIC ---
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OnlineStatusColor));
                OnPropertyChanged(nameof(OnlineStatusVisible));

                // Update text if no messages
                if (LastMessagePreview == "Online" || LastMessagePreview == "Offline")
                    LastMessagePreview = value ? "Online" : "Offline";
                OnPropertyChanged(nameof(LastMessagePreview));
            }
        }

        // Green if online, Gray if offline (or Transparent if you prefer hiding it)
        public Color OnlineStatusColor => IsOnline ? Colors.LightGreen : Colors.Gray;
        public bool OnlineStatusVisible => IsPrivate; // Only show dots for DMs

        // --- AVATAR VISUALS ---
        public Color DisplayTextColor => MessageViewModel.IsDarkMode ? Colors.White : Colors.Black;
        public bool HasAvatarUrl => !string.IsNullOrEmpty(AvatarUrl);
        public int AvatarCornerRadius => IsChannel ? 10 : 30;

        // Generate a color based on the name (Consistent Hash)
        public Color AvatarBgColor
        {
            get
            {
                if (string.IsNullOrEmpty(DisplayName)) return Colors.Gray;
                int hash = Math.Abs(DisplayName.GetHashCode());
                string[] colors = new[] { "#ef4444", "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4", "#3b82f6", "#8b5cf6", "#d946ef", "#ec4899" };
                return Color.FromArgb(colors[hash % colors.Length]);
            }
        }

        public void RefreshColor() => OnPropertyChanged(nameof(DisplayTextColor));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}