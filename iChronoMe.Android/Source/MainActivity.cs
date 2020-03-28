using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Calendar;
using iChronoMe.Droid.GUI.Debug;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Droid.GUI.Service;
using iChronoMe.Widgets;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;

namespace iChronoMe.Droid
{
    [Activity(Label = "@string/app_name", Name = "me.ichrono.droid.MainActivity", Theme = "@style/splashscreen", MainLauncher = true, ScreenOrientation = ScreenOrientation.FullUser, LaunchMode = LaunchMode.SingleTask)]
    [MetaData("android.app.shortcuts", Resource = "@xml/shortcuts")]
    public class MainActivity : BaseActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        int iNavigationItem = Resource.Id.nav_clock;
        bool bNavDoneByCreate = false;
        ImageButton btnNavPin;
        CoordinatorLayout mainAppBar;
        View vSpaceHolder;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
#if DEBUG
                //Task.Delay(5000).Wait();
#endif
                LoadAppTheme();
                SetContentView(Resource.Layout.activity_main);

                Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                SetSupportActionBar(Toolbar);

                Drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
                Drawer.SetScrimColor(base.Resources.GetColor(Resource.Color.navigationScrim, Theme));
                DrawerToggle = new ActionBarDrawerToggle(this, Drawer, Toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
                Drawer.AddDrawerListener(DrawerToggle);                

                NavigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
                NavigationView.SetNavigationItemSelectedListener(this);

                if (Intent.HasExtra("NavigationItem"))
                    iNavigationItem = Intent.GetIntExtra("NavigationItem", iNavigationItem);

                if (savedInstanceState != null)
                {
                    iNavigationItem = savedInstanceState.GetInt("NavigationItem", iNavigationItem);
                    blRestoreFragment = savedInstanceState.GetBundle("ActiveFragment");
                }

                mainAppBar = FindViewById<CoordinatorLayout>(Resource.Id.main_app_bar);
                vSpaceHolder = FindViewById(Resource.Id.v_placeholder);

                ProcessNavigationChange(iNavigationItem);
                bNavDoneByCreate = true;
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(2500).Wait();

                    try
                    {
                        StatisticAppStart();
                    }
                    catch { }

                    if (NeedsStartAssistant())
                        return;

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.M && (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted))
                    {
                        ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
                    }

                    try
                    {
                        BackgroundService.RestartService(this, AppWidgetManager.ActionAppwidgetUpdate);
                    }
                    catch { }

                    try
                    {
                        CheckErrorLog();
                    }
                    catch { }

