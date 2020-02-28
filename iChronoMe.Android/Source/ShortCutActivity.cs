using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;

using iChronoMe.Droid.GUI.Calendar;

namespace iChronoMe.Droid
{
    [Activity(Label = "ShortCutActivity", Name = "me.ichrono.droid.ShortCutActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class ShortCutActivity : AppCompatActivity
    {
        /*
         Shortcuts:

            create_calender_event
            create_alarm
         */
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                string cType = Intent.GetStringExtra("shortcut");
                string cExtra = Intent.GetStringExtra("extra");

                if ("create_calender_event".Equals(cType.ToLower()))
                {
                    var intent = new Intent(this, typeof(EventEditActivity));
                    StartActivity(intent);
                    return;
                }
                if ("edit_calender_event".Equals(cType.ToLower()))
                {
                    var intent = new Intent(this, typeof(EventEditActivity));
                    intent.PutExtra("EventId", cExtra);
                    StartActivity(intent);
                    return;
                }
                Tools.ShowToast(this, "ShortCut: " + cType + " : " + cExtra + ": noNITausprogrammiert");
            }
            finally
            {
                FinishAndRemoveTask();
            }
        }
    }
}