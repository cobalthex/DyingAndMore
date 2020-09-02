using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace DyingAndMore
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        Logo = "@drawable/logo",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class GameActivity : AndroidGameActivity
    {
        private DyingAndMoreGame game;
        private View view;

        protected override void OnCreate(Bundle bundle)
        {
            Window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;
         
            base.OnCreate(bundle);
            Takai.Data.Cache.Assets = Assets;
            game = new DyingAndMoreGame();
            view = game.Services.GetService(typeof(View)) as View;

            SetContentView(view);
            game.Run();
        }
    }
}