                    try
                    {
                        TimeZoneMap.GetTimeZone(1, 1);
                    }
                    catch { }
                    //sys.DebugLogException(new Exception("lalaaa"));
                });
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            return;
            Task.Factory.StartNew(() =>
            {
                while (bNavDoneByCreate || ActiveFragment == null)
                    Task.Delay(100).Wait();
                Task.Delay(100).Wait();
                RunOnUiThread(() =>
                {
                    try
                    {
                        var header = NavigationView.GetHeaderView(0);
                        btnNavPin = header.FindViewById<ImageButton>(Resource.Id.btn_nav_pin);
                        btnNavPin.Click += BtnNavPin_Click;

                        if (sys.DisplayOrientation == Xamarin.Essentials.DisplayOrientation.Landscape)
                        {
                            btnNavPin.Visibility = ViewStates.Visible;
                            if (AppConfigHolder.MainConfig.MainNavigationPiddend)
                                LockNavigationOpen();
                        }
                        else
                        {
                            btnNavPin.Visibility = ViewStates.Gone;
                            if (AppConfigHolder.MainConfig.MainNavigationPiddend)
                                UnlockNavigationDrawer();
                        }
                    }
                    catch { }
                });
            });
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            DrawerToggle.SyncState();
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            DrawerToggle.OnConfigurationChanged(newConfig);
        }

        private void StatisticAppStart()
        {
            try
            {
                var stat = AppConfigHolder.UsageInfo;

                //check and do after update..
                if (stat.LastAppVersionCode != sys.iAppVersionCode)
                {
                    try
                    {
                        if (stat.LastAppVersionCode > 0)
                        {
                            //App has been updated
                            ImageLoader.ClearCache(ImageLoader.filter_clockfaces);
                            ClockHandConfig.ClearCache();
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        xLog.Error(ex);
                    }
                    stat.LastAppVersion = sys.cAppVersionInfo;
                    stat.LastAppVersionCode = sys.iAppVersionCode;
                }

                stat.LastAppStart = DateTime.Now;
                stat.AppStartCount++;
                AppConfigHolder.SaveUsageInfo();
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (NeedsStartAssistant())
            {
                ShowStartAssistant();
                return;
            }

            //if (!bNavDoneByCreate)
            //    OnNavigationItemSelected(iNavigationItem);
            bNavDoneByCreate = false;
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            if (intent.Extras != null)
            {
                iNavigationItem = intent.GetIntExtra("NavigationItem", iNavigationItem);
                if (ActiveFragment != null)
                    OnNavigationItemSelected(iNavigationItem);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("NavigationItem", iNavigationItem);
            if (ActiveFragment != null)
            {
                var blFragment = new Bundle();
                ActiveFragment.OnSaveInstanceState(blFragment);
                outState.PutBundle("ActiveFragment", blFragment);
                blRestoreFragment = blFragment;
            }
        }

        Bundle blRestoreFragment = null;
        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            if (savedInstanceState != null)
                blRestoreFragment = savedInstanceState.GetBundle("ActiveFragment");
        }

        int lastMainItem = Resource.Id.nav_clock;
        public override void OnBackPressed()
        {
            if (Drawer.IsDrawerOpen(GravityCompat.Start) && Drawer.GetDrawerLockMode(GravityCompat.Start) != DrawerLayout.LockModeLockedOpen)
            {
                Drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                if (ActiveFragment is ClockFragment || ActiveFragment is CalendarFragment || ActiveFragment is WorldTimeMapFragment)
                {
                    if (!MoveTaskToBack(false))
                        base.OnBackPressed();
                }
                else
                    OnNavigationItemSelected(lastMainItem);
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        private void BtnNavPin_Click(object sender, EventArgs e)
        {
            AppConfigHolder.MainConfig.MainNavigationPiddend = !AppConfigHolder.MainConfig.MainNavigationPiddend;
            AppConfigHolder.SaveMainConfig();

            if (AppConfigHolder.MainConfig.MainNavigationPiddend)
                LockNavigationOpen();
            else
                UnlockNavigationDrawer();
        }

        private void LockNavigationOpen()
        {
            if (!Drawer.IsDrawerOpen((int)GravityFlags.Start))
                Drawer.OpenDrawer((int)GravityFlags.Start);
            Drawer.SetScrimColor(Android.Graphics.Color.Transparent);
            Drawer.SetDrawerLockMode(DrawerLayout.LockModeLockedOpen, (int)GravityFlags.Start);
            vSpaceHolder.LayoutParameters = new LinearLayout.LayoutParams(NavigationView.Width, LinearLayout.LayoutParams.MatchParent);
            vSpaceHolder.Visibility = ViewStates.Visible;
            DrawerToggle.DrawerIndicatorEnabled = false;
            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            DrawerToggle.SyncState();
        }

        private void UnlockNavigationDrawer(bool bCloseIfOpen = true)
        {
            Drawer.SetDrawerLockMode(DrawerLayout.LockModeUnlocked, (int)GravityFlags.Start);
            if (bCloseIfOpen && Drawer.IsDrawerOpen((int)GravityFlags.Start))
                Drawer.CloseDrawer((int)GravityFlags.Start);
            Drawer.SetScrimColor(base.Resources.GetColor(Resource.Color.navigationScrim, Theme));
            DrawerToggle.DrawerIndicatorEnabled = true;
            vSpaceHolder.Visibility = ViewStates.Gone;
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            DrawerToggle.SyncState();
        }

        ActivityFragment frClock = null;
        ActivityFragment frCalendar = null;
        ActivityFragment frSettings = null;

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            return OnNavigationItemSelected(item.ItemId);
        }

        public bool OnNavigationItemSelected(int id)
        {
            Task.Factory.StartNew(() =>
            {
                ProcessNavigationChange(id);
            });
            return true;
        }

        private void ProcessNavigationChange(int id)
        {
            try
            {
                if (id == -2) //from static Shortcut
                    id = Resource.Id.nav_calendar;

                ActivityFragment fr = null;
                string cTitle = localize.AppName;
                if (id == Resource.Id.nav_clock)
                {
                    //if (frClock == null)
                    frClock = new ClockFragment();
                    fr = frClock;
                    lastMainItem = id;
                    cTitle = localize.menu_clock;
                }
                else if (id == Resource.Id.nav_calendar)
                {
                    if (ActiveFragment is CalendarFragment)
                        return;
                    //if (frCalendar == null)
                    frCalendar = new CalendarFragment();
                    fr = frCalendar;
                    lastMainItem = id;
                    cTitle = localize.menu_calendar;
                }
                else if (id == Resource.Id.nav_world_time_map)
                {
                    fr = new WorldTimeMapFragment();
                    lastMainItem = id;
                    cTitle = localize.menu_world_time_map;
                }
                else if (id == Resource.Id.nav_settings)
                {
                    fr = new MainSettingsFragment();
                    cTitle = localize.menu_settings;
                }
                /*else if (id == Resource.Id.nav_faq)
                {
                    fr = new FaqFragment();
                    cTitle = localize.menu_faq;
                }*/
                else if (id == Resource.Id.nav_about)
                {
#if DEBUG
                    fr = new DebugFragment();
#else
                    fr = new AboutFragment();
#endif
                    cTitle = localize.menu_about;
                }
                else if (id == Resource.Id.nav_theme)
                {
                    ShowThemeSelector();
                }
                if (fr != null && blRestoreFragment != null)
                    fr.OnViewStateRestored(blRestoreFragment);
                blRestoreFragment = null;

                RunOnUiThread(() =>
                {
                    try
                    {
                        Title = cTitle;
                        if (fr != null)
                        {
                            iNavigationItem = id;
                            SupportFragmentManager.BeginTransaction()
                            .Replace(Resource.Id.main_frame, fr)
                            .Commit();
                            ActiveFragment = fr;
                        }
                    }
                    catch { }
                });
            }
            finally
            {
                if (Drawer.GetDrawerLockMode(GravityCompat.Start) != DrawerLayout.LockModeLockedOpen)
                {
                    RunOnUiThread(() =>
                    {
                        try
                        {
                            Drawer.CloseDrawer(GravityCompat.Start);
                        }
                        catch { }
                    });
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            ActiveFragment?.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void CheckErrorLog()
        {
            string cErrorPath = sys.ErrorLogPath;

            if (Directory.Exists(cErrorPath))
            {
                try
                {
                    var logS = Directory.GetFiles(cErrorPath);

                    if (logS.Length > 0)
                    {
                        if (!AppConfigHolder.MainConfig.SendErrorLogs)
                        {
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    var cb = new CheckBox(this);
                                    cb.SetText(Resource.String.action_deny_error_screentshot);
                                    cb.Checked = AppConfigHolder.MainConfig.DenyErrorScreens;
                                    cb.CheckedChange += Cb_CheckedChange;
                                    new AlertDialog.Builder(this)
                                        .SetTitle(Resource.String.progress_senderrorlog_title)
                                        .SetMessage(Resource.String.progress_senderrorlog_message)
                                        .SetView(cb)
                                        .SetPositiveButton(base.Resources.GetString(Resource.String.action_yes), (s, e) =>
                                        {
                                            AppConfigHolder.MainConfig.SendErrorLogs = true;
                                            AppConfigHolder.SaveMainConfig();
                                            ErrorLogDlg.SendLogs();
                                        })
                                        .SetNegativeButton(base.Resources.GetString(Resource.String.action_no), (s, e) =>
                                        {
                                            try { Directory.Delete(cErrorPath, true); } catch { };
                                            Tools.ShowToast(this, Resource.String.progress_deleteerrorlog_done);
                                        })
                                        .SetNeutralButton(Resource.String.progress_senderrorlog_more, (s, e) =>
                                        {
                                            var logDlg = new ErrorLogDlg();
                                            logDlg.OnDialogCancel += LogDlg_OnDialogCancel;
                                            logDlg.Show(SupportFragmentManager, "");
                                        })
                                        .Create().Show();
                                }
                                catch { }
                            });
                            return;
                        }

                        ErrorLogDlg.SendLogs();
                    }
                }
                catch (Exception e)
                {
                    xLog.Error(e);
                }
            }
        }

        private void LogDlg_OnDialogCancel(object sender, EventArgs e)
        {
            CheckErrorLog();
        }

        private void Cb_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            AppConfigHolder.MainConfig.DenyErrorScreens = e.IsChecked;
            AppConfigHolder.SaveMainConfig();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

