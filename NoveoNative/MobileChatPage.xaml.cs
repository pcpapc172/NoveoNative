namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string title, string? recipientId = null)
    {
        InitializeComponent();
        Title = title;
        MainChatView.LoadChat(chatId, recipientId);
    }
}