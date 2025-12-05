namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string chatName, string? recipientId = null)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        MainChatView.LoadChat(chatId, recipientId);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopModalAsync();
        return true;
    }
}
