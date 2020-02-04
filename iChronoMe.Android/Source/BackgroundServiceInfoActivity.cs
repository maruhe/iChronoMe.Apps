
using Android.App;
using Android.OS;

namespace iChronoMe.Droid
{
    [Activity(Label = "BackgroundServiceInfoActivity", Theme = "@style/splashscreen")]
    public class BackgroundServiceInfoActivity : BaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetTheme(Resource.Style.AppTheme);
        }
    }
}