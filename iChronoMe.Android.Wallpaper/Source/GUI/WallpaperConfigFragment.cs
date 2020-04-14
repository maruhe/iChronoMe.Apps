using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.Wallpaper.Controls;
using iChronoMe.Widgets;
using static iChronoMe.Droid.Wallpaper.LiveWallpapers.WallpaperClockService;

namespace iChronoMe.Droid.Wallpaper
{
    public class WallpaperConfigFragment : Fragment
    {
        ViewGroup RootView;
        ViewGroup ConfigFrame;
        AppCompatActivity mContext = null;
        FloatingActionButton fabSave;

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
            ConfigFrame = RootView.FindViewById<ViewGroup>(Resource.Id.flConfig);
            fabSave = RootView.FindViewById<FloatingActionButton>(Resource.Id.fabSave);

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
                    UpdateAllConfigLayouts();
                    preview.Start();
                });
            });
        }

        public override void OnPause()
        {
            base.OnPause();
            preview.Stop();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            engine?.OnSurfaceDestroyed(null);
        }

        private void Ul_UserChanged(object sender, EventArgs e)
        {
            if (sender is ConfigLayout && (sender as ConfigLayout).WallpaperItem != null)
            {
                var cl = sender as ConfigLayout;
                var x = cl.GetSize();

                int i = cl.ItemId;
                var cfg = WallpaperConfigHolder.GetConfig(WallpaperType.HomeScreen, sys.DisplayOrientation, true);
                cfg.Items[i].X = x.x;
                cfg.Items[i].Y = x.y;
                cfg.Items[i].Width = x.width;
                cfg.Items[i].Heigth = x.height;
                WallpaperConfigHolder.SetConfig(WallpaperType.HomeScreen, sys.DisplayOrientation, cfg);
                //engine.RefreshConfig(cfg);
                preview.Invalidate();
            }
        }

        public void UpdateAllConfigLayouts()
        {
            var cfg = WallpaperConfigHolder.GetConfig(WallpaperType.HomeScreen, sys.DisplayOrientation, true);
            int i = 0;
            ConfigFrame.RemoveAllViews();
            foreach (var item in cfg.Items)
            {
                var cl = new ConfigLayout(mContext);
                ConfigFrame.AddView(cl, new CoordinatorLayout.LayoutParams(CoordinatorLayout.LayoutParams.MatchParent, CoordinatorLayout.LayoutParams.MatchParent));
                item.ConfigLayout = cl;
                cl.WallpaperConfig = cfg;
                cl.ItemId = i;
                cl.WallpaperItem = item;
                i++;

                cl.SetSize(item.X, item.Y, item.Width, item.Heigth);
                cl.UserChanged += Ul_UserChanged;
                cl.OptionsClicked += Cl_OptionsClicked;
            }
        }

        const int menu_Background = 101;
        const int menu_HandTypes = 102;
        const int menu_ClockHandColors = 103;
        const int menu_ClockFace = 104;
        const int menu_LocationType = 111;
        const int menu_TimeType = 112;
        const int menu_Delete = 900;

        /*
                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.Background, null, cfg, typeof(WidgetCfgAssistant_ClockAnalog_Background), cfgPrev));
                    if (ClockHandConfig.Count > 1)
                        Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.HandTypes, null, cfg, typeof(WidgetCfgAssistant_ClockAnalog_HandType)));
                    cfg = BaseSample.GetConfigClone();
                    cfgPrev = BaseSample.GetConfigClone();
                    cfgPrev.ColorHourHandStroke = cfgPrev.ColorMinuteHandFill = cfgPrev.ColorMinuteHandStroke = cfgPrev.ColorMinuteHandFill =
                        cfgPrev.ColorSecondHandStroke = cfgPrev.ColorSecondHandFill = xColor.HotPink;
                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.ClockHandColors, null, cfg, typeof(WidgetCfgAssistant_ClockAnalog_HandColorType), cfgPrev));
                    //if (string.IsNullOrEmpty(cfg.BackgroundImage))
                    {
                        cfg = BaseSample.GetConfigClone();
                        cfgPrev = BaseSample.GetConfigClone();
                        cfgPrev.ColorTickMarks = xColor.HotPink;
                        Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.ClockFace, null, cfg, typeof(WidgetCfgAssistant_ClockAnalog_TickMarks), cfgPrev));
                    }
                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.LocationType, null, BaseSample.GetConfigClone(), typeof(WidgetCfgAssistant_ClockAnalog_Start)));
                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.TimeType, null, BaseSample.GetConfigClone(), typeof(WidgetCfgAssistant_ClockAnalog_WidgetTimeType)));

                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.TextColor, null, BaseSample.GetConfigClone(), typeof(WidgetCfgAssistant_ClockAnalog_TextColor)));
                    //Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.Theme, null, BaseSample.GetConfigClone(), typeof(WidgetCfgAssistant_ClockAnalog_Theme)));

                    Samples.Add(new WidgetCfgSample<WidgetCfg_ClockAnalog>(localize.ClickAction, null, BaseSample.GetConfigClone(), typeof(WidgetCfgAssistant_Universal_ClickAction<WidgetCfg_ClockAnalog>)));

        */

        View popView;

        private void Cl_OptionsClicked(object sender, EventArgs e)
        {
            Tools.ShowToast(mContext, "pop");

            ConfigLayout cl = sender as ConfigLayout;
            if (popView == null)
            {
                popView = new View(mContext);
                popView.LayoutParameters = new ViewGroup.LayoutParams(1, 1);
                popView.SetBackgroundColor(Color.Transparent);

                RootView.AddView(popView);
            }

            var x = cl.GetSize();
            popView.SetX(x.x + x.width);
            popView.SetY(x.y);

            PopupMenu popup = new PopupMenu(mContext, popView, GravityFlags.Center);
            popup.Menu.Add(1, menu_Background, 0, localize.Background);
            popup.Menu.Add(1, menu_HandTypes, 0, localize.HandTypes);
            popup.Menu.Add(1, menu_ClockHandColors, 0, localize.ClockHandColors);
            popup.Menu.Add(1, menu_ClockFace, 0, localize.ClockFace);
            popup.Menu.Add(1, menu_LocationType, 0, localize.LocationType);
            popup.Menu.Add(1, menu_TimeType, 0, localize.TimeType);
            popup.Menu.Add(900, menu_Delete, 0, localize.action_delete);

            popup.MenuItemClick += (s, e) =>
            {
                if (e.Item.ItemId == menu_Delete)
                {

                }
                else if (e.Item.ItemId == menu_TimeType)
                {
                    new AlertDialog.Builder(mContext)
                        .SetTitle(Resource.String.label_choose_default_timetype)
                        .SetAdapter(new TimeTypeAdapter(mContext), (s, e) =>
                        {
                            var tt = TimeType.RealSunTime;
                            switch (e.Which)
                            {
                                case 1:
                                    tt = TimeType.MiddleSunTime;
                                    break;
                                case 2:
                                    tt = TimeType.TimeZoneTime;
                                    break;
                            }

                            cl.WallpaperItem.ClockCfg.WidgetTimeType = cl.WallpaperItem.ClockCfg.CurrentTimeType = tt;

                        })
                        .Create().Show();
                }
                else
                {
                    Type tAssistant = null;

                    if (tAssistant != null)
                    {

                    }
                }
            };
            popup.Show();
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