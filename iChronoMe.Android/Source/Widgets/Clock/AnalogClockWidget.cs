
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