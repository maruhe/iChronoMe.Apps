using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Wallpaper.Controls;
using static iChronoMe.Droid.Wallpaper.LiveWallpapers.WallpaperClockService;

namespace iChronoMe.Droid.Wallpaper
{
    public class WallpaperConfigFragment : Fragment
    {
        public ViewGroup RootView { get; protected set; }
        public AppCompatActivity mContext { get; private set; } = null;
        
        WallpaperPreView preview;
        WallpaperClockEngine engine;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_wallpaper_config, container, false);
            
            preview = RootView.FindViewById<WallpaperPreView>(Resource.Id.preview);

            UserLayout ul = RootView.FindViewById<UserLayout>(Resource.Id.ulClockSize);
            ul.SetSize(50, 50, 400, 400);
            ul.UserChanged += Ul_UserChanged;

            return RootView;
        }

        public override void OnAttach(Android.Content.Context context)
        {
            base.OnAttach(context);
            if (context is AppCompatActivity)
                mContext = context as AppCompatActivity;
        }

        public override void OnResume()
        {
            base.OnResume();

            if (Activity is AppCompatActivity)
                mContext = Activity as AppCompatActivity;

            this.Activity?.InvalidateOptionsMenu();

            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                mContext.RunOnUiThread(() =>
                {
                    if (engine == null)
                    {
                        engine = new WallpaperClockEngine(mContext);
                    }
                    engine.OnSurfaceChanged(null, Android.Graphics.Format.Rgba8888, preview.Width, preview.Height);
                    preview.WallpaperClockEngine = engine;
                    preview.Invalidate();
                    RootView.FindViewById(Resource.Id.progress).Visibility = ViewStates.Gone;
                });
            });
        }

        private void Ul_UserChanged(object sender, EventArgs e)
        {
            if (sender is UserLayout)
            {
                var x = (sender as UserLayout).GetSize();
                var cfg = WallpaperConfigHolder.GetConfig(WallpaperType.HomeScreen, sys.DisplayOrientation, true);
                cfg.Items[0].X = x.x;
                cfg.Items[0].Y = x.y;
                cfg.Items[0].Width = x.width;
                cfg.Items[0].Heigth = x.height;
                WallpaperConfigHolder.SetConfig(WallpaperType.HomeScreen, sys.DisplayOrientation, cfg);
                //engine.RefreshConfig(cfg);
                preview.Invalidate();
            }
        }

        public Task<bool> RequestPermissionsTask { get { return tcsRP == null ? Task.FromResult(false) : tcsRP.Task; } }
        private TaskCompletionSource<bool> tcsRP = null;

        protected async Task<bool> RequestPermissionsAsync(string[] permissions, int requestCode)
        {
            tcsRP = new TaskCompletionSource<bool>();
            RequestPermissions(permissions, requestCode);
            await RequestPermissionsTask;
            return RequestPermissionsTask.Result;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            var res = true;
            foreach (var grand in grantResults)
                res = res && grand == Permission.Granted;
            tcsRP?.TrySetResult(res);
        }
    }
}