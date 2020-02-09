using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;

namespace iChronoMe.Droid
{
    [Activity(Label = "ShortCutActivity", Name = "me.ichrono.droid.ShortCutActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class ShortCutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //SetTheme(Resource.Style.MainTheme);

            string cType = Intent.GetStringExtra("shortcut");
            string cExtra = Intent.GetStringExtra("extra");

            Tools.ShowToast(this, "ShortCut: " + cType + " : " + cExtra);
        }

        protected override void OnStop()
        {
            base.OnStop();
            Finish();
        }
    }
}