using Android.App;
using Android.OS;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [Activity(Label = "WidgetConfig", Theme = "@style/splashscreen")]

    public class CalendarWidgetConfigActivityAdvanced : BaseWidgetActivity<WidgetCfg_Calendar>
    {
        int appWidgetId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            /*
            SetTheme(Resource.Style.MainTheme);
            SetContentView(Resource.Layout.formsmapper_layout);

            Intent launchIntent = Intent;
            Bundle extras = launchIntent.Extras;

            if (extras != null)
            {
                appWidgetId = extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            }
            else
            {
                Toast.MakeText(this, Resource.String.error_message_paramers, ToastLength.Long).Show();
                FinishAndRemoveTask();
                return;
            }

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(250);

                Device.BeginInvokeOnMainThread(() =>
                {
                    AndroidFormsPage mPage = null;
                    var holder = new WidgetConfigHolder();
                    var cfg = holder.GetWidgetCfg<WidgetCfg_Calendar>(appWidgetId);

                    if (cfg is WidgetCfg_CalendarTimetable)
                        mPage = new AndroidWidgetConfig_CalendarPage(appWidgetId);
                    else if (cfg is WidgetCfg_CalendarCircleWave)
                        mPage = new AndroidWidgetConfig_CalendarCircleWavePage(appWidgetId);
                    else if (cfg is WidgetCfg_CalendarMonthView)
                        mPage = new AndroidWidgetConfig_CalendarMonthViewPage(appWidgetId);

                    if (mPage == null)
                    {
                        FinishAndRemoveTask();
                    }
                    var mFragment = mPage.CreateSupportFragment(this);
                    SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.fragment_frame_layout, mFragment)
                    .Commit();

                    FindViewById(Resource.Id.loading_panel).Visibility = Android.Views.ViewStates.Gone;

                    AndroidFormsPage.WidgetConfigIsDone = false;

                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1500);
                        while (!AndroidFormsPage.WidgetConfigIsDone)
                            await Task.Delay(100);

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Intent resultValue = new Intent();
                            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                            SetResult(Result.Ok, resultValue);

                            Finish();

                        });
                    });
                });
            });
        }

        public override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed())
            {
                // Do something if there are some pages in the `PopupStack`
            }
            else
            {
                if (new WidgetConfigHolder().WidgetExists(appWidgetId))
                    base.OnBackPressed();
                else
                {
                    new AlertDialog.Builder(this)
                        .SetTitle("Widget speichern?")
                        .SetMessage("Soll das Widget angelegt oder verworfen werden?")
                        .SetPositiveButton("speichern", (senderAlert, args) =>
                        {
                            //Widgent wird gespeichert..
                            Finish();
                        }).SetNegativeButton("Verwerfen", (senderAlert, args) =>
                        {
                            Intent cancelResultValue = new Intent();
                            cancelResultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                            SetResult(Result.Canceled, cancelResultValue);
                            Finish();
                        }).Create().Show();
                }
            }
        }

        protected override void OnStop()
        {
            CalendarWidget.updateWidgets(this, appWidgetId);
            base.OnStop();
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            */
        }
    }
}