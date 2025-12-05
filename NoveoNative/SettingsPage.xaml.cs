namespace NoveoNative;

public partial class SettingsPage : BaseContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = this;
        AvatarImage.Source = ChatListPage.Client.CurrentUserAvatar;
        UsernameEntry.Text = ChatListPage.Client.CurrentUsername;
        ThemeSwitch.IsToggled = SettingsManager.IsDarkMode;
    }

    private async void OnUploadAvatar(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync(new PickOptions
        {
            FileTypes = FilePickerFileType.Images,
            PickerTitle = "Select Avatar"
        });

        if (file != null)
        {
            var url = await ChatListPage.Client.UploadFile(file, "avatar");
            if (url != null)
            {
                AvatarImage.Source = ChatListPage.Client.CurrentUserAvatar;
                await DisplayAlert("Success", "Avatar updated!", "OK");
            }
        }
    }

    private async void OnSaveUsername(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(UsernameEntry.Text))
        {
            await ChatListPage.Client.UpdateUsername(UsernameEntry.Text);
            await DisplayAlert("Success", "Username updated!", "OK");
        }
    }

    private void OnToggleTheme(object sender, ToggledEventArgs e)
    {
        SettingsManager.IsDarkMode = e.Value;
        MessageViewModel.IsDarkMode = e.Value;
        RefreshTheme();
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnLogout(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            SettingsManager.ClearSession();
            Application.Current!.MainPage = new ChatListPage();
        }
    }
}
