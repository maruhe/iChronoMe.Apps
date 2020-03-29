
using System;

using Android.App;
using Android.Content;
using Android.OS;
using iChronoMe.Droid.Widgets.Clock;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Intent.ActionScreenOn, Intent.ActionScreenOff })]
    public class ScreenOnOffReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action.Equals(Intent.ActionScreenOn))
            {
                ScreenStateReceived?.Invoke(true);
            }
            else if (intent.Action.Equals(Intent.ActionScreenOff))
            {
                ScreenStateReceived?.Invoke(false);
            }
        }

        public delegate void ScreenStateEventHandler(bool bIsScreenOn);
        public event ScreenStateEventHandler ScreenStateReceived;
    };
}