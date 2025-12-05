using Foundation;
using UIKit;

namespace NoveoNative;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Set status bar to light content (white text/icons)
        UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.LightContent, false);

        return base.FinishedLaunching(application, launchOptions);
    }
}
