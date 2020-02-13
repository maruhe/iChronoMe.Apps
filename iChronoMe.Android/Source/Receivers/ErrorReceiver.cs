﻿
using Android.App;
using Android.Content;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.Receivers
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { sys.action_ErrorReceiver })]
    public class ErrorReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (sys.currentError == null)
                Toast.MakeText(context, "exXX is null", ToastLength.Long).Show();
            else
                Toast.MakeText(context, sys.currentError.Message, ToastLength.Long).Show();

            try
            {
                var v = sys.currentActivity.Window.DecorView.RootView;
                v.ToString();
            }
            catch { }
        }
    }
}