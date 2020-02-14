using System;
using System.IO;
using System.Net.Http;
using System.Threading;
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

using iChronoMe.Core.Classes;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Calendar;
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

                SetTheme(Resource.Style.AppTheme_NoActionBar);
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
                    CheckErrorLog();
                    TimeZoneMap.GetTimeZone(1, 1);
                });

                BackgroundService.RestartService(this, AppWidgetManager.ActionAppwidgetUpdate);
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
            else if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
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
                ActivityFragment fr = null;

                if (id == Resource.Id.nav_clock)
                {
                    //if (frClock == null)
                    frClock = new ClockFragment();
                    fr = frClock;
                }
                else if (id == Resource.Id.nav_calendar)
                {
                    //if (frCalendar == null)
                    frCalendar = new CalendarFragment();
                    fr = frCalendar;
                }
                else if (id == Resource.Id.nav_world_time_map)
                {
                    fr = new WorldTimeMapFragment();
                }
                else if (id == Resource.Id.nav_share)
                {

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
                    fr = new AboutFragment();
                }
                if (fr != null && blRestoreFragment != null)
                    fr.OnViewStateRestored(blRestoreFragment);
                blRestoreFragment = null;

                RunOnUiThread(() =>
                {
                    try
                    {
                        iNavigationItem = id;
                        if (fr != null)
                        {
                            SupportFragmentManager.BeginTransaction()
                            .Replace(Resource.Id.main_frame, fr)
                            .Commit();
                            ActiveFragment = fr;
                        }

                        DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
                        drawer.CloseDrawer(GravityCompat.Start);

                    }
                    catch { }
                });
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
                                new AlertDialog.Builder(this)
                                    .SetTitle("letztes mal ging etwas schif oder die App ist abgestürzt!")
                                    .SetMessage("dürfen Fehlerprotokolle übertragen werden, damit sowas gelöst werden kann?\ndiese Protokollen enthalten technische Parameter deines Geräts, aber natürlich keinerlei persönliche Informationen!")
                                    .SetPositiveButton(Resources.GetString(Resource.String.action_yes), (s, e) =>
                                    {
                                        AppConfigHolder.MainConfig.SendErrorLogs = true;
                                        AppConfigHolder.SaveMainConfig();
                                        CheckErrorLog();
                                    })
                                    .SetNegativeButton(Resources.GetString(Resource.String.action_no), (s, e) =>
                                    {
                                        try { Directory.Delete(cErrorPath, true); } catch { };
                                    })
                                    .SetNeutralButton("more info", (s, e) =>
                                    {
                                        var logDlg = new ErrorLogDlg();
                                        logDlg.OnDialogDismiss += LogDlg_OnDialogDismiss;
                                        logDlg.Show(SupportFragmentManager, "");
                                    })
                                    .Create().Show();
                            });
                            return;
                        }

                        new Thread(async () =>
                        {
                            string cUrl = "https://apps.ichrono.me/bugs/upload.php?os=" + sys.OsType.ToString();
#if DEBUG
                            cUrl += "&debug";
#endif
                            foreach (string log in logS)
                            {
                                try
                                {
                                    HttpClient client = new HttpClient();
                                    HttpContent content = new StringContent(File.ReadAllText(log));
                                    HttpResponseMessage response = await client.PutAsync(cUrl, content);
                                    string result = await response.Content.ReadAsStringAsync();

                                    File.Delete(log);

                                    await Task.Delay(50);
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString();
                                }
                            }

                            Directory.Delete(cErrorPath, true);
                        }).Start();
                    }
                }
                catch (Exception e)
                {
                    e.ToString();
                }
            }
        }

        private void LogDlg_OnDialogDismiss(object sender, EventArgs e)
        {
            CheckErrorLog();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

