
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.GUI.Service
{
    [Activity(Label = "support iChronoMe", Name = "me.ichrono.droid.GUI.Service.SupportProjectActivity")]

    public class SupportProjectActivity : BaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LoadAppTheme();
            SetContentView(Resource.Layout.activity_dummy_frame);
            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(Toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            var frame = FindViewById<FrameLayout>(Resource.Id.main_frame);

            var content = (ViewGroup)LayoutInflater.Inflate(Resource.Layout.fragment_support_project, frame);

            content.FindViewById<Button>(Resource.Id.btnContact).Click += (s, e) =>
            {
                var intent = new Intent(this, typeof(ContactActivity));
                intent.PutExtra("Topic", 2);
                StartActivity(intent);
            };
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }
    }
}