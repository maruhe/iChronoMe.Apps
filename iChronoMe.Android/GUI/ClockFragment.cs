using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using SkiaSharp.Views.Android;
using System.Threading;
using SkiaSharp;
using Android.Support.V7.App;
using iChronoMe.Widgets;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Core;
using System.Threading.Tasks;
using Android.Locations;
using Android.Support.Design.Widget;
using iChronoMe.Droid.Widgets;
using iChronoMe.Core.Classes;
using Android.Support.V4.Widget;
using iChronoMe.Droid.Adapters;
using iChronoMe.Core.DynamicCalendar;
using System.IO;
using Xamarin.Essentials;
using iChronoMe.Core.Types;
using System.Net;
using System.Drawing;

namespace iChronoMe.Droid.GUI
{
    public class ClockFragment : ActivityFragment, IMenuItemOnMenuItemClickListener, NavigationView.IOnNavigationItemSelectedListener
    {
        public TimeType TimeType { get; set; } = AppConfigHolder.MainConfig.DefaultTimeType;
        private DrawerLayout Drawer;
        NavigationView navigationView;
        private CoordinatorLayout coordinator;
        private TextView lTitle, lGeoPos, lTime1, lTime2, lTime3, lTimeInfo1, lTimeInfo2, lTimeInfo3;
        private SKCanvasView skiaView;
        private WidgetView_ClockAnalog vClock;
        private AppCompatActivity mContext = null;
        private LocationTimeHolder lth;
        private FloatingActionButton fabTimeType;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            mContext = (AppCompatActivity)container.Context;

            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_clock, container, false);
            coordinator = RootView.FindViewById<CoordinatorLayout>(Resource.Id.coordinator_layout);
            Drawer = RootView.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            skiaView = RootView.FindViewById<SKCanvasView>(Resource.Id.skia_clock);
            lTitle = RootView.FindViewById<TextView>(Resource.Id.text_clock_area);
            lGeoPos = RootView.FindViewById<TextView>(Resource.Id.text_clock_location);
            lTime1 = RootView.FindViewById<TextView>(Resource.Id.text_time1);
            lTime2 = RootView.FindViewById<TextView>(Resource.Id.text_time2);
            lTime3 = RootView.FindViewById<TextView>(Resource.Id.text_time3);
            lTimeInfo1 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo1);
            lTimeInfo2 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo2);
            lTimeInfo3 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo3);

            fabTimeType = RootView.FindViewById<FloatingActionButton>(Resource.Id.btn_time_type);
            fabTimeType.Click += Fab_Click;

            navigationView = RootView.FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            return RootView;
        }

        bool isFABOpen = false;
        private void Fab_Click(object sender, EventArgs e)
        {
            if (!isFABOpen)
            {
                showFABMenu();
            }
            else
            {
                closeFABMenu();
            }
        }

        List<FloatingActionButton> fabs;

        private void showFABMenu()
        {
            isFABOpen = true;
            List<TimeType> menu = new List<TimeType>(new TimeType[] { TimeType.TimeZoneTime, TimeType.MiddleSunTime, TimeType.RealSunTime });
            menu.Remove(this.TimeType);

            int margin = (int)Resources.GetDimension(Resource.Dimension.fab_margin);
            var lp = new CoordinatorLayout.LayoutParams(CoordinatorLayout.LayoutParams.WrapContent, CoordinatorLayout.LayoutParams.WrapContent);
            lp.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
            lp.SetMargins(margin, margin, margin, margin);
            
            float fAnimate = Resources.GetDimension(Resource.Dimension.standard_60);

            fabs = new List<FloatingActionButton>();
            foreach (TimeType tt in menu)
            {
                var fab = new FloatingActionButton(mContext);
                fab.SetImageResource(MainWidgetBase.GetTimeTypeIcon(tt, lth));
                fab.Tag = new Java.Lang.String(tt.ToString());
                fab.Click += FabMenu_Click;
                coordinator.AddView(fab, lp);
                fabs.Add(fab);

                fab.Animate().TranslationY(-(fAnimate));
                fAnimate += Resources.GetDimension(Resource.Dimension.standard_60);
            }
            fabTimeType.BringToFront();
        }

        private void FabMenu_Click(object sender, EventArgs e)
        {
            try
            {
                string tag = (string)(sender as FloatingActionButton).Tag;
                var tt = Enum.Parse<TimeType>(tag);
                SetTimeType(tt);
            }
            finally
            {
                closeFABMenu();
            }
        }

        private void closeFABMenu()
        {
            isFABOpen = false;
            if (fabs == null)
                return;
            foreach(var fab in fabs)
            {
                fab.Animate().TranslationY(0).WithEndAction(new Java.Lang.Runnable(() => { coordinator.RemoveView(fab); }));
            }            
        }

        DateTime tClockTime = DateTime.MinValue;
        private void Lth_TimeChanged(object sender, TimeChangedEventArgs e)
        {
            mContext.RunOnUiThread(() =>
            {
                DateTime tCurrent = lth.GetTime(this.TimeType);
                DateTime tReal = lth.GetTime(TimeType.RealSunTime);
                DateTime tMiddle = lth.GetTime(TimeType.MiddleSunTime);
                DateTime tZone = lth.GetTime(TimeType.TimeZoneTime);
                if (lth.Latitude == 0 && lth.Longitude == 0)
                    lGeoPos.Text = "unknown position";
                else
                {
                    lGeoPos.Text = sys.DezimalGradToGrad(lth.Latitude, lth.Longitude) + "\nGMT " + lth.TimeZoneOffsetGmt.ToString("+#;-#;0");
                    if (lth.TimeZoneOffset != lth.TimeZoneOffsetGmt)
                        lGeoPos.Text += "\nDST " + lth.TimeZoneOffset.ToString("+#;-#;0");
                }

                lTime1.Text = TimeType.RealSunTime.ToString()+":";
                lTimeInfo1.Text = tReal.ToLongTimeString();
                if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tReal))
                    lTimeInfo1.Text += "\t(" + (tCurrent > tReal ? "-" : "+") + (tReal - tCurrent).ToString(@"mm\:ss") + ")";

                lTime2.Text = TimeType.MiddleSunTime.ToString() + ":";
                lTimeInfo2.Text = tMiddle.ToLongTimeString();
                if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tMiddle))
                    lTimeInfo2.Text += "\t(" + (tCurrent > tMiddle ? "-" : "+") + (tMiddle - tCurrent).ToString(@"mm\:ss") + ")";

                lTime3.Text = TimeType.TimeZoneTime.ToString() + ":";
                lTimeInfo3.Text = tZone.ToLongTimeString();
                if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tZone))
                    lTimeInfo3.Text += "\t(" + (tCurrent > tZone ? "-" : "+") + (tZone - tCurrent).ToString(@"mm\:ss") + ")";

                tCurrent = sys.GetTimeWithoutMilliSeconds(tCurrent);
                if (tClockTime != tCurrent)
                {
                    skiaView.Invalidate();
                    tClockTime = tCurrent;
                }
            });
        }
        private void Lth_AreaChanged(object sender, AreaChangedEventArgs e)
        {
            mContext.RunOnUiThread(() => lTitle.Text = lth.AreaName + ", " + lth.CountryName);
        }

        public void SetTimeType(TimeType tt)
        {
            this.TimeType = tt;
            fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth));
            lth.Stop();
            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
            Lth_TimeChanged(this, new TimeChangedEventArgs(TimeChangedFlag.TimeSourceUpdate));
            lth.Start(true, true, true);// this.TimeType == TimeType.RealSunTime, this.TimeType == TimeType.MiddleSunTime, this.TimeType == TimeType.TimeZoneTime || this.TimeType == TimeType.UtcTime);
        }

        private void BtnMaps_Click(object sender, EventArgs e)
        {
            LocationPickerDialog.NewInstance(null).Show(ChildFragmentManager, "lala");            
        }

        public override void OnResume()
        {
            base.OnResume();

            vClock = new WidgetView_ClockAnalog();
            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);

            lth = LocationTimeHolder.LocalInstance;
            fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth));
            skiaView.PaintSurface += OnPaintSurface;

            lth.TimeChanged += Lth_TimeChanged;
            lth.AreaChanged += Lth_AreaChanged;
            lth.Start(true, true, true);// this.TimeType == TimeType.RealSunTime, this.TimeType == TimeType.MiddleSunTime, this.TimeType == TimeType.TimeZoneTime || this.TimeType == TimeType.UtcTime);
            Task.Factory.StartNew(() =>
            {
                var locationManager = (LocationManager)Context.GetSystemService(Context.LocationService);

                var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                if (lastLocation == null)
                    lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                if (lastLocation != null)
                    lth.ChangePositionDelay(lastLocation.Latitude, lastLocation.Longitude, true);
            });
        }

        public override void OnPause()
        {
            base.OnPause();

            skiaView.PaintSurface -= OnPaintSurface;
            lth.TimeChanged -= Lth_TimeChanged;
            lth.AreaChanged -= Lth_AreaChanged;
            lth.Stop();
        }

        public override void Reinit()
        {
            base.Reinit();
            SetTimeType(AppConfigHolder.MainConfig.DefaultTimeType);
            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            try
            {
                SetTimeType(this.TimeType);
            } catch { }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        const int menu_options = 1001;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            var item = menu.Add(0, menu_options, 1, "Options");
            item.SetIcon(Resource.Drawable.icons8_services);
            item.SetShowAsAction(ShowAsAction.Always);
            item.SetOnMenuItemClickListener(this);
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            if (item.ItemId == menu_options)
            {
                if (Drawer.IsDrawerOpen((int)GravityFlags.Right))
                    Drawer.CloseDrawer((int)GravityFlags.Right);
                else
                    Drawer.OpenDrawer((int)GravityFlags.Right);
            }

            return true;
        }
        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            return OnNavigationItemSelected(menuItem.ItemId);
        }

        public bool OnNavigationItemSelected(int id)
        {
            if (id == Resource.Id.clock_TimeType)
            {
                new AlertDialog.Builder(mContext)
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
                        AppConfigHolder.MainConfig.DefaultTimeType = tt;
                        AppConfigHolder.SaveMainConfig();
                        SetTimeType(tt);
                    })
                    .Create().Show();
            }
            else if (id == Resource.Id.clock_Colors)
            {
                var mgr = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(mContext);

                Task.Factory.StartNew(async () => {
                    var cfg = await mgr.StartAt(typeof(WidgetCfgAssistant_ClockAnalog_HandColorType), AppConfigHolder.MainConfig.MainClock);
                    if (cfg != null)
                    {
                        AppConfigHolder.MainConfig.MainClock = cfg.GetConfigClone();
                        AppConfigHolder.SaveMainConfig();
                        vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    }
                });
            }
            else if (id == Resource.Id.clock_Background)
            {
                var mgr = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(mContext);

                Task.Factory.StartNew(async () => {
                    var cfg = await mgr.StartAt(typeof(WidgetCfgAssistant_ClockAnalog_BackgroundImage), AppConfigHolder.MainConfig.MainClock, new List<Type>(new Type[] { typeof(WidgetCfgAssistant_ClockAnalog_HandColorType)}));
                    if (cfg != null)
                    {
                        AppConfigHolder.MainConfig.MainClock = cfg.GetConfigClone();
                        AppConfigHolder.SaveMainConfig();
                        vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    }
                });
            }
            unCheckAllMenuItems(navigationView.Menu);
            Drawer.CloseDrawer((int)GravityFlags.Right);
            return true;
        }

        private void unCheckAllMenuItems(IMenu menu)
        {
            int size = menu.Size();
            for (int i = 0; i < size; i++)
            {
                IMenuItem item = menu.GetItem(i);
                if (item.HasSubMenu)
                {
                    // Un check sub menu items
                    unCheckAllMenuItems(item.SubMenu);
                }
                else
                {
                    item.SetChecked(false);
                }
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            vClock.DrawCanvas(e.Surface.Canvas, lth.GetTime(this.TimeType), (int)e.Info.Width, (int)e.Info.Height, true);
        }
    }
}