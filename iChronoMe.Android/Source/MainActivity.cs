using System;
using System.IO;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
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

using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;

namespace iChronoMe.Droid
{
    [Activity(Label = "@string/app_name", Name = "me.ichrono.droid.MainActivity", Theme = "@style/splashscreen", MainLauncher = true)]
    public class MainActivity : BaseActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        int iNavigationItem = Resource.Id.nav_clock;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState); 

            try
            {
                LoadAppTheme();
                SetContentView(Resource.Layout.activity_main);

                Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                SetSupportActionBar(Toolbar);

                Drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
                ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, Drawer, Toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
                Drawer.AddDrawerListener(toggle);
                toggle.SyncState();

                NavigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
                NavigationView.SetNavigationItemSelectedListener(this);

                if (Intent.HasExtra("NavigationItem"))
                    iNavigationItem = Intent.GetIntExtra("NavigationItem", iNavigationItem);

                if (savedInstanceState != null)
                {
                    iNavigationItem = savedInstanceState.GetInt("NavigationItem", iNavigationItem);
                    blRestoreFragment = savedInstanceState.GetBundle("ActiveFragment");
                }               

                Task.Factory.StartNew(() =>
                {
                    Task.Delay(2500).Wait();
                    BackgroundService.RestartService(this, AppWidgetManager.ActionAppwidgetUpdate);
                    CheckErrorLog();
                    TimeZoneMap.GetTimeZone(1, 1);
                    //sys.DebugLogException(new Exception("lalaaa"));
                });
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (NeedsStartAssistant())
            {
                ShowStartAssistant();
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.M && (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted))
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
            }

            OnNavigationItemSelected(iNavigationItem);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            if (intent.Extras != null)
            {
                iNavigationItem = intent.GetIntExtra("NavigationItem", iNavigationItem);
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

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
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
                try
                {
                    ActivityFragment fr = null;

                    if (id == Resource.Id.nav_clock)
                    {
                        //if (frClock == null)
                        frClock = new ClockFragment();
                        fr = frClock;
                    }
                    else if (id == Resource.Id.nav_calendar)
                    {
                        if (ActiveFragment is CalendarFragment)
                            return;
                        //if (frCalendar == null)
                        frCalendar = new CalendarFragment();
                        fr = frCalendar;
                    }
                    else if (id == Resource.Id.nav_world_time_map)
                    {
                        fr = new WorldTimeMapFragment();
                    }
                    else if (id == Resource.Id.nav_settings)
                    {
                        fr = new MainSettingsFragment();
                    }
                    else if (id == Resource.Id.nav_faq)
                    {
                        fr = new FaqFragment();
                    }
                    else if (id == Resource.Id.nav_about)
                    {
#if DExxBUG
                        fr = new DebugFragment();
#else
                    fr = new AboutFragment();
#endif
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
                    RunOnUiThread(() =>
                    {
                        try
                        {
                            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
                            drawer.CloseDrawer(GravityCompat.Start);
                        }
                        catch { }
                    });
                }
            });
            return true;
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
                                        .SetPositiveButton(Resources.GetString(Resource.String.action_yes), (s, e) =>
                                        {
                                            AppConfigHolder.MainConfig.SendErrorLogs = true;
                                            AppConfigHolder.SaveMainConfig();
                                            ErrorLogDlg.SendLogs();
                                        })
                                        .SetNegativeButton(Resources.GetString(Resource.String.action_no), (s, e) =>
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
                    e.ToString();
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

