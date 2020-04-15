
using System;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Widget;

using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.Clock
{
    [BroadcastReceiver(Label = "@string/widget_title_clock_digital", Name = "me.ichrono.droid.Clock.DigitalClockWidget")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_digitalclock")]
    public class DigitalClockWidget : MainWidgetBase
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            BackgroundService.RestartService(context, AppWidgetManager.ActionAppwidgetUpdate, appWidgetIds.Length == 1 ? (int?)appWidgetIds[0] : null);
            return;
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            BackgroundService.RestartService(context, AppWidgetManager.ActionAppwidgetDeleted);
        }
    }
}