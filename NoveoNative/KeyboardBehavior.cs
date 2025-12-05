using Microsoft.Maui.Controls;

namespace NoveoNative;

public class KeyboardBehavior : Behavior<ContentPage>
{
    protected override void OnAttachedTo(ContentPage page)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
#if __ANDROID__
            Android.App.Activity? activity = Platform.CurrentActivity;
            if (activity != null)
            {
                activity.Window?.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
            }
#endif
        }
        base.OnAttachedTo(page);
    }
}
