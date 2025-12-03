using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoveoNative
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public string ChatId { get; set; } = "";

        private string _displayName = "";
        public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }

        private string _avatarLetter = "";
        public string AvatarLetter { get => _avatarLetter; set { _avatarLetter = value; OnPropertyChanged(); } }

        private string _avatarUrl = "";
        public string AvatarUrl { get => _avatarUrl; set { _avatarUrl = value; OnPropertyChanged(); } }

        private string _lastMessagePreview = "";
        public string LastMessagePreview { get => _lastMessagePreview; set { _lastMessagePreview = value; OnPropertyChanged(); } }

        public Color DisplayTextColor => MessageViewModel.IsDarkMode ? Colors.White : Colors.Black;

        public void RefreshColor() => OnPropertyChanged(nameof(DisplayTextColor));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}