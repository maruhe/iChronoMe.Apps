
using Android.App;
using Android.OS;

using iChronoMe.Droid.Source.GUI.Settings;

namespace iChronoMe.Droid
{
    [Activity(Label = "BackgroundServiceInfoActivity", Theme = "@style/splashscreen")]
    public class BackgroundServiceInfoActivity : BaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.activity_main);

            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(Toolbar);
        }

        protected override void OnResume()
        {
            base.OnResume();

            SupportFragmentManager.BeginTransaction()
            .Replace(Resource.Id.main_frame, new BackgroundServiceSettingsFragment(true))
            .Commit();
        }
    }
}