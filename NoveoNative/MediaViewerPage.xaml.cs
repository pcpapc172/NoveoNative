namespace NoveoNative;

public partial class MediaViewerPage : ContentPage
{
    private double _currentScale = 1;

    public MediaViewerPage(string imageUrl)
    {
        InitializeComponent();
        FullImage.Source = imageUrl;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Running)
        {
            _currentScale += (e.Scale - 1) * _currentScale;
            _currentScale = Math.Max(1, _currentScale);
            _currentScale = Math.Min(_currentScale, 10);

            FullImage.Scale = _currentScale;
        }
    }
}
