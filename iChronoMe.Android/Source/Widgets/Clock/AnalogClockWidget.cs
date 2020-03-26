
using System;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Widget;

using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    [BroadcastReceiver(Label = "@string/widget_title_clock_analog", Name = "me.ichrono.droid.Clock.AnalogClockWidget")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_analogclock")]
    public class AnalogClockWidget : MainWidgetBase
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            BackgroundService.RestartService(context, AppWidgetManager.ActionAppwidgetUpdate, appWidgetIds.Length == 1 ? (int?)appWidgetIds[0] : null);
            return;

            xLog.Verbose("start");

            var cfgHolder = new WidgetConfigHolder();

            foreach (int iWidgetId in appWidgetIds)
            {
                var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ClockAnalog>(iWidgetId);
                RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_clock);

                Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);

                WidgetView_ClockAnalog v = new WidgetView_ClockAnalog();
                v.ReadConfig(cfg);
                var bmp = v.GetBitmap(DateTime.Now, wSize.X, wSize.Y);

                rv.SetImageViewBitmap(Resource.Id.analog_clock, BitmapFactory.DecodeStream(bmp));

                appWidgetManager.UpdateAppWidget(iWidgetId, rv);
            }
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            BackgroundService.RestartService(context, AppWidgetManager.ActionAppwidgetDeleted);
        }
    }
}

/*
Job Intent-Service


        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            xLog.Verbose("start");

            Intent intent = new Intent();
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
            if (intent.GetIntExtra(ClockWidgetService.Extra_FromTimer, -1) == 1)
                intent.PutExtra(ClockWidgetService.Extra_FromTimer, 1);
            else
                intent.PutExtra(ClockWidgetService.Extra_UpdateFromWidget, 1);
            ClockWidgetService.EnqueueWork(context, intent);

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            return;

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
                        rv.SetOnClickPendingIntent(Resource.Id.widget, MainWidgetBase.GetClickActionPendingIntent(context, new ClickAction(ClickActionType.OpenSettings), appWidgetId, "me.ichrono.droid/me.ichrono.droid.Widgets.Clock.AnalogClockWidgetConfigActivity"));
                        appWidgetManager.UpdateAppWidget(appWidgetId, rv);
                    }
                    if (loadWidgetIdS.Count > 0)
                    {
                        intent = new Intent();
                        intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, loadWidgetIdS.ToArray());
                        if (intent.GetIntExtra(ClockWidgetService.Extra_FromTimer, -1) == 1)
                            intent.PutExtra(ClockWidgetService.Extra_FromTimer, 1);
                        else
                            intent.PutExtra(ClockWidgetService.Extra_UpdateFromWidget, 1);
                        ClockWidgetService.EnqueueWork(context, intent);
                    }
                }
                catch { }
            }

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public static void updateWidgets(Context context, int iWidgetID = -1)
        {
            Intent intent = new Intent(context.ApplicationContext, typeof(AnalogClockWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            int[] ids = new int[] { iWidgetID };
            if (iWidgetID < 0)
            {
                AppWidgetManager widgetManager = AppWidgetManager.GetInstance(context);
                ids = widgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
            }
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }

 */
