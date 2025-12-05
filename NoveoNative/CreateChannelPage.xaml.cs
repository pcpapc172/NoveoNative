using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NoveoNative;

public partial class CreateChannelPage : BaseContentPage
{
    private FileResult? _selectedAvatar;
    private bool _hasSelectedAvatar;

    public bool HasSelectedAvatar
    {
        get => _hasSelectedAvatar;
        set { _hasSelectedAvatar = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowDefaultAvatar)); }
    }

    public bool ShowDefaultAvatar => !HasSelectedAvatar;

    public CreateChannelPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnSelectAvatar(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Select Channel Avatar"
            });

            if (result != null)
            {
                _selectedAvatar = result;
                var stream = await result.OpenReadAsync();
                AvatarPreview.Source = ImageSource.FromStream(() => stream);
                HasSelectedAvatar = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select image: {ex.Message}", "OK");
        }
    }

    private async void OnCreateChannel(object sender, EventArgs e)
    {
        var name = ChannelNameEntry.Text?.Trim();
        var handle = HandleEntry.Text?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowStatus("Channel name is required", true);
            return;
        }

        if (string.IsNullOrEmpty(handle))
        {
            ShowStatus("Channel handle is required", true);
            return;
        }

        if (!Regex.IsMatch(handle, @"^[a-zA-Z0-9_]+$"))
        {
            ShowStatus("Handle can only contain letters, numbers, and underscores", true);
            return;
        }

        if (handle.StartsWith("@"))
            handle = handle.Substring(1);

        try
        {
            ShowStatus("Creating channel...", false);
            await ChatListPage.Client.CreateChannel(name, handle, _selectedAvatar);
            ShowStatus("Channel created successfully!", false);
            await Task.Delay(1000);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", true);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.Red : Colors.Green;
        StatusLabel.IsVisible = true;
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
