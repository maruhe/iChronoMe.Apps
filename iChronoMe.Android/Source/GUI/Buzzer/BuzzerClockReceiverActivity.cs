using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.GUI.Buzzer
{
    [Activity(Label = "BuzzerReceiverActivity", Theme = "@style/splashscreen")]

    public class BuzzerClockReceiverActivity : BaseActivity
    {
        private Ringtone ringtone = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
            {
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
                var keyguardManager = GetSystemService(Context.KeyguardService) as KeyguardManager;
                keyguardManager.RequestDismissKeyguard(this, null);
            }
            else
            {
                Window.AddFlags(WindowManagerFlags.TurnScreenOn | WindowManagerFlags.ShowWhenLocked | WindowManagerFlags.DismissKeyguard);
            }

            this.RequestWindowFeature(WindowFeatures.NoTitle);
            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            SetContentView(Resource.Layout.activity_buzzer_receiver);

            Button stopAlarm = (Button)FindViewById(Resource.Id.stopAlarm);

            stopAlarm.Touch += (s, e) =>
            {
                if (ringtone != null)
                    ringtone.Stop();
                Finish();
                e.Handled = false;
            };

            Task.Factory.StartNew(() => {
                Task.Delay(1000).Wait();
                xLog.Debug("AlarmClockReceiverActivity start sound");
                playSound(this, getAlarmUri());
            });
        }

        private void playSound(Context context, Android.Net.Uri alert)
        {
            try
            {
                ringtone = RingtoneManager.GetRingtone(context, alert);
                ringtone.Play();
            }
            catch (IOException ex)
            {
                xLog.Error(ex);
            }
        }

        //Get an alarm sound. Try for an alarm. If none set, try notification, 
        //Otherwise, ringtone.
        private Android.Net.Uri getAlarmUri()
        {
            Android.Net.Uri alert = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
            if (alert == null)
            {
                alert = RingtoneManager
                            .GetDefaultUri(RingtoneType.Ringtone);
                if (alert == null)
                {
                    alert = RingtoneManager
                        .GetDefaultUri(RingtoneType.Notification);
                }
            }
            return alert;
        }

        protected void xxOnDestroy()
        {
            base.OnDestroy();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
            {
                SetShowWhenLocked(false);
                SetTurnScreenOn(false);
            }
            else
            {
                Window.ClearFlags(WindowManagerFlags.TurnScreenOn | WindowManagerFlags.ShowWhenLocked | WindowManagerFlags.DismissKeyguard);
                if (ringtone != null)
                    ringtone.Stop();
            }
            base.OnStop();
        }

        protected override void OnStop()
        {
            base.OnStop();           

            if (ringtone != null)
            {
                ringtone.Stop();
                Finish();
            }
        }

    }
}