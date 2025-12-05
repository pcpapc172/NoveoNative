namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string chatName, string? recipientId = null)
    {
        InitializeComponent();

        // Load chat
        ChatViewControl.LoadChat(chatId, recipientId);

        // Enable back button in ChatView
        ChatViewControl.ShowMobileBackButton = true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if IOS
        Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, true);
#endif
    }
}
