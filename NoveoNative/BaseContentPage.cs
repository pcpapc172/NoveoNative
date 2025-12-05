using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoveoNative
{
    public class BaseContentPage : ContentPage, INotifyPropertyChanged
    {
        public Color PageBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#111827") : Colors.White;
        public Color PageTextColor => SettingsManager.IsDarkMode ? Colors.White : Colors.Black;
        public Color CardBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#1f2937") : Colors.White;
        public Color BorderColor => SettingsManager.IsDarkMode ? Color.FromArgb("#374151") : Color.FromArgb("#e5e7eb");
        public Color InputBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#374151") : Color.FromArgb("#f3f4f6");
        public Color SecondaryBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#1f2937") : Color.FromArgb("#f9fafb");
        public Color PlaceholderColor => SettingsManager.IsDarkMode ? Color.FromArgb("#9ca3af") : Color.FromArgb("#6b7280");

        public BaseContentPage()
        {
            BindingContext = this;
        }

        public void RefreshTheme()
        {
            OnPropertyChanged(nameof(PageBgColor));
            OnPropertyChanged(nameof(PageTextColor));
            OnPropertyChanged(nameof(CardBgColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(InputBgColor));
            OnPropertyChanged(nameof(SecondaryBgColor));
            OnPropertyChanged(nameof(PlaceholderColor));
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
