using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.Receivers;

namespace iChronoMe.Droid
{
    public abstract class BaseActivity : AppCompatActivity
    {
        public Toolbar Toolbar { get; protected set; } = null;
        public DrawerLayout Drawer { get; protected set; } = null;
        public NavigationView NavigationView { get; protected set; } = null;
        public ActivityFragment ActiveFragment { get; protected set; } = null;

        private static ErrorReceiver errorReceiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

                errorReceiver = new ErrorReceiver();
                Android.Support.V4.Content.LocalBroadcastManager.GetInstance(this).RegisterReceiver(errorReceiver, new IntentFilter("com.xamarin.example.TEST"));

                Xamarin.Essentials.Platform.Init(this, savedInstanceState);
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncFusionLicenseKey);
            }
            catch (Exception ex) {
                sys.LogException(ex);
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
            catch { }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            try
            {
                ActiveFragment?.OnPrepareOptionsMenu(menu);
            }
            catch { }
            return base.OnPrepareOptionsMenu(menu);
        }

        public override void OnOptionsMenuClosed(IMenu menu)
        {
            base.OnOptionsMenuClosed(menu);
            try
            {
                ActiveFragment?.OnOptionsMenuClosed(menu);
            }
            catch { }
        }

        const float nStartAssistantMaxStep = 1.4F;
        public bool NeedsStartAssistant()
        {
            return AppConfigHolder.MainConfig.WelcomeScreenDone < nStartAssistantMaxStep;
        }

        bool bStartAssistantActive = false;
        public void ShowStartAssistant()
        {
            if (bStartAssistantActive)
                return;
            bStartAssistantActive = true;
            if (AppConfigHolder.MainConfig.WelcomeScreenDone < 1.1F)
                ShowFirstStartAssistant();
            else if (AppConfigHolder.MainConfig.WelcomeScreenDone < 1.2F)
                ShowPermissionsAssistant();
            else if (AppConfigHolder.MainConfig.WelcomeScreenDone < 1.3F)
                ShowPrivacyAssistant();
            else if (AppConfigHolder.MainConfig.WelcomeScreenDone < 1.4F)
                ShowPrivacyNotice();
            else
                SetAssistantDone();
        }

        public void ShowFirstStartAssistant()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(base.Resources.GetString(Resource.String.firststart_welcome))
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
                    AppConfigHolder.MainConfig.WelcomeScreenDone = 1.1F;
                    AppConfigHolder.SaveMainConfig();
                    //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                    ShowPermissionsAssistant();
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowPermissionsAssistant()
        {
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
                        await this.RequestPermissionsAsync(req.ToArray(), 2);

                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                        {
                            AppConfigHolder.MainConfig.WelcomeScreenDone = 1.2F;
                            AppConfigHolder.SaveMainConfig();
                            RunOnUiThread(() => ShowPrivacyAssistant());
                        }
                        else
                        {
                            RunOnUiThread(() => ShowPermissionsAssistant());
                        }
                    });
                    //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowPrivacyAssistant()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(base.Resources.GetString(Resource.String.assistant_privacy_question))
                .SetPositiveButton(Resources.GetString(Resource.String.action_yes), (s, e) =>
                {
                    AppConfigHolder.MainConfig.WelcomeScreenDone = 1.3F;
                    AppConfigHolder.SaveMainConfig();
                    //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                    ShowPrivacyNotice();
                })
                .SetNegativeButton(Resources.GetString(Resource.String.action_no), (s, e) =>
                {
                    AppConfigHolder.MainConfig.WelcomeScreenDone = 1.4F;
                    AppConfigHolder.SaveMainConfig();
                    //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                    SetAssistantDone();
                })
                .SetOnCancelListener(new QuitOnCancelListener(this))
                .Create().Show();
        }

        public void ShowPrivacyNotice()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetTitle(base.Resources.GetString(Resource.String.assistant_privacy_title))
                        .SetMessage(Resources.GetString(Resource.String.assistant_privacy_message))
                        .SetPositiveButton(Resources.GetString(Resource.String.action_accept), (s, e) =>
                        {
                            AppConfigHolder.MainConfig.WelcomeScreenDone = 1.4F;
                            AppConfigHolder.SaveMainConfig();
                            //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                            SetAssistantDone();
                        })
                        .SetNeutralButton(Resources.GetString(Resource.String.action_ignore), (s, e) =>
                        {
                            AppConfigHolder.MainConfig.WelcomeScreenDone = 1.4F;
                            AppConfigHolder.SaveMainConfig();
                            //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                            SetAssistantDone();
                        })
                        .SetNegativeButton(Resources.GetString(Resource.String.action_decline), (s, e) =>
                        {
                            //(s as Android.Support.V7.App.AlertDialog)?.Dismiss();
                            FinishAndRemoveTask();
                        })
                        .SetOnCancelListener(new QuitOnCancelListener(this))
                    .Create().Show();
        }

        public void SetAssistantDone()
        {
            bStartAssistantActive = false;
            AppConfigHolder.MainConfig.WelcomeScreenDone = nStartAssistantMaxStep;
            AppConfigHolder.SaveMainConfig();
            OnResume();
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