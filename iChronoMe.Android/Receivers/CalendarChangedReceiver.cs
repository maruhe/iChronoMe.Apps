using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using iChronoMe.Droid.Widgets.Calendar;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionProviderChanged }, DataHost = "com.android.calendar", DataScheme = "content")]
    public class CalendarChangedReceiver : BroadcastReceiver
    {
        private const String TAG = "CalendarChangedReceiver";
        public override void OnReceive(Context context, Intent intent)
        {
            var manager = AppWidgetManager.GetInstance(context);
            int[] appWidgetID1s = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));
            if (appWidgetID1s.Length > 0)
                manager.NotifyAppWidgetViewDataChanged(appWidgetID1s, Resource.Id.event_list);
        }
    }
}