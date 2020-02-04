
using Android.App;
using Android.Content;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionProviderChanged }, DataHost = "com.android.calendar", DataScheme = "content")]
    public class CalendarChangedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            sys.NotifyCalendarEventsUpdated();
        }
    }
}