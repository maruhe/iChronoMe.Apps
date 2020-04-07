using System;
using System.Drawing;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [Activity(Label = "CalendarWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]
    public class CalendarWidgetConfigActivity : BaseWidgetActivity<WidgetCfg_Calendar>
    {
        DynamicCalendarModel CalendarModel;
        EventCollection myEventsMonth;
        EventCollection myEventsList;

        bool bPermissionTryed = false;

        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
                ShowStartAssistant();
            else
            {
                if (Build.VERSION.SdkInt > BuildVersionCodes.M && (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted))
                {
                    if (!bPermissionTryed)
                    {
                        ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadCalendar, Manifest.Permission.WriteCalendar, Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
                        bPermissionTryed = true;
                    }
                    else
                    {
                        new AlertDialog.Builder(this)
                            .SetMessage("calendar-permission is required for a calendar-widget!")
                            .SetPositiveButton("accept", (s, e) => { FinishAndRemoveTask(); })
                            .Create().Show();
                    }
                    return;
                }

                StartWidgetSelection();
            }
        }

        Point wSize = new Point(400, 300);

        public void StartWidgetSelection()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted))
            {
                ShowExitMessage(Resource.String.widget_error_location_is_requered);
                return;
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(100).Wait();

                    SearchForDeletedWidgets();

                    TryGetWallpaper();

                    myEventsMonth = new EventCollection();

                    myEventsList = new EventCollection();

                    CalendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();

                    var dToday = CalendarModel.GetDateFromUtcDate(DateTime.Now);
                    var dFirst = dToday.BoM;
                    var dLast = dFirst.AddDays((int)(CalendarModel.GetDaysOfMonth(dFirst.Year, dFirst.Month) * 2));
                    myEventsMonth.DoLoadCalendarEventsGrouped(dFirst.UtcDate.AddDays(-7), dLast.UtcDate).Wait();
                    myEventsList.DoLoadCalendarEventsGrouped(dToday.UtcDate, dToday.UtcDate.AddDays(22), 10).Wait();

                    RunOnUiThread(() =>
                    {
                        ShowWidgetTypeSelector();
                        pDlg.Dismiss();
                    });
                }
                catch (System.Exception ex)
                {
                    ShowExitMessage(ex.Message);
                }
            });
        }

        private void ShowWidgetTypeSelector()
        {
            var tStartAssistant = typeof(WidgetCfgAssistant_Calendar_Start);
            if (cfgHolder.WidgetExists<WidgetCfg_CalendarCircleWave>(appWidgetId))
            {
                ShowWidgetCircleWaveSelector();
                return;
            }
            else if (cfgHolder.WidgetExists<WidgetCfg_Calendar>(appWidgetId))
                tStartAssistant = typeof(WidgetCfgAssistant_Calendar_OptionsBase);
            var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_Calendar>(appWidgetId, false);
            var manager = new WidgetConfigAssistantManager<WidgetCfg_Calendar>(this, CalendarModel, myEventsList, myEventsMonth, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                bool bStartedSubManager = false;
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        cfgHolder.SetWidgetCfg(result.WidgetConfig);

                        if (cfg == null && result.WidgetConfig is WidgetCfg_CalendarCircleWave)
                        {
                            bStartedSubManager = true;
                            ShowWidgetCircleWaveSelector(true);
                            return;
                        }

                        Intent resultValue = new Intent();
                        resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        SetResult(Result.Ok, resultValue);

                        UpdateWidget();
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Long).Show());
                }
                finally
                {
                    if (!bStartedSubManager)
                        FinishAndRemoveTask();
                }
            });
        }

        private void ShowWidgetCircleWaveSelector(bool bIsFirstCreate = false)
        {
            var tStartAssistant = typeof(WidgetCfgAssistant_CalendarCircleWave_OptionsBase);
            if (bIsFirstCreate)
                tStartAssistant = typeof(WidgetCfgAssistant_CalendarCircleWave_Length);
            var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_CalendarCircleWave>(appWidgetId, false);
            var manager = new WidgetConfigAssistantManager<WidgetCfg_CalendarCircleWave>(this, CalendarModel, myEventsList, myEventsMonth, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        cfgHolder.SetWidgetCfg(result.WidgetConfig);

                        Intent resultValue = new Intent();
                        resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        SetResult(Result.Ok, resultValue);

                        UpdateWidget();
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Long).Show());
                }
                finally
                {
                    FinishAndRemoveTask();
                }
            });

        }
    }
}