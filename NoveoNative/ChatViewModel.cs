using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NoveoNative
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public string ChatId { get; set; } = "";

        private string _displayName = "";
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayNameFormatted));
            }
        }

        // ✅ NEW: Parse username with tag format for chat list
        public FormattedString DisplayNameFormatted
        {
            get
            {
                var formatted = new FormattedString();
                if (string.IsNullOrEmpty(_displayName))
                {
                    formatted.Spans.Add(new Span { Text = "Unknown", TextColor = DisplayTextColor });
                    return formatted;
                }

                // Regex to match: "Username [#COLOR, "TAG"]"
                var tagRegex = new Regex(@"^(.*?)\s*\[\s*#([0-9a-fA-F]{3,6})\s*,\s*""([^""]+)""\s*\]$");
                var match = tagRegex.Match(_displayName);

                if (match.Success)
                {
                    // Display name
                    var displayNamePart = match.Groups[1].Value.Trim();
                    formatted.Spans.Add(new Span
                    {
                        Text = displayNamePart + " ",
                        TextColor = DisplayTextColor,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 18
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
                        Text = _displayName,
                        TextColor = DisplayTextColor,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 18
                    });
                }

                return formatted;
            }
        }

        private string _avatarLetter = "";
        public string AvatarLetter
        {
            get => _avatarLetter;
            set
            {
                _avatarLetter = value;
                OnPropertyChanged();
            }
        }

        private string _avatarUrl = "";
        public string AvatarUrl
        {
            get => _avatarUrl;
            set
            {
                _avatarUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAvatarUrl));
            }
        }

        private string _lastMessagePreview = "";
        public string LastMessagePreview
        {
            get => _lastMessagePreview;
            set
            {
                _lastMessagePreview = value;
                OnPropertyChanged();
            }
        }

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

        public void RefreshColor()
        {
            OnPropertyChanged(nameof(DisplayTextColor));
            OnPropertyChanged(nameof(DisplayNameFormatted));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}