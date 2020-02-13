using System;

using Android.App;
using Android.Appwidget;
using Android.Content;

namespace iChronoMe.Droid.Widgets.Clock
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { "me.ichrono.droid.Widgets.Clock.ClockUpdateBroadcast" })]
    public class ClockUpdateBroadcastReceiver : BroadcastReceiver
    {
        public const string intentFilter = "me.ichrono.droid.Widgets.Clock.ClockUpdateBroadcast";
        public const string command = "command";
        public const string baseaction = "baseaction";
        public const string cmdStopUpdates = "cmd_stop_updates";
        public const string cmdRestartUpdates = "cmd_restart_update";

        public ClockUpdateBroadcastReceiver()
        {
        }

        public override void OnReceive(Context context, Intent intent)
        {
            //var srv = Service;
            String cCommand = intent.GetStringExtra(command);
            String cBaseAction = intent.GetStringExtra(baseaction);
            int? iAppWidgetID = intent.HasExtra(AppWidgetManager.ExtraAppwidgetId) ? (int?)intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, 0) : null;
            if (CommandReceived != null && !string.IsNullOrEmpty(cCommand))
                CommandReceived(cCommand, cBaseAction, iAppWidgetID);
        }

        public delegate void CommandReceivedEventHandler(string command, string baseaction, int? iAppWidgetID);
        public event CommandReceivedEventHandler CommandReceived;
    }
}