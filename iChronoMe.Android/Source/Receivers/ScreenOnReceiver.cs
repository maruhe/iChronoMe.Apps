
using System;

using Android.App;
using Android.Content;
using Android.OS;
using iChronoMe.Droid.Widgets.Clock;

namespace iChronoMe.Droid.Receivers
{
    /*
     * not working fine this stuff
    [Service(Label = "BackgroundManagerServer", Exported = true)]
    public class BackgroundManagerServer : Service
    {
        public static ScreenOnReceiver ScreenOnReceiver { get; private set; }

        public override void OnCreate()
        {
            base.OnCreate();

            SetTheme(Resource.Style.BaseTheme_iChronoMe_Dark);

            if (ScreenOnReceiver == null)
            {
                ScreenOnReceiver = new ScreenOnReceiver();
                RegisterReceiver(ScreenOnReceiver, new IntentFilter(Intent.ActionScreenOn)); 
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }

    public class ScreenOnReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Tools.ShowToastDebug(context, intent.Action);
            if (intent.Action.Equals(Intent.ActionScreenOn))
            {
                ClockWidgetService.EnqueueWork(context, new Intent(ClockWidgetService.Action_ScreenOn));
            }
        }
    };
    */
}