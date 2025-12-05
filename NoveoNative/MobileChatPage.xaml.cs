namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string chatName, string? recipientId = null)
    {
        InitializeComponent();
        Title = chatName;

        // ADD KEYBOARD BEHAVIOR FOR ANDROID/iOS
        Behaviors.Add(new KeyboardBehavior());

        // Load chat
        MainChatView.LoadChat(chatId, recipientId);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
