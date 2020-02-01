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
        private TextView lTitle, lTime;
        private SKCanvasView skiaView;
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
            lTime = RootView.FindViewById<TextView>(Resource.Id.text_clock_time);

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
        private Point wSize;

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

        private void Lth_TimeChanged()
        {
            mContext.RunOnUiThread(() =>
            {
                lTime.Text = lth.GetTime(this.TimeType).ToLongTimeString() + " (" + this.TimeType.ToString() + ")";
                skiaView.Invalidate();
            });
        }
        private void Lth_AreaChanged()
        {
            mContext.RunOnUiThread(() => lTitle.Text = lth.AreaName + ", " + lth.CountryName);
        }

        public void SetTimeType(TimeType tt)
        {
            this.TimeType = tt;
            fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth));
            lth.Stop();
            Lth_TimeChanged();
            lth.Start(this.TimeType == TimeType.RealSunTime, this.TimeType == TimeType.MiddleSunTime, this.TimeType == TimeType.TimeZoneTime || this.TimeType == TimeType.UtcTime);
        }

        private void BtnMaps_Click(object sender, EventArgs e)
        {
            LocationPickerDialog.NewInstance(null).Show(ChildFragmentManager, "lala");            
        }

        public override void OnResume()
        {
            base.OnResume();

            lth = new LocationTimeHolder(0, 0);
            fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth));
            skiaView.PaintSurface += OnPaintSurface;

            lth.TimeChanged += Lth_TimeChanged;
            lth.AreaChanged += Lth_AreaChanged;
            lth.Start(this.TimeType == TimeType.RealSunTime, this.TimeType == TimeType.MiddleSunTime, this.TimeType == TimeType.TimeZoneTime || this.TimeType == TimeType.UtcTime);
            Task.Factory.StartNew(() =>
            {
                var locationManager = (LocationManager)Context.GetSystemService(Context.LocationService);

                var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                if (lastLocation == null)
                    lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                if (lastLocation != null)
                    lth.ChangePosition(lastLocation.Latitude, lastLocation.Longitude, true);                    
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

        public override void Refresh()
        {
            base.Refresh();
        }
        public override void Reinit()
        {
            base.Reinit();
            SetTimeType(AppConfigHolder.MainConfig.DefaultTimeType);
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
            WidgetView_ClockAnalog v = new WidgetView_ClockAnalog();
            v.ReadConfig(AppConfigHolder.MainConfig.MainClock);
            var canvas = e.Surface.Canvas;
            var scale = Resources.DisplayMetrics.Density;
            var scaledSize = new SKSize(e.Info.Width / scale, e.Info.Height / scale);
            //canvas.Scale(scale);
            v.DrawCanvas(canvas, lth.GetTime(this.TimeType), (int)e.Info.Width, (int)e.Info.Height, true);
        }
    }
}