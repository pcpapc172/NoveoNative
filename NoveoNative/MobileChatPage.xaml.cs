namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string chatName, string? recipientId = null)
    {
        InitializeComponent();

        // Bind header to ChatView properties
        BindingContext = ChatViewControl;

        // Load chat
        ChatViewControl.LoadChat(chatId, recipientId);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Set status bar color for iOS
#if IOS
        Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, true);
#endif
    }
}
