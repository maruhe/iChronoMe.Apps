using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Interfaces;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Droid.Receivers;
using iChronoMe.Widgets;

namespace iChronoMe.Droid
{
    public abstract class BaseActivity : AppCompatActivity, IProgressChangedHandler
    {
        public Android.Support.V7.Widget.Toolbar Toolbar { get; protected set; } = null;
        public DrawerLayout Drawer { get; protected set; } = null;
        public NavigationView NavigationView { get; protected set; } = null;
        public ActivityFragment ActiveFragment { get; protected set; } = null;

        private static ErrorReceiver errorReceiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                if (errorReceiver == null)
                {
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                    TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

                    errorReceiver = new ErrorReceiver();
                    Android.Support.V4.Content.LocalBroadcastManager.GetInstance(this).RegisterReceiver(errorReceiver, new IntentFilter("com.xamarin.example.TEST"));

                    Xamarin.Essentials.Platform.Init(this, savedInstanceState);
                    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncFusionLicenseKey);

                    if (sys.MainUserIO == null || this is MainActivity)
                        sys.MainUserIO = this;
                }
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.Clear();
        }

        public void LoadAppTheme()
        {
            try
            {
                string cThemeName = AppConfigHolder.MainConfig.AppThemeName;
                if (string.IsNullOrEmpty(cThemeName))
                    cThemeName = nameof(Resource.Style.AppTheme_iChronoMe_Dark);
                int iThemeId = (int)typeof(Resource.Style).GetField(cThemeName).GetValue(null);
                SetTheme(iThemeId);
            }
            catch (Exception ex)
            {
                AppConfigHolder.MainConfig.AppThemeName = string.Empty;
                AppConfigHolder.SaveMainConfig();
                sys.LogException(ex);
                SetTheme(Resource.Style.AppTheme_iChronoMe_Dark);
            }
        }

        public async void ShowThemeSelector()
        {
            var adapter = new ThemeAdapter(this);
            string title = bStartAssistantActive ? base.Resources.GetString(Resource.String.welcome_ichronomy) + "\n" + base.Resources.GetString(Resource.String.label_choose_theme_firsttime) : Resources.GetString(Resource.String.action_change_theme);
            var theme = await Tools.ShowSingleChoiseDlg(this, title, adapter, false);
            if (bStartAssistantActive)
            {
                AppConfigHolder.MainConfig.InitScreenTheme = 1;
                AppConfigHolder.SaveMainConfig();
                SetAssistantDone();
            }
            if (theme >= 0)
            {
                AppConfigHolder.MainConfig.AppThemeName = adapter[theme].Text1;
                AppConfigHolder.SaveMainConfig();
                RunOnUiThread(() => Recreate());
                return;
            }
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            var newExc = new Exception("TaskSchedulerOnUnobservedTaskException", unobservedTaskExceptionEventArgs.Exception);
            sys.LogException(newExc);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var newExc = new Exception("CurrentDomainOnUnhandledException", unhandledExceptionEventArgs.ExceptionObject as Exception);
            sys.LogException(newExc);
        }

        protected override void OnResume()
        {
            base.OnResume();
            sys.currentActivity = this;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            ActiveFragment?.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            var res = true;
            foreach (var grand in grantResults)
                res = res && grand == Permission.Granted;
            tcsRP?.TrySetResult(res);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            try
            {
                ActiveFragment?.OnCreateOptionsMenu(menu, MenuInflater);
            }
            catch (Exception ex) { sys.LogException(ex); }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            try
            {
                if (ActiveFragment?.IsAdded == true && ActiveFragment?.IsDetached == false)
                    ActiveFragment?.OnPrepareOptionsMenu(menu);
            }
            catch (Exception ex) { sys.LogException(ex); }
            return base.OnPrepareOptionsMenu(menu);
        }

        public override void OnOptionsMenuClosed(IMenu menu)
        {
            base.OnOptionsMenuClosed(menu);
            try
            {
                ActiveFragment?.OnOptionsMenuClosed(menu);
            }
            catch (Exception ex) { sys.LogException(ex); }
        }

        const float nStartAssistantMaxStep = 1.4F;
        public bool NeedsStartAssistant()
        {
            return (AppConfigHolder.MainConfig.InitScreenTheme < 1 && (this is MainActivity)) ||
                AppConfigHolder.MainConfig.InitScreenTimeType < 1 ||
                AppConfigHolder.MainConfig.InitScreenPrivacy < 2 ||
                AppConfigHolder.MainConfig.InitBaseDataDownload < 1 ||
                AppConfigHolder.MainConfig.InitScreenPermission < 1 ||
                AppConfigHolder.MainConfig.InitScreenUserLocation < 1;
        }

        bool bStartAssistantActive = false;
        public void ShowStartAssistant()
        {
            if (bStartAssistantActive)
                return;
            bStartAssistantActive = true;
            if (AppConfigHolder.MainConfig.InitScreenTheme < 1)// && (this is MainActivity))
                ShowThemeSelector();
            else if (AppConfigHolder.MainConfig.InitScreenTimeType < 1)
                ShowInitScreen_TimeType();
            else if (AppConfigHolder.MainConfig.InitScreenPrivacy < 1)
                ShowInitScreen_PrivacyAssistant();
            else if (AppConfigHolder.MainConfig.InitScreenPrivacy < 2)
                ShowInitScreen_PrivacyNotice();
            else if (AppConfigHolder.MainConfig.InitBaseDataDownload < 1)
                InitBaseDataDownload();
            else if (AppConfigHolder.MainConfig.InitScreenPermission < 1)
                ShowInitScreen_Permissions();
            else if (AppConfigHolder.MainConfig.InitScreenUserLocation < 1)
                ShowInitScreen_UserLocation();
            else
            {
                bStartAssistantActive = false;
                RunOnUiThread(() => OnResume());
            }
        }

        private void InitBaseDataDownload()
        {
            Task.Factory.StartNew(() =>
            {
                ClockHandConfig.CheckUpdateLocalData(this);
                AppConfigHolder.MainConfig.InitBaseDataDownload = 1;
                AppConfigHolder.SaveMainConfig();
                SetAssistantDone();
            });
        }

        public void ShowInitScreen_TimeType()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.label_choose_default_timetype)
                .SetAdapter(new TimeTypeAdapter(this), (s, e) =>
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
                    AppConfigHolder.MainConfig.InitScreenTimeType = 1;
                    AppConfigHolder.SaveMainConfig();
                    SetAssistantDone();
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowInitScreen_Permissions()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                AppConfigHolder.MainConfig.InitScreenPermission = 1;
                AppConfigHolder.SaveMainConfig();
                SetAssistantDone();
            }

            String[] items = new string[] { Resources.GetString(Resource.String.assistant_permission_location), Resources.GetString(Resource.String.assistant_permission_calendar), Resources.GetString(Resource.String.assistant_permission_storage) };
            bool[] checks = new bool[] { true, true, true };

            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(base.Resources.GetString(Resource.String.assistant_permission_welcome))
                .SetMultiChoiceItems(items, checks, (s, e) =>
                {
                    checks[e.Which] = e.IsChecked;
                })
                .SetPositiveButton(Resources.GetString(Resource.String.action_continue), (s, e) =>
                {

                    var req = new List<string>();

                    req.Add(Manifest.Permission.AccessCoarseLocation);
                    req.Add(Manifest.Permission.AccessFineLocation);

                    if (checks[1])
                    {
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadCalendar) != Permission.Granted)
                        {
                            req.Add(Manifest.Permission.ReadCalendar);
                            req.Add(Manifest.Permission.WriteCalendar);
                        }
                    }
                    if (checks[2])
                    {
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted)
                        {
                            req.Add(Manifest.Permission.ReadExternalStorage);
                        }
                    }
                    Task.Factory.StartNew(async () =>
                    {
                        bStartAssistantActive = false;
                        await this.RequestPermissionsAsync(req.ToArray(), 2);

                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                        {
                            AppConfigHolder.MainConfig.InitScreenPermission = 1;
                            AppConfigHolder.SaveMainConfig();
                        }
                    });
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowInitScreen_UserLocation()
        {
            var pDlg = ProgressDlg.NewInstance("~~location~~");
            pDlg.Show(SupportFragmentManager, null);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                if (Build.VERSION.SdkInt < BuildVersionCodes.M || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                {
                    var locationManager = (LocationManager)GetSystemService(Context.LocationService);

                    bool bIsPassive = locationManager.IsProviderEnabled(LocationManager.PassiveProvider);

                    if (!locationManager.IsProviderEnabled(LocationManager.NetworkProvider) && !locationManager.IsProviderEnabled(LocationManager.GpsProvider))
                    {
                        pDlg.SetProgressDone();
                        if (await Tools.ShowYesNoMessage(this, Resource.String.location_provider_disabled_alert, Resource.String.location_provider_disabled_question))
                        {
                            bStartAssistantActive = false;
                            StartActivity(new Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                            return;
                        }
                    }

                    try
                    {
                        var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                        if (lastLocation != null)
                        {
                            LocationTimeHolder.LocalInstance.ChangePositionDelay(lastLocation.Latitude, lastLocation.Longitude);
                            LocationTimeHolder.LocalInstance.SaveLocal();
                            AppConfigHolder.MainConfig.InitScreenUserLocation = 1;
                            AppConfigHolder.SaveMainConfig();
                            pDlg.SetProgressDone();
                            SetAssistantDone();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                var loc = await LocationPickerDialog.SelectLocation(this);

                if (loc != null)
                {
                    LocationTimeHolder.LocalInstance.ChangePositionDelay(loc.Latitude, loc.Longitude);
                    LocationTimeHolder.LocalInstance.SaveLocal();
                    AppConfigHolder.MainConfig.InitScreenUserLocation = 1;
                    AppConfigHolder.SaveMainConfig();
                    SetAssistantDone();
                    return;
                }

                await Tools.ShowMessageAndWait(this, Resource.String.error_location_is_needet, Resource.String.app_needs_location_description);

                loc = await LocationPickerDialog.SelectLocation(this);

                if (loc != null)
                {
                    LocationTimeHolder.LocalInstance.ChangePositionDelay(loc.Latitude, loc.Longitude);
                    LocationTimeHolder.LocalInstance.SaveLocal();
                    AppConfigHolder.MainConfig.InitScreenUserLocation = 1;
                    AppConfigHolder.SaveMainConfig();
                    SetAssistantDone();
                    return;
                }

                ShowExitMessage(Resource.String.error_location_is_needet);

            });
        }

        public void ShowInitScreen_PrivacyAssistant()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(base.Resources.GetString(Resource.String.assistant_privacy_question))
                .SetPositiveButton(Resources.GetString(Resource.String.action_yes), (s, e) =>
                {
                    AppConfigHolder.MainConfig.InitScreenPrivacy = 1;
                    AppConfigHolder.SaveMainConfig();
                    SetAssistantDone();
                })
                .SetNegativeButton(Resources.GetString(Resource.String.action_no), (s, e) =>
                {
                    AppConfigHolder.MainConfig.InitScreenPrivacy = 2;
                    AppConfigHolder.SaveMainConfig();
                    SetAssistantDone();
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowInitScreen_PrivacyNotice()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetTitle(base.Resources.GetString(Resource.String.assistant_privacy_title))
                        .SetMessage(Resources.GetString(Resource.String.assistant_privacy_message))
                        .SetPositiveButton(Resources.GetString(Resource.String.action_accept), (s, e) =>
                        {
                            AppConfigHolder.MainConfig.InitScreenPrivacy = 2;
                            AppConfigHolder.SaveMainConfig();
                            SetAssistantDone();
                        })
                        .SetNeutralButton(Resources.GetString(Resource.String.action_ignore), (s, e) =>
                        {
                            AppConfigHolder.MainConfig.InitScreenPrivacy = 2;
                            AppConfigHolder.SaveMainConfig();
                            SetAssistantDone();
                        })
                        .SetNegativeButton(Resources.GetString(Resource.String.action_decline), (s, e) =>
                        {
                            FinishAndRemoveTask();
                        })
                        .SetOnCancelListener(new QuitOnCancelListener(this))
                    .Create().Show();
        }

        public void SetAssistantDone()
        {
            bStartAssistantActive = false;
            ShowStartAssistant();
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

        ProgressDlg pDlg = null;

        public void StartProgress(string cTitle)
        {
            RunOnUiThread(() =>
            {
                pDlg = ProgressDlg.NewInstance(cTitle);
                pDlg.Show(this.SupportFragmentManager, "progress_widget_cfg_assi_mgr");
            });
        }

        public void SetProgress(int progress, int max, string cMessage)
        {
            RunOnUiThread(() =>
            {
                if (pDlg == null)
                    StartProgress(Resources.GetString(Resource.String.just_a_moment));
                pDlg.SetProgress(progress, max, cMessage);
            });
        }

        public void SetProgressDone()
        {
            RunOnUiThread(() =>
            {
                if (pDlg != null)
                    pDlg.SetProgressDone();
            });
        }

        public void ShowToast(string cMessage)
        {
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, cMessage, ToastLength.Long).Show();
            });
        }

        public void ShowError(string cMessage)
        {
            RunOnUiThread(() =>
            {
                if (pDlg != null)
                    pDlg.SetProgressDone();
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(250).Wait();
                    RunOnUiThread(() =>
                    {
                        new Android.Support.V7.App.AlertDialog.Builder(this).SetTitle(Resources.GetString(Resource.String.label_error)).SetMessage(cMessage).Create().Show();
                    });
                });
            });
        }

        protected void ShowExitMessage(string cMessage)
        {
            var alert = new Android.Support.V7.App.AlertDialog.Builder(this)
               .SetMessage(cMessage)
               .SetCancelable(false);
            alert.SetPositiveButton(Resource.String.action_ok, (senderAlert, args) =>
            {
                (senderAlert as IDialogInterface).Dismiss();
                FinishAndRemoveTask();
            });

            alert.Show();
        }

        protected void ShowExitMessage(int iMessage)
        {
            var alert = new Android.Support.V7.App.AlertDialog.Builder(this)
               .SetMessage(iMessage)
               .SetCancelable(false);
            alert.SetPositiveButton(Resource.String.action_ok, (senderAlert, args) =>
            {
                (senderAlert as IDialogInterface).Dismiss();
                FinishAndRemoveTask();
            });

            alert.Show();
        }
    }

    public class QuitOnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        BaseActivity mContext;

        public QuitOnCancelListener(BaseActivity context)
        {
            mContext = context;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            mContext.FinishAndRemoveTask();
        }

        protected override void Dispose(bool disposing)
        {
            mContext = null;
            base.Dispose(disposing);
        }
    }
}