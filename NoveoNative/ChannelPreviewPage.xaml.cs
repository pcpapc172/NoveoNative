namespace NoveoNative;

public partial class ChannelPreviewPage : ContentPage
{
    private string _channelHandle;
    private string _channelId;
    private bool _isAlreadyJoined;

    public Color MainBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#111827") : Colors.White;
    public Color CardBgColor => SettingsManager.IsDarkMode ? Color.FromArgb("#1f2937") : Colors.White;
    public Color TextColor => SettingsManager.IsDarkMode ? Colors.White : Colors.Black;

    public ChannelPreviewPage(string channelHandle)
    {
        InitializeComponent();
        BindingContext = this;
        _channelHandle = channelHandle;
        LoadChannelInfo();
    }

    private async void LoadChannelInfo()
    {
        // Request channel info from server
        await ChatListPage.Client.GetChannelByHandle(_channelHandle);

        // Wait a bit for response
        await Task.Delay(500);

        // Check if channel exists
        var channel = ChatListPage.Client.AllChats.FirstOrDefault(c =>
            c.ChatType == "channel" && c.ChatName?.ToLower() == _channelHandle.ToLower());

        if (channel != null)
        {
            _channelId = channel.ChatId;
            ChannelName.Text = channel.ChatName ?? "Unknown Channel";
            ChannelHandle.Text = "@" + _channelHandle;
            AvatarLetter.Text = ChannelName.Text.Substring(0, 1).ToUpper();

            // Check if already joined
            _isAlreadyJoined = channel.Members?.Contains(ChatListPage.Client.CurrentUserId) ?? false;

            if (_isAlreadyJoined)
            {
                JoinButton.Text = "Open Channel";
                JoinButton.BackgroundColor = Color.FromArgb("#22c55e");
            }

            // Show member count if available
            if (channel.Members != null)
            {
                MemberCount.Text = $"{channel.Members.Count} members";
            }

            // Load avatar if exists
            if (!string.IsNullOrEmpty(channel.AvatarUrl))
            {
                AvatarImage.Source = ChatListPage.Client.GetFullUrl(channel.AvatarUrl);
                AvatarImage.IsVisible = true;
            }
        }
        else
        {
            await DisplayAlert("Error", "Channel not found", "OK");
            await Navigation.PopAsync();
        }
    }

    private async void OnJoinClicked(object sender, EventArgs e)
    {
        if (_isAlreadyJoined)
        {
            // Already joined - just open it
            await Navigation.PopAsync();
            // Trigger chat opening through event or navigation
            MessagingCenter.Send(this, "OpenChannel", _channelId);
        }
        else
        {
            // Join channel
            JoinButton.IsEnabled = false;
            JoinButton.Text = "Joining...";

            await ChatListPage.Client.JoinChannel(_channelId);
            await Task.Delay(500);

            await DisplayAlert("Success", "Joined channel!", "OK");
            await Navigation.PopAsync();
            MessagingCenter.Send(this, "OpenChannel", _channelId);
        }
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
