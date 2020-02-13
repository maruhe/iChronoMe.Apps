using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Droid.Widgets;
using iChronoMe.Widgets;

using SkiaSharp.Views.Android;

namespace iChronoMe.Droid.GUI
{
    public class ClockFragment : ActivityFragment, IMenuItemOnMenuItemClickListener, NavigationView.IOnNavigationItemSelectedListener
    {
        public TimeType TimeType { get; set; } = sys.DefaultTimeType;
        private DrawerLayout Drawer;
        NavigationView navigationView;
        private CoordinatorLayout coordinator;
        private TextView lTitle, lGeoPos, lTime1, lTime2, lTime3, lTimeInfo1, lTimeInfo2, lTimeInfo3;
        private ImageView imgClockBack;
        private SKCanvasView skiaView;
        private WidgetView_ClockAnalog vClock;
        private AppCompatActivity mContext = null;
        private LocationTimeHolder lth = LocationTimeHolder.LocalInstanceClone;
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
            imgClockBack = RootView.FindViewById<ImageView>(Resource.Id.img_clock_background);
            skiaView = RootView.FindViewById<SKCanvasView>(Resource.Id.skia_clock);
            lTitle = RootView.FindViewById<TextView>(Resource.Id.text_clock_area);
            lGeoPos = RootView.FindViewById<TextView>(Resource.Id.text_clock_location);
            lTime1 = RootView.FindViewById<TextView>(Resource.Id.text_time1);
            lTime2 = RootView.FindViewById<TextView>(Resource.Id.text_time2);
            lTime3 = RootView.FindViewById<TextView>(Resource.Id.text_time3);
            lTimeInfo1 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo1);
            lTimeInfo2 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo2);
            lTimeInfo3 = RootView.FindViewById<TextView>(Resource.Id.text_timeinfo3);

            RootView.FindViewById<ImageButton>(Resource.Id.btn_locate).Click += btnLocate_Click; ;

            fabTimeType = RootView.FindViewById<FloatingActionButton>(Resource.Id.btn_time_type);
            fabTimeType.Click += Fab_Click;

            navigationView = RootView.FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            return RootView;
        }

        private void btnLocate_Click(object sender, EventArgs e)
        {
            PopupMenu popup = new PopupMenu(Activity, sender as View);
            popup.Menu.Add(0, 1, 0, Resource.String.action_refresh_location);
            popup.Menu.Add(0, 2, 0, Resource.String.action_select_location);

            popup.MenuItemClick += (s, e) =>
            {
                if (e.Item.ItemId == 1)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var locationManager = (LocationManager)Context.GetSystemService(Context.LocationService);

                            var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                            if (lastLocation == null)
                                lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                            if (lastLocation != null)
                                lth.ChangePositionDelay(lastLocation.Latitude, lastLocation.Longitude, false, true);
                        }
                        catch { }
                    });
                }
                else if (e.Item.ItemId == 2)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        var sel = await LocationPickerDialog.SelectLocation((AppCompatActivity)Activity);
                        if (sel != null)
                            lth.ChangePositionDelay(sel.Latitude, sel.Longitude, true, true);
                    });
                }
            };

            popup.Show();
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
            string tag = (string)(sender as FloatingActionButton).Tag;
            var tt = Enum.Parse<TimeType>(tag);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
                    DateTime tStart = DateTime.Now;
                    DateTime tStop = DateTime.Now.Add(tsDuriation);

                    DateTime tAnimateFrom = lth.GetTime(this.TimeType);
                    DateTime tAnimateTo = sys.GetTimeWithoutMilliSeconds(lth.GetTime(tt).Add(tsDuriation)); //=> Second hand stops animation on full second
                    TimeSpan tsAnimateWay = tAnimateTo - tAnimateFrom;
                    int iDestSecond = tAnimateTo.Second;
                    if (!vClock.FlowMinuteHand)
                        tAnimateTo = sys.GetTimeWithoutSeconds(tAnimateTo);

                    mContext.RunOnUiThread(() => SetTimeType(tt));

                    while (DateTime.Now < tStop)
                    {
                        tClockTimeOverride = tAnimateFrom.AddMilliseconds(tsAnimateWay.TotalMilliseconds / tsDuriation.TotalMilliseconds * (DateTime.Now - tStart).TotalMilliseconds);
                        nSecondHandOverrideSecond = (float)(tAnimateFrom.Second + (iDestSecond - tAnimateFrom.Second) / tsDuriation.TotalMilliseconds * (DateTime.Now - tStart).TotalMilliseconds);
                        xLog.Verbose("Set tClockTimeOverride: " + tClockTimeOverride.ToLongTimeString());
                        mContext.RunOnUiThread(() =>
                        {
                            bNoClockUpdate = true;
                            vClock.FlowMinuteHand = true;
                            vClock.FlowSecondHand = true;
                            skiaView.Invalidate();
                        });
                        Task.Delay(1000 / 60).Wait();
                    }
                    tClockTimeOverride = sys.GetTimeWithoutSeconds(tAnimateTo).AddSeconds(iDestSecond);
                    mContext.RunOnUiThread(() =>
                    {
                        vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                        skiaView.Invalidate();
                    });
                }
                catch (Exception ex)
                {
                    xLog.Error(ex);
                }
                finally
                {
                    tClockTimeOverride = DateTime.MinValue;
                    nSecondHandOverrideSecond = null;
                    bNoClockUpdate = false;
                    mContext.RunOnUiThread(() => SetTimeType(tt));
                }
            });
            closeFABMenu();
        }

        private void closeFABMenu()
        {
            isFABOpen = false;
            if (fabs == null)
                return;
            foreach (var fab in fabs)
            {
                fab.Animate().TranslationY(0).WithEndAction(new Java.Lang.Runnable(() => { coordinator.RemoveView(fab); }));
            }
        }

        private void Lth_AreaChanged(object sender, AreaChangedEventArgs e)
        {
            mContext.RunOnUiThread(() =>
            {
                lTitle.Text = lth.AreaName + (string.IsNullOrEmpty(lth.CountryName) ? string.Empty : ", " + lth.CountryName);
                if (lth.Latitude == 0 && lth.Longitude == 0)
                    lGeoPos.Text = "unknown position";
                else
                {
                    lGeoPos.Text = sys.DezimalGradToGrad(lth.Latitude, lth.Longitude) + "\nGMT " + lth.TimeZoneOffsetGmt.ToString("+#;-#;0");
                    if (lth.TimeZoneOffset != lth.TimeZoneOffsetGmt)
                        lGeoPos.Text += "\nDST " + lth.TimeZoneOffset.ToString("+#;-#;0");
                }
            });
        }

        public void SetTimeType(TimeType tt)
        {
            StopClockUpdates();
            this.TimeType = tt;
            StartClockUpdates();
        }

        private void BtnMaps_Click(object sender, EventArgs e)
        {
            LocationPickerDialog.NewInstance(null).Show(ChildFragmentManager, "lala");
        }

        public override void OnResume()
        {
            base.OnResume();

            StartClockUpdates();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var locationManager = (LocationManager)Context.GetSystemService(Context.LocationService);

                    var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                    if (lastLocation == null)
                        lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                    if (lastLocation != null)
                        lth.ChangePositionDelay(lastLocation.Latitude, lastLocation.Longitude, false, true);
                }
                catch { }
            });
        }

        public override void OnPause()
        {
            base.OnPause();
            StopClockUpdates();
        }

        bool bNoClockUpdate = false;
        private void StartClockUpdates()
        {
            try
            {
                bNoClockUpdate = false;
                if (vClock == null)
                    vClock = new WidgetView_ClockAnalog();
                RefreshClockCfg();

                skiaView.PaintSurface += OnPaintSurface;

                lth.AreaChanged += Lth_AreaChanged;
                Lth_AreaChanged(null, null);

                mContext.RunOnUiThread(() => fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth)));

                string cTsFormat = sys.Debugmode ? @"mm\:ss\.fff" : @"mm\:ss";

                lth.StartTimeChangedHandler(this, TimeType.RealSunTime, (s, e) =>
                {
                    mContext.RunOnUiThread(() =>
                    {
                        DateTime tCurrent = lth.GetTime(this.TimeType);
                        DateTime tInfo = lth.GetTime(TimeType.RealSunTime);
                        lTime1.Text = TimeType.RealSunTime.ToString() + ":";
                        lTimeInfo1.Text = tInfo.ToLongTimeString();
                        if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
                            lTimeInfo1.Text += "\t(" + (tCurrent > tInfo ? "-" : "+") + (tInfo - tCurrent).ToString(cTsFormat) + ")";
                    });
                });
                lth.StartTimeChangedHandler(this, TimeType.MiddleSunTime, (s, e) =>
                {
                    mContext.RunOnUiThread(() =>
                    {
                        DateTime tCurrent = lth.GetTime(this.TimeType);
                        DateTime tInfo = lth.GetTime(TimeType.MiddleSunTime);
                        lTime2.Text = TimeType.MiddleSunTime.ToString() + ":"; xColor.FromUint((uint)lTime2.CurrentTextColor).HexString.ToString();
                        lTimeInfo2.Text = tInfo.ToLongTimeString();
                        if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
                            lTimeInfo2.Text += "\t(" + (tCurrent > tInfo ? "-" : "+") + (tInfo - tCurrent).ToString(cTsFormat) + ")";
                    });
                });
                lth.StartTimeChangedHandler(this, TimeType.TimeZoneTime, (s, e) =>
                {
                    mContext.RunOnUiThread(() =>
                    {
                        DateTime tCurrent = lth.GetTime(this.TimeType);
                        DateTime tInfo = lth.GetTime(TimeType.TimeZoneTime);
                        lTime3.Text = TimeType.TimeZoneTime.ToString() + ":";
                        lTimeInfo3.Text = tInfo.ToLongTimeString();
                        if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
                            lTimeInfo3.Text += "\t(" + (tCurrent > tInfo ? "-" : "+") + (tInfo - tCurrent).ToString(cTsFormat) + ")";
                    });
                });

                lth.StartTimeChangedHandler(skiaView, this.TimeType, (s, e) =>
                {
                    if (bNoClockUpdate)
                        return;

                    mContext.RunOnUiThread(() =>
                    {
                        skiaView.Invalidate();
                    });
                });
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void StopClockUpdates()
        {
            skiaView.PaintSurface -= OnPaintSurface;
            lth.AreaChanged -= Lth_AreaChanged;
            lth.StopTimeChangedHandler(this);
            lth.StopTimeChangedHandler(skiaView);
        }

        const int menu_options = 1001;

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

            var item = menu.Add(0, menu_options, 1, Resources.GetString(Resource.String.action_options));
            //var icon = VectorDrawableCompat.Create(Activity.Resources, Resource.Drawable.icons8_alarm_3, Activity.Theme);
            item.SetIcon(Resource.Drawable.icons8_view_quilt);
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

                Task.Factory.StartNew(async () =>
                {
                    var cfg = await mgr.StartAt(typeof(WidgetCfgAssistant_ClockAnalog_HandColorType), AppConfigHolder.MainConfig.MainClock);
                    if (cfg != null)
                    {
                        AppConfigHolder.MainConfig.MainClock = cfg.GetConfigClone();
                        AppConfigHolder.SaveMainConfig();
                        RefreshClockCfg();
                    }
                });
            }
            else if (id == Resource.Id.clock_Background)
            {
                var mgr = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(mContext);

                Task.Factory.StartNew(async () =>
                {
                    var cfg = await mgr.StartAt(typeof(WidgetCfgAssistant_ClockAnalog_BackgroundImage), AppConfigHolder.MainConfig.MainClock, new List<Type>(new Type[] { typeof(WidgetCfgAssistant_ClockAnalog_HandColorType) }));
                    if (cfg != null)
                    {
                        AppConfigHolder.MainConfig.MainClock = cfg.GetConfigClone();
                        AppConfigHolder.SaveMainConfig();
                        RefreshClockCfg();
                    }
                });
            }
            unCheckAllMenuItems(navigationView.Menu);
            Drawer.CloseDrawer((int)GravityFlags.Right);
            return true;
        }

        private void RefreshClockCfg()
        {
            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
            try
            {
                if (string.IsNullOrEmpty(AppConfigHolder.MainConfig.MainClock.BackgroundImage) || !System.IO.File.Exists(AppConfigHolder.MainConfig.MainClock.BackgroundImage))
                    imgClockBack.SetImageBitmap(null);
                else
                    imgClockBack.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(AppConfigHolder.MainConfig.MainClock.BackgroundImage)));
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
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

        DateTime tClockTimeOverride = DateTime.MinValue;
        float? nSecondHandOverrideSecond = null;
        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            try
            {
                vClock.DrawCanvas(e.Surface.Canvas, tClockTimeOverride == DateTime.MinValue ? lth.GetTime(this.TimeType) : tClockTimeOverride, (int)e.Info.Width, (int)e.Info.Height, false, nSecondHandOverrideSecond);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}