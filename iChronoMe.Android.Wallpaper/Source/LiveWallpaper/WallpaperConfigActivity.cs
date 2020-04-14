
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Fragment = Android.Support.V4.App.Fragment;

namespace iChronoMe.Droid.Wallpaper.LiveWallpapers
{
    [Activity(Label = "@string/wallpaper_title_clock_analog", Name = "me.ichrono.droid.Wallpaper.WallpaperConfigActivity", Exported = true, Theme = "@style/AppTheme.NoActionBar")]//, LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class WallpaperClockConfigActivity : AppCompatActivity
    {
        public const string SHARED_PREFS_NAME = "WallpaperClockSettings";

        Fragment ActiveFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_fragment);

            ActiveFragment = new WallpaperConfigFragment();
            SupportFragmentManager.BeginTransaction()
                            .Replace(Resource.Id.flRoot, ActiveFragment)
                            .Commit();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            ActiveFragment?.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}