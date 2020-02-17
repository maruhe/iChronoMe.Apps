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
using Xamarin.Essentials;

namespace iChronoMe.Droid.GUI
{
    public class ClockFragment : ActivityFragment, IMenuItemOnMenuItemClickListener, NavigationView.IOnNavigationItemSelectedListener
    {
        public TimeType TimeType { get; set; } = sys.DefaultTimeType;
        private DrawerLayout Drawer;
        NavigationView navigationView;
        private CoordinatorLayout coordinator;
        private TextView lTitle, lGeoPos, lTime1, lTime2, lTime3, lTimeInfo1, lTimeInfo2, lTimeInfo3;
        private ImageView imgTZ;
        private ImageView imgClockBack;
        private SKCanvasView skiaView;
        private WidgetView_ClockAnalog vClock;
        private AppCompatActivity mContext = null;
        private LocationTimeHolder lth;
        private FloatingActionButton fabTimeType;
        private WidgetAnimator_ClockAnalog animator;

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
            skiaView.PaintSurface += OnPaintSurface;

            lTitle = RootView.FindViewById<TextView>(Resource.Id.text_clock_area);
            lGeoPos = RootView.FindViewById<TextView>(Resource.Id.text_clock_location);

            RootView.FindViewById<TextView>(Resource.Id.title).Visibility = ViewStates.Gone;
            lTime1 = RootView.FindViewById<TextView>(Resource.Id.time_rdt);
            lTimeInfo1 = RootView.FindViewById<TextView>(Resource.Id.time_offset_rdt);
            lTime2 = RootView.FindViewById<TextView>(Resource.Id.time_mst);
            lTimeInfo2 = RootView.FindViewById<TextView>(Resource.Id.time_offset_mst);
            lTime3 = RootView.FindViewById<TextView>(Resource.Id.time_tzt);
            lTimeInfo3 = RootView.FindViewById<TextView>(Resource.Id.time_offset_tzt);
            imgTZ = RootView.FindViewById<ImageView>(Resource.Id.img_timezone);

            RootView.FindViewById<ImageButton>(Resource.Id.btn_locate).Click += btnLocate_Click;
            RootView.FindViewById<ImageButton>(Resource.Id.btn_animate).Click += btnAnimate_Click;
            if (!sys.Debugmode)
                RootView.FindViewById<ImageButton>(Resource.Id.btn_animate).Visibility = ViewStates.Gone;

            fabTimeType = RootView.FindViewById<FloatingActionButton>(Resource.Id.btn_time_type);
            fabTimeType.Click += Fab_Click;

            navigationView = RootView.FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            return RootView;
        }

        private void btnAnimate_Click(object sender, EventArgs e)
        {
            PopupMenu popup = new PopupMenu(Activity, sender as View);
            foreach (var style in Enum.GetValues(typeof(ClockAnalog_AnimationStyle)))
                popup.Menu.Add(0, (int)style, 0, style.ToString());
            

            popup.MenuItemClick += (s, e) =>
            {
                ClockAnalog_AnimationStyle style = (ClockAnalog_AnimationStyle)Enum.ToObject(typeof(ClockAnalog_AnimationStyle), e.Item.ItemId);

                TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
                DateTime tAnimateFrom = lth.GetTime(this.TimeType);
                DateTime tAnimateTo = tAnimateFrom.Add(tsDuriation);

                animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, style)
                    .SetStart(tAnimateFrom)
                    .SetEnd(tAnimateTo)
                    .SetPushFrame((h, m, s) =>
                    {
                        nManualHour = h;
                        nManualMinute = m;
                        nManualSecond = s;
                        mContext.RunOnUiThread(() =>
                        {
                            bNoClockUpdate = true;
                            vClock.FlowMinuteHand = true;
                            vClock.FlowSecondHand = true;
                            skiaView.Invalidate();
                        });
                    })
                    .SetLastRun((h, m, s) =>
                    {
                        nManualHour = h;
                        nManualMinute = m;
                        nManualSecond = s;

                        mContext.RunOnUiThread(() =>
                        {
                            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                            skiaView.Invalidate();
                        });
                    })
                    .SetFinally(() =>
                    {
                        mContext.RunOnUiThread(() =>
                        {
                            nManualHour = nManualMinute = nManualSecond = null;
                            bNoClockUpdate = false;
                        });
                    })
                    .StartAnimation();

            };
        
            popup.Show();
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
                            {
                                Activity.RunOnUiThread(() =>
                                {
                                    StopClockUpdates();
                                    TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
                                    DateTime tAnimateFrom = lth.GetTime(this.TimeType);
                                    lth = LocationTimeHolder.LocalInstance;
                                    nLastLatitude = lastLocation.Latitude;//to prevent standard-Animation
                                    nLastLongitude = lastLocation.Longitude;
                                    lth.ChangePositionDelay(lastLocation.Latitude, lastLocation.Longitude, false, true);
                                    DateTime tAnimateTo = lth.GetTime(this.TimeType).Add(tsDuriation);
                                    StartClockUpdates();

                                    animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.Over12)
                                    .SetStart(tAnimateFrom)
                                    .SetEnd(tAnimateTo)
                                    .SetPushFrame((h, m, s) =>
                                    {
                                        nManualHour = h;
                                        nManualMinute = m;
                                        nManualSecond = s;
                                        mContext.RunOnUiThread(() =>
                                        {
                                            bNoClockUpdate = true;
                                            vClock.FlowMinuteHand = true;
                                            vClock.FlowSecondHand = true;
                                            skiaView.Invalidate();
                                        });
                                    })
                                    .SetLastRun((h, m, s) =>
                                    {
                                        nManualHour = h;
                                        nManualMinute = m;
                                        nManualSecond = s;

                                        mContext.RunOnUiThread(() =>
                                        {
                                            vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                                            skiaView.Invalidate();
                                        });
                                    })
                                    .SetFinally(() =>
                                    {
                                        mContext.RunOnUiThread(() =>
                                        {
                                            nManualHour = nManualMinute = nManualSecond = null;
                                            bNoClockUpdate = false;                                            
                                        });
                                    })
                                    .StartAnimation();
                                });

                            }
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
                        {
                            Activity.RunOnUiThread(() =>
                            {
                                StopClockUpdates();
                                TimeSpan tsDuriation = TimeSpan.FromSeconds(2);
                                DateTime tAnimateFrom = lth.GetTime(this.TimeType);
                                lth = LocationTimeHolder.LocalInstanceClone;
                                nLastLatitude = lth.Latitude;//to prevent standard-Animation
                                nLastLongitude = lth.Longitude;
                                lth.ChangePositionDelay(sel.Latitude, sel.Longitude, true, true);
                                DateTime tAnimateTo = lth.GetTime(this.TimeType).Add(tsDuriation);
                                StartClockUpdates();

                                animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.Over12)
                                .SetStart(tAnimateFrom)
                                .SetEnd(tAnimateTo)
                                .SetPushFrame((h, m, s) =>
                                {
                                    nManualHour = h;
                                    nManualMinute = m;
                                    nManualSecond = s;
                                    mContext.RunOnUiThread(() =>
                                    {
                                        bNoClockUpdate = true;
                                        vClock.FlowMinuteHand = true;
                                        vClock.FlowSecondHand = true;
                                        skiaView.Invalidate();
                                    });
                                })
                                .SetLastRun((h, m, s) =>
                                {
                                    nManualHour = h;
                                    nManualMinute = m;
                                    nManualSecond = s;

                                    mContext.RunOnUiThread(() =>
                                    {
                                        vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                                        skiaView.Invalidate();
                                    });
                                })
                                .SetFinally(() =>
                                {
                                    mContext.RunOnUiThread(() =>
                                    {
                                        nManualHour = nManualMinute = nManualSecond = null;
                                        bNoClockUpdate = false;
                                    });
                                })
                                .StartAnimation();
                            });
                        }
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

            TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
            DateTime tAnimateFrom = lth.GetTime(this.TimeType);
            DateTime tAnimateTo = lth.GetTime(tt).Add(tsDuriation);

            SetTimeType(tt);

            animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.HandsNatural)
            .SetStart(tAnimateFrom)
            .SetEnd(tAnimateTo)
            .SetPushFrame((h, m, s) =>
            {
                nManualHour = h;
                nManualMinute = m;
                nManualSecond = s;
                mContext.RunOnUiThread(() =>
                {
                    bNoClockUpdate = true;
                    vClock.FlowMinuteHand = true;
                    vClock.FlowSecondHand = true;
                    skiaView.Invalidate();
                });
            })
            .SetLastRun((h, m, s) =>
            {
                nManualHour = h;
                nManualMinute = m;
                nManualSecond = s;

                mContext.RunOnUiThread(() =>
                {
                    vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    skiaView.Invalidate();
                });
            })
            .SetFinally(() =>
            {
                mContext.RunOnUiThread(() =>
                {
                    vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    nManualHour = nManualMinute = nManualSecond = null;
                    bNoClockUpdate = false;
                });
            })
            .StartAnimation();

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

        double nLastLatitude = 0;
        double nLastLongitude = 0;

        private void Lth_AreaChanged(object sender, AreaChangedEventArgs e)
        {
            mContext.RunOnUiThread(() =>
            {
                if (tLastClockTime == DateTime.MinValue)
                {
                    nLastLatitude = lth.Latitude;
                    nLastLongitude = lth.Longitude;
                } 
                else
                {
                    if (Xamarin.Essentials.Location.CalculateDistance(nLastLatitude, nLastLongitude, lth.Latitude, lth.Longitude, DistanceUnits.Kilometers) > 5)
                    {
                        //Animate Time-Change on Area-Change
                        animator?.AbortAnimation();

                        //Tools.ShowToast(mContext, "AreaChangedAnimation :-)");

                        TimeSpan tsDuriation = TimeSpan.FromSeconds(2);
                        DateTime tAnimateTo = lth.GetTime(this.TimeType).Add(tsDuriation);

                        animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.HandsDirect);
                        if (nManualHour != null && nManualMinute != null && nManualSecond != null)
                            animator.SetStart(nManualHour.Value, nManualMinute.Value, nManualSecond.Value);
                        else
                            animator.SetStart(tLastClockTime);
                        animator.SetEnd(tAnimateTo)
                        .SetPushFrame((h, m, s) =>
                        {
                            nManualHour = h;
                            nManualMinute = m;
                            nManualSecond = s;
                            mContext.RunOnUiThread(() =>
                            {
                                bNoClockUpdate = true;
                                vClock.FlowMinuteHand = true;
                                vClock.FlowSecondHand = true;
                                skiaView.Invalidate();
                            });
                        })
                        .SetLastRun((h, m, s) =>
                        {
                            nManualHour = h;
                            nManualMinute = m;
                            nManualSecond = s;

                            mContext.RunOnUiThread(() =>
                            {
                                vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                                skiaView.Invalidate();
                            });
                        })
                        .SetFinally(() =>
                        {
                            mContext.RunOnUiThread(() =>
                            {
                                nManualHour = nManualMinute = nManualSecond = null;
                                bNoClockUpdate = false;
                            });
                        })
                        .StartAnimation();
                    }
                }
                nLastLatitude = lth.Latitude;
                nLastLongitude = lth.Longitude;
                imgTZ.SetImageResource(MainWidgetBase.GetTimeTypeIcon(TimeType.TimeZoneTime, lth));
                lTitle.Text = lth.AreaName + (string.IsNullOrEmpty(lth.CountryName) ? string.Empty : ", " + lth.CountryName);
                if (lth.Latitude == 0 && lth.Longitude == 0)
                    lGeoPos.Text = Resources.GetString(Resource.String.unknown_position);
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
            LocationPickerDialog.NewInstance(null).Show(ChildFragmentManager, "");
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

            TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
            DateTime tAnimateFrom = DateTime.Today;
            DateTime tAnimateTo = lth.GetTime(this.TimeType).Add(tsDuriation);

            animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.HandsDirect)
            .SetStart(tAnimateFrom)
            .SetEnd(tAnimateTo)
            .SetPushFrame((h, m, s) =>
            {
                nManualHour = h;
                nManualMinute = m;
                nManualSecond = s;
                mContext.RunOnUiThread(() =>
                {
                    bNoClockUpdate = true;
                    vClock.FlowMinuteHand = true;
                    vClock.FlowSecondHand = true;
                    skiaView.Invalidate();
                });
            })
            .SetLastRun((h, m, s) =>
            {
                nManualHour = h;
                nManualMinute = m;
                nManualSecond = s;

                mContext.RunOnUiThread(() =>
                {
                    vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    skiaView.Invalidate();
                });
            })
            .SetFinally(() =>
            {
                mContext.RunOnUiThread(() =>
                {
                    vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                    nManualHour = nManualMinute = nManualSecond = null;
                    bNoClockUpdate = false;
                });
            })
            .StartAnimation();

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
                if (lth == null)
                    lth = LocationTimeHolder.LocalInstance;

                lth.AreaChanged += Lth_AreaChanged;
                Lth_AreaChanged(null, null);

                mContext.RunOnUiThread(() => fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(this.TimeType, lth)));

                lth.StartTimeChangedHandler(this, TimeType.RealSunTime, (s, e) =>
                { 
                    mContext.RunOnUiThread(() => UpdateTime(lTime1, lTimeInfo1, TimeType.RealSunTime));
                });
                lth.StartTimeChangedHandler(this, TimeType.MiddleSunTime, (s, e) =>
                {
                    mContext.RunOnUiThread(() => UpdateTime(lTime2, lTimeInfo2, TimeType.MiddleSunTime));
                });
                lth.StartTimeChangedHandler(this, TimeType.TimeZoneTime, (s, e) =>
                {
                    mContext.RunOnUiThread(() => UpdateTime(lTime3, lTimeInfo3, TimeType.TimeZoneTime));
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

        private void UpdateTime(TextView tvTime, TextView tvOffset, TimeType typeType)
        {
            if (lth == null)
                return;
            DateTime tCurrent = lth.GetTime(this.TimeType);
            DateTime tInfo = lth.GetTime(typeType);
            var tsOff = tInfo - tCurrent;
            tvTime.Text = tInfo.ToLongTimeString();
            if (sys.GetTimeWithoutMilliSeconds(tCurrent) != sys.GetTimeWithoutMilliSeconds(tInfo))
            {
                tvOffset.Text = (tCurrent > tInfo ? "-" : "+") + tsOff.ToShortString();
                double iMin = tsOff.TotalMinutes;
                if (iMin < 0) iMin *= -1;
                if (iMin < 30)
                    tvOffset.SetTextColor(xColor.MaterialLightGreen.ToAndroid());
                else if (iMin < 45)
                    tvOffset.SetTextColor(xColor.MaterialAmber.ToAndroid());
                else if (iMin < 60)
                    tvOffset.SetTextColor(xColor.MaterialOrange.ToAndroid());
                else if (iMin < 90)
                    tvOffset.SetTextColor(xColor.MaterialDeepOrange.ToAndroid());
                else
                    tvOffset.SetTextColor(xColor.MaterialRed.ToAndroid());
            }
            else
                tvOffset.Text = "";
        }

        private void StopClockUpdates()
        {
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
                        AppConfigHolder.MainConfig.DefaultTimeType = tt;
                        AppConfigHolder.SaveMainConfig();

                        TimeSpan tsDuriation = TimeSpan.FromSeconds(1);
                        DateTime tAnimateFrom = lth.GetTime(this.TimeType);
                        DateTime tAnimateTo = lth.GetTime(tt).Add(tsDuriation);

                        SetTimeType(tt);

                        animator = new WidgetAnimator_ClockAnalog(vClock, tsDuriation, ClockAnalog_AnimationStyle.HandsDirect)
                        .SetStart(tAnimateFrom)
                        .SetEnd(tAnimateTo)
                        .SetPushFrame((h, m, s) =>
                        {
                            nManualHour = h;
                            nManualMinute = m;
                            nManualSecond = s;
                            mContext.RunOnUiThread(() =>
                            {
                                bNoClockUpdate = true;
                                vClock.FlowMinuteHand = true;
                                vClock.FlowSecondHand = true;
                                skiaView.Invalidate();
                            });
                        })
                        .SetLastRun((h, m, s) =>
                        {
                            nManualHour = h;
                            nManualMinute = m;
                            nManualSecond = s;

                            mContext.RunOnUiThread(() =>
                            {
                                vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                                skiaView.Invalidate();
                            });
                        })
                        .SetFinally(() =>
                        {
                            mContext.RunOnUiThread(() =>
                            {
                                vClock.ReadConfig(AppConfigHolder.MainConfig.MainClock);
                                nManualHour = nManualMinute = nManualSecond = null;
                                bNoClockUpdate = false;
                            });
                        })
                        .StartAnimation();



                    })
                    .Create().Show();
            }
            else if (id == Resource.Id.clock_Colors)
            {
                var mgr = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(mContext, null);

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
                var mgr = new WidgetConfigAssistantManager<WidgetCfg_ClockAnalog>(mContext, null);

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

        double? nManualHour = null;
        double? nManualMinute = null;
        double? nManualSecond = null;
        DateTime tLastClockTime = DateTime.MinValue;

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            try
            {
                if (nManualSecond == null)
                {
                    tLastClockTime = lth.GetTime(this.TimeType);
                    vClock.DrawCanvas(e.Surface.Canvas, tLastClockTime, (int)e.Info.Width, (int)e.Info.Height, false);
                }
                else
                    vClock.DrawCanvas(e.Surface.Canvas, nManualHour.Value, nManualMinute.Value, nManualSecond.Value, (int)e.Info.Width, (int)e.Info.Height, false);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}