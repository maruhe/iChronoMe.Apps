using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.App.AlarmManager;

namespace iChronoMe.Droid.GUI.Buzzer
{
    [Activity(Label = "AlarmManagerActivity", Theme = "@style/splashscreen")]
    public class BuzzerManagerActivity : BaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LoadAppTheme();

            LinearLayout ll = new LinearLayout(this);
            ll.Orientation = Orientation.Vertical;
            TextView tv = new TextView(this);
            tv.Text = "es ist wohl ein Wecker gestellt...";

            AlarmManager am = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);

            AlarmClockInfo info = am.NextAlarmClock;
            if (info != null)
            {
                tv.Text += "\n" + new Java.Util.Date(info.TriggerTime).ToString();
            }
            ll.AddView(tv);

            Button btn = new Button(this);
            btn.Text = "Color";
            btn.Click += Btn_Click;
            ll.AddView(btn);

            SetContentView(ll);         
        }

        private async void Btn_Click(object sender, EventArgs e)
        {
          
        }
    }
}