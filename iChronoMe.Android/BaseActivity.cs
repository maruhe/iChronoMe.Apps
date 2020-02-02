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

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Adapters;
using iChronoMe.Droid.GUI;

namespace iChronoMe.Droid
{
    public abstract class BaseActivity : AppCompatActivity
    {
        public Toolbar Toolbar { get; protected set; } = null;
        public DrawerLayout Drawer { get; protected set; } = null;
        public NavigationView NavigationView { get; protected set; } = null;
        public ActivityFragment ActiveFragment { get; protected set; } = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncFusionLicenseKey);
        }

        protected override void OnResume()
        {
            base.OnResume();
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

        public bool NeedsStartAssistant()
        {
            return AppConfigHolder.MainConfig.WelcomeScreenDone < 1;
        }
        public void ShowStartAssistant()
        {
            ShowFirstStartAssistant();
        }

        public void ShowFirstStartAssistant()
        {
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle("Welcome to iChronoMe!\nPlease select your default Time-Type:")
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
                    AppConfigHolder.SaveMainConfig();
                    ShowPermissionsAssistant();
                })
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create().Show();
        }

        public void ShowPermissionsAssistant()
        {
            String[] items = new string[] { Resources.GetString(Resource.String.assistant_permission_location), Resources.GetString(Resource.String.assistant_permission_calendar), Resources.GetString(Resource.String.assistant_permission_storage) };
            bool[] checks = new bool[] { true, true, true };

            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resources.GetString(Resource.String.assistant_permission_welcome))
                .SetMultiChoiceItems(items, checks, (s, e) =>
                {
                    checks[e.Which] = e.IsChecked;
                })
                .SetPositiveButton("Continue", (s, e) =>
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
                            SetAssistantDone();
                        }
                        else
                        {
                            RunOnUiThread(() => ShowPermissionsAssistant());
                        }
                    });
                })
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create().Show();
        }

        public void SetAssistantDone()
        {
            AppConfigHolder.MainConfig.WelcomeScreenDone = 1;
            AppConfigHolder.SaveMainConfig();

            ActiveFragment?.Reinit();
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

    public class myDialogCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        BaseActivity mContext;

        public myDialogCancelListener(BaseActivity context)
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