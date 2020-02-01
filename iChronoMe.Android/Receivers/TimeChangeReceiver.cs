using Android.App;
using Android.Content;
using Android.Util;
using System;

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
            /*
            if (BackgroundService.currentService == null && !Xamarin.Forms.Forms.IsInitialized)
                return;

            Console.WriteLine("Timezone or Device-Time Changed");
            //var srv = Service;
            string cAction = intent.Action;
            cAction.ToString();

            try
            {
                TimeHolder.Resync();
            } catch (Exception ex)
            {
                Log.Error("TimeChangeReceiver", ex.Message);
            }
            */
        }
    }
}