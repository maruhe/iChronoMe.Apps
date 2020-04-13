using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Wallpaper.Controls;
using iChronoMe.Droid.Wallpaper.LiveWallpapers;
using static iChronoMe.Droid.Wallpaper.LiveWallpapers.WallpaperClockService;
using Fragment = Android.Support.V4.App.Fragment;

namespace iChronoMe.Droid.Wallpaper
{
    [Activity(Label = "@string/app_name", Theme = "@style/splashscreen", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Fragment ActiveFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
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

