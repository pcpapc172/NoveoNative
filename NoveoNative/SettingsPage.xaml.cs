namespace NoveoNative;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        AvatarImage.Source = ChatListPage.Client.CurrentUserAvatar;
        UsernameEntry.Text = ChatListPage.Client.CurrentUsername;
    }

    private async void OnUploadAvatar(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync();
        if (file != null)
        {
            var url = await ChatListPage.Client.UploadFile(file, "avatar");
            if (url != null)
                await DisplayAlert("Success", "Avatar updated! Restart app to see changes everywhere.", "OK");
        }
    }

    private async void OnSaveUsername(object sender, EventArgs e)
    {
        await ChatListPage.Client.UpdateUsername(UsernameEntry.Text);
        await DisplayAlert("Success", "Username updated!", "OK");
    }

    private void OnLogout(object sender, EventArgs e)
    {
        SettingsManager.ClearSession();
        // Reset Main Page to ChatList (which will show login)
        Application.Current!.Windows[0].Page = new NavigationPage(new ChatListPage());
    }
}