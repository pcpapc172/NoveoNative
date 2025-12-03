namespace NoveoNative;

public partial class MobileChatPage : ContentPage
{
    public MobileChatPage(string chatId, string title)
    {
        InitializeComponent();
        Title = title;
        MainChatView.LoadChat(chatId);
    }
}