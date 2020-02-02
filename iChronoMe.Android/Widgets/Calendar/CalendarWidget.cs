using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [BroadcastReceiver(Label = "Kalender", Name = "me.ichrono.droid.Widgets.Calendar.CalendarWidget")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_calendar")]
    public class CalendarWidget : MainWidgetBase
    {
        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            //manual Refresh via Widget-Button
            if (ActionManualRefresh.Equals(intent.Action))
            {
                Task.Factory.StartNew(() =>
                {
                    CalendarWidgetService.ResetData = true;
                    int appWidgetId = intent.Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);

                    Intent itUpdate = new Intent(context.ApplicationContext, typeof(CalendarWidget));
                    itUpdate.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                    int[] ids = new int[] { appWidgetId };
                    itUpdate.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
                    context.SendBroadcast(itUpdate);
                });
            }
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Log.Debug("CalendarWidget", "OnUpdate");

            var holder = new WidgetConfigHolder();

            foreach (int appWidgetId in appWidgetIds)
            {
                try
                {
                    List<int> loadWidgetIdS = new List<int>();
                    if (holder.WidgetExists(appWidgetId))
                    {
                        loadWidgetIdS.Add(appWidgetId);
                    }
                    else
                    {
                        RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_unconfigured);

                        var itConfig = new Intent(context, typeof(CalendarWidgetConfigActivity));
                        itConfig.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        rv.SetOnClickPendingIntent(Resource.Id.widget, PendingIntent.GetActivity(context, appWidgetId, itConfig, PendingIntentFlags.CancelCurrent));

                        appWidgetManager.UpdateAppWidget(appWidgetId, rv);
                    }
                    if (loadWidgetIdS.Count > 0)
                    {
                        Intent intent = new Intent();
                        intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, loadWidgetIdS.ToArray());
                        CalendarWidgetService.EnqueueWork(context, intent);
                    }
                }
                catch { }
            }

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public static void updateWidgets(Context context, int iWidgetID = -1)
        {
            Intent intent = new Intent(context.ApplicationContext, typeof(CalendarWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            int[] ids = new int[] { iWidgetID };
            if (iWidgetID < 0)
            {
                AppWidgetManager widgetManager = AppWidgetManager.GetInstance(context);
                ids = widgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));
            }
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}