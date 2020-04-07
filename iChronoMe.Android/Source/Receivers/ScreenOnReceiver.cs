
using Android.App;
using Android.Content;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Intent.ActionScreenOn, Intent.ActionScreenOff })]
    public class ScreenOnOffReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            xLog.Debug(intent.Action);
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