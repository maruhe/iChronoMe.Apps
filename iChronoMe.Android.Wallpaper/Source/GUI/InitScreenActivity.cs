using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Interfaces;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Widgets;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace iChronoMe.Droid.Wallpaper.GUI
{
    [Android.App.Activity(Label = "InitScreenActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class InitScreenActivity : AppCompatActivity, IProgressChangedHandler, ILocationListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetTheme(Resource.Style.AppTheme);
            SetTheme(Resource.Style.TransparentTheme);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.Clear();
        }

        protected bool bKillOnClose = false;

        protected override void OnPause()
        {
            base.OnPause();
            if (bKillOnClose)
            {
                KillActivity(this);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            try { dlgToClose?.Dismiss(); } catch { }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            bStartAssistantActive = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
                ShowStartAssistant();
            else
                FinishAndRemoveTask();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum]Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            var res = true;
            foreach (var grand in grantResults)
                res = res && grand == Permission.Granted;
            tcsRP?.TrySetResult(res);
        }

        const float nStartAssistantMaxStep = 1.4F;
        public static bool NeedsStartAssistant()
        {
            return
               (AppConfigHolder.MainConfig.InitScreenTimeType < 1 ||
                AppConfigHolder.MainConfig.InitScreenPrivacy < 2 ||
                AppConfigHolder.MainConfig.InitBaseDataDownload < 1 ||
                AppConfigHolder.MainConfig.InitScreenPermission < 1 ||
                AppConfigHolder.MainConfig.InitScreenUserLocation < 1);
        }

        static bool bStartAssistantActive = false;
        public void ShowStartAssistant()
        {
            if (bStartAssistantActive)
                return;
            bStartAssistantActive = true;
            bKillOnClose = true;
            RunOnUiThread(() =>
            {
                try
                {
                    if (AppConfigHolder.MainConfig.InitScreenTimeType < 1)
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
                        bKillOnClose = false;
                        try { OnResume(); } catch { };
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                }
            });
        }

        private void InitBaseDataDownload()
        {
            Task.Factory.StartNew(() =>
            {
                bStartAssistantActive = false; // for in case the user minimizes the app while progress
                bKillOnClose = true; // to be sure
                try
                {
                    ClockHandConfig.CheckUpdateLocalData(this);
                }
                catch { }
                try
                {
                    TimeZoneMap.GetTimeZone(1, 1);
                }
                catch { }
                AppConfigHolder.MainConfig.InitBaseDataDownload = 1;
                AppConfigHolder.SaveMainConfig();
                SetAssistantDone();
            });
        }

        public void ShowInitScreen_TimeType()
        {
            if (Looper.MainLooper == null)
                Looper.Prepare();

            dlgToClose = new AlertDialog.Builder(this)
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
                .SetOnCancelListener(new KillOnCancelListener(this))
                .SetCancelable(false)
                .SetOnKeyListener(new KillOnBackPressListener(this))
                .Create();
            dlgToClose.Show();
        }

        public void ShowInitScreen_Permissions()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                AppConfigHolder.MainConfig.InitScreenPermission = 1;
                AppConfigHolder.SaveMainConfig();
                SetAssistantDone();
                return;
            }

            if (Looper.MainLooper == null)
                Looper.Prepare();

            bKillOnClose = true;
            String[] items = new string[] { Resources.GetString(Resource.String.assistant_permission_location), Resources.GetString(Resource.String.assistant_permission_storage) };
            bool[] checks = new bool[] { true, true };

            dlgToClose = new AlertDialog.Builder(this)
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
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted)
                        {
                            req.Add(Manifest.Permission.ReadExternalStorage);
                        }
                    }
                    Task.Factory.StartNew(async () =>
                    {
                        bKillOnClose = false;
                        //bStartAssistantActive = false;
                        await this.RequestPermissionsAsync(req.ToArray(), 2);

                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                        {
                            AppConfigHolder.MainConfig.InitScreenPermission = 1;
                            AppConfigHolder.SaveMainConfig();
                        }
                        SetAssistantDone();
                    });
                })
                .SetOnCancelListener(new KillOnCancelListener(this))
                .SetCancelable(false)
                .SetOnKeyListener(new KillOnBackPressListener(this))
                .Create();
            dlgToClose.Show();
        }

        AlertDialog dlgToClose;

        public void ShowInitScreen_UserLocation()
        {
            var pDlg = ProgressDlg.NewInstance(localize.progress_determineLocation);
            pDlg.Show(SupportFragmentManager, null);
            bStartAssistantActive = false; // for in case the user minimizes the app while progress
            bKillOnClose = true; // to be sure

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                if (Looper.MainLooper == null)
                    Looper.Prepare();
                SelectPositionResult selPos = null;
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
                            bKillOnClose = false;
                            StartActivity(new Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                            return;
                        }
                        ShowInitScreen_UserLocationManual();
                    }

                    try
                    {
                        var lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.PassiveProvider);
                        if (lastLocation == null)
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);

                        int iTry = 0;
                        while (lastLocation == null && iTry < 3)
                        {
                            iTry++;
                            try { locationManager.RequestSingleUpdate(LocationManager.NetworkProvider, this, Looper.MainLooper); } catch { this.ToString(); }
                            try { locationManager.RequestSingleUpdate(LocationManager.GpsProvider, this, Looper.MainLooper); } catch { this.ToString(); }
                            Task.Delay(1500).Wait();
                            lastLocation = locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                            if (lastLocation == null)
                                lastLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                        }

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
                        xLog.Error(ex);
                    }

                    RunOnUiThread(() =>
                    {
                        try
                        {
                            dlgToClose = new AlertDialog.Builder(this)
                            .SetTitle(Resource.String.error_no_location_demitered)
                            .SetMessage(Resource.String.app_needs_location_description)
                            .SetPositiveButton(Resource.String.action_try_again, (s, e) =>
                            {
                                SetAssistantDone();
                            })
                            .SetNegativeButton(Resource.String.action_select_location, async (s, e) =>
                            {
                                ShowInitScreen_UserLocationManual();
                            })
                            .SetNeutralButton(Resource.String.action_settings, (s, e) =>
                            {
                                StartActivity(new Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                            })
                            .SetOnCancelListener(new KillOnCancelListener(this))
                            .SetCancelable(false)
                            .SetOnKeyListener(new KillOnBackPressListener(this))
                            .Create();

                            pDlg.SetProgressDone();
                            dlgToClose.Show();
                        }
                        catch { }
                    });
                    bStartAssistantActive = false;
                    return;
                }
            });
        }

        public void ShowInitScreen_UserLocationManual()
        {
            Task.Factory.StartNew(async () =>
            {
                var loc = await LocationPickerDialog.SelectLocation(this);

                if (loc != null)
                {
                    LocationTimeHolder.LocalInstance.ChangePositionDelay(loc.Latitude, loc.Longitude);
                    LocationTimeHolder.LocalInstance.SaveLocal();
                    AppConfigHolder.MainConfig.InitScreenUserLocation = 1;
                    AppConfigHolder.SaveMainConfig();
                    pDlg?.SetProgressDone();
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
                    pDlg?.SetProgressDone();
                    SetAssistantDone();
                    return;
                }

                pDlg?.SetProgressDone();
                ShowExitMessage(Resource.String.error_location_is_needet);
            });
        }

        public void ShowInitScreen_PrivacyAssistant()
        {
            if (Looper.MainLooper == null)
                Looper.Prepare();
            dlgToClose = new AlertDialog.Builder(this)
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
                .SetOnCancelListener(new KillOnCancelListener(this))
                .SetCancelable(false)
                .SetOnKeyListener(new KillOnBackPressListener(this))
                .Create();
            dlgToClose.Show();
        }

        public void ShowInitScreen_PrivacyNotice()
        {
            if (Looper.MainLooper == null)
                Looper.Prepare();
            var textView = new TextView(this);
            textView.Text = "**text not found**\n" + "          :-(";
            try
            {
                AssetManager assets = this.Assets;
                string assetId = "privacy_notice.html";
                string localId = "privacy_notice_" + CultureInfo.CurrentCulture.TwoLetterISOLanguageName + ".html";
                textView.Text += "\n" + assetId + "\n" + localId;
                if (Array.IndexOf(assets.List(""), localId) >= 0)
                    assetId = localId;
                textView.Text += "\n\n" + assetId;
                using (StreamReader sr = new StreamReader(assets.Open(assetId)))
                {
                    if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                    {
#pragma warning disable CS0618 // Typ oder Element ist veraltet
                        textView.TextFormatted = Android.Text.Html.FromHtml(sr.ReadToEnd());
#pragma warning restore CS0618 // Typ oder Element ist veraltet
                    }
                    else
                        textView.TextFormatted = Android.Text.Html.FromHtml(sr.ReadToEnd(), Android.Text.FromHtmlOptions.ModeLegacy);
                }
            }
            catch (Exception ex)
            {
                textView.Text += ex.Message;
                sys.LogException(ex);
            }
            int pad = (int)(5 * sys.DisplayDensity);
            textView.SetPadding(pad, pad, pad, pad);
            textView.MovementMethod = LinkMovementMethod.Instance;
            var scroll = new ScrollView(this);
            scroll.AddView(textView);
            dlgToClose = new AlertDialog.Builder(this)
                        .SetView(scroll)
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
                        .SetOnCancelListener(new KillOnCancelListener(this))
                    .Create();
            dlgToClose.Show();
            dlgToClose.Window.SetLayout(WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent);
        }

        public void SetAssistantDone()
        {
            bKillOnClose = false;
            bStartAssistantActive = false;
            ShowStartAssistant();
        }

        public void ShowKeyboard(View userInput)
        {
            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                RunOnUiThread(() =>
                {
                    try
                    {
                        userInput.RequestFocus();
                        InputMethodManager imm = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                        imm.ToggleSoftInput(ShowFlags.Forced, 0);
                    }
                    catch { }
                });
            });
        }

        public void HideKeyboard(View userInput)
        {
            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                RunOnUiThread(() =>
                {
                    try
                    {
                        InputMethodManager imm = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(userInput.WindowToken, 0);
                    }
                    catch { }
                });
            });
        }

        public static void KillActivity(Activity activity)
        {
            try { activity.MoveTaskToBack(true); } catch { }
            Process.KillProcess(Process.MyPid());
            Java.Lang.JavaSystem.Exit(0);
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
                try
                {
                    pDlg = ProgressDlg.NewInstance(cTitle);
                    pDlg.Show(this.SupportFragmentManager, "progress_widget_cfg_assi_mgr");
                }
                catch { }
            });
        }

        public void SetProgress(int progress, int max, string cMessage)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    if (pDlg == null)
                        StartProgress(Resources.GetString(Resource.String.just_a_moment));
                    pDlg.SetProgress(progress, max, cMessage);
                }
                catch { }
            });
        }

        public void SetProgressDone()
        {
            RunOnUiThread(() =>
            {
                try
                {
                    if (pDlg != null)
                        pDlg.SetProgressDone();
                }
                catch { }
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
                        try
                        {
                            new AlertDialog.Builder(this).SetTitle(Resources.GetString(Resource.String.label_error)).SetMessage(cMessage).Create().Show();
                        }
                        catch { }
                    });
                });
            });
        }

        protected void ShowExitMessage(string cMessage)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var alert = new AlertDialog.Builder(this)
                       .SetMessage(cMessage)
                       .SetCancelable(false)
                       .SetOnKeyListener(new KillOnBackPressListener(this))
                       .SetPositiveButton(Resource.String.action_ok, (senderAlert, args) =>
                       {
                           (senderAlert as IDialogInterface).Dismiss();
                           FinishAndRemoveTask();
                       }).Create();
                    alert.Show();
                }
                catch
                {
                    KillActivity(this);
                }
            });
        }

        protected void ShowExitMessage(int iMessage)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var alert = new AlertDialog.Builder(this)
                   .SetMessage(iMessage)
                   .SetCancelable(false)
                   .SetOnKeyListener(new KillOnBackPressListener(this));
                    alert.SetPositiveButton(Resource.String.action_ok, (senderAlert, args) =>
                    {
                        (senderAlert as IDialogInterface).Dismiss();
                        FinishAndRemoveTask();
                    }).Create();

                    alert.Show();
                }
                catch
                {
                    KillActivity(this);
                }
            });
        }

        void ILocationListener.OnLocationChanged(Location location)
        {
            xLog.Debug("OnLocationChanged: " + location.ToString());
        }

        void ILocationListener.OnProviderDisabled(string provider)
        {
            xLog.Debug("OnProviderDisabled: " + provider);
        }

        void ILocationListener.OnProviderEnabled(string provider)
        {
            xLog.Debug("OnProviderEnabled: " + provider);
        }

        void ILocationListener.OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            xLog.Debug("OnStatusChanged: " + provider + ": " + status.ToString());
        }
    }

    public class KillOnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        Activity myActivity;

        public KillOnCancelListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            InitScreenActivity.KillActivity(myActivity);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            myActivity = null;
        }
    }

    public class KillOnBackPressListener : Java.Lang.Object, IDialogInterfaceOnKeyListener
    {
        Activity myActivity;

        public KillOnBackPressListener(Activity activity)
        {
            myActivity = activity;
        }
        public bool OnKey(IDialogInterface dialog, [GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                dialog.Dismiss();
                InitScreenActivity.KillActivity(myActivity);
                return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            myActivity = null;
        }
    }
}