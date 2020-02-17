
using System;

using Android.App;
using Android.Content;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class AutoStartReceiver : BroadcastReceiver
    {
        static bool bDone = false;
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                xLog.Debug("Received AutoStartReceiver intent!.");

                if (bDone)
                    return;
                bDone = true;

                if (AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
                {
                    BackgroundService.RestartService(context, Intent.ActionBootCompleted);
                }
            }
            catch (Exception e)
            {
                xLog.Error(e);
            }
        }
    }
}