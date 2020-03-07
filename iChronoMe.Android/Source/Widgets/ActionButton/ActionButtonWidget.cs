using System;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;

using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [BroadcastReceiver(Label = "@string/widget_title_actionbutton", Name = "me.ichrono.droid.Widgets.ActionButton.ActionButtonWidget")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_action_button")]
    public class ActionButtonWidget : MainWidgetBase
    {
        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            //manual Refresh via Widget-Button
            if (ActionManualRefresh.Equals(intent.Action))
            {
                Task.Factory.StartNew(() =>
                {
                    ActionButtonService.ResetData = true;
                    int appWidgetId = intent.Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);

                    Intent itUpdate = new Intent(context.ApplicationContext, typeof(ActionButtonWidget));
                    itUpdate.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                    int[] ids = new int[] { appWidgetId };
                    itUpdate.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
                    context.SendBroadcast(itUpdate);
                });
            }
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            xLog.Verbose("start");

            var cfgHolder = new WidgetConfigHolder();
            foreach (int iWidgetId in appWidgetIds)
            {
                //Config beim ersten mal anlegen
                if (!cfgHolder.WidgetExists(iWidgetId))
                {
                    cfgHolder.SetWidgetCfg(cfgHolder.GetWidgetCfg<WidgetCfg_ActionButton>(iWidgetId));
                }
            }

            DateTime tSendWork = DateTime.Now;

            Intent intent = new Intent();
            intent.PutExtra("appWidgetIds", appWidgetIds);
            ActionButtonService.EnqueueWork(context, intent);

            Task.Factory.StartNew(() =>
            {
                Task.Delay(500).Wait();

                if (ActionButtonService.LastWorkStart < tSendWork)
                {
                    var calendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();
                    DynamicDate dToday = calendarModel.GetDateFromUtcDate(DateTime.Now);

                    int iDayCount = calendarModel.GetDaysOfMonth(dToday.Year, dToday.Month);
                    int iDay = dToday.DayOfYear;
                    iDayCount = calendarModel.GetDaysOfYear(dToday.Year);

                    foreach (int iWidgetId in appWidgetIds)
                    {
                        var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ActionButton>(iWidgetId, false);
                        Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);
                        float nHour = (float)ActionButtonService.lth.GetTime(cfg.CurrentTimeType).TimeOfDay.TotalHours;

                        ActionButtonService.DrawButton(context, cfg, wSize, appWidgetManager, iWidgetId, nHour, iDay, iDayCount, false);
                    }
                }
            });

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public static void updateWidgets(Context context)
        {
            Intent intent = new Intent(context.ApplicationContext, typeof(ActionButtonWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(context);
            int[] ids = widgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(ActionButtonWidget)).Name));
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}