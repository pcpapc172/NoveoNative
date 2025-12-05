namespace NoveoNative;

public partial class MediaViewerPage : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;

    public MediaViewerPage(string url)
    {
        InitializeComponent();
        FullImage.Source = url;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = _currentScale;
                FullImage.AnchorX = 0;
                FullImage.AnchorY = 0;
                break;

            case GestureStatus.Running:
                _currentScale = Math.Max(1, Math.Min(_startScale * e.Scale, 10));
                FullImage.Scale = _currentScale;
                break;

            case GestureStatus.Completed:
                _xOffset = FullImage.TranslationX;
                _yOffset = FullImage.TranslationY;
                break;
        }
    }
}
