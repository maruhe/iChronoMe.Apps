using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Wallpaper.LiveWallpapers
{
    [Activity(Label = "@string/wallpaper_title_clock_analog", Name = "me.ichrono.droid.LiveWallpapers.WallpaperClockConfigActivity", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class WallpaperClockConfigActivity : AppCompatActivity
    {
        public const string SHARED_PREFS_NAME = "WallpaperClockSettings";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_wallpaper_config);

            Tools.ShowToast(this, "WallpaperConfig");
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}