using System;

using Android.App;
using Android.Content;
using Android.Util;
using Android.Widget;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class AutoStartReceiver : BroadcastReceiver
    {
        static bool bDone = false;
        public override void OnReceive(Context context, Intent intent)
        {
            /*
            try
            {
                Console.WriteLine("Received AutoStartReceiver intent!.");

                if (bDone)
                    return;
                bDone = true;

                global::Xamarin.Forms.Forms.Init(context, null);

                if (AppConfigHolder.MainConfig.AlwaysShowForegroundNotification)
                {
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    {
                        context.StartForegroundService(new Intent(context, typeof(BackgroundService)));
                    }
                    else
                    {
                        context.StartService(new Intent(context, typeof(BackgroundService)));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("AutoStartReceiver", e.Message);
            }
            */
        }
    }
}