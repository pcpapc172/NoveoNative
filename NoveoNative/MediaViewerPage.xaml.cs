namespace NoveoNative;

public partial class MediaViewerPage : ContentPage
{
    public MediaViewerPage(string url)
    {
        InitializeComponent();
        FullImage.Source = url;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}