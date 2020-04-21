using System;

using Android.App;
using Android.Content;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Intent.ActionTimeChanged, Intent.ActionDateChanged, Intent.ActionTimezoneChanged })]
    class TimeChangeReceiver : BroadcastReceiver
    {
        public TimeChangeReceiver()
        {

        }

        public override void OnReceive(Context context, Intent intent)
        {
            xLog.Warn("Timezone or Device-Time Changed");
            //var srv = Service;
            string cAction = intent.Action;
            cAction.ToString();

            try
            {
                TimeHolder.Reset(true);
                TimeHolder.Resync(true);
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
        }
    }
}