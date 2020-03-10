
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Droid.Source.Adapters;

namespace iChronoMe.Droid.GUI.Service
{
    public class AboutFragment : ActivityFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_service_about, container, false);

            RootView.FindViewById<ListView>(Resource.Id.lv_contributors).Adapter = new ContributorAdapter(Activity);

            RootView.FindViewById<Button>(Resource.Id.btnContact).Click += (s, e) => {
                StartActivity(new Intent(Context, typeof(ContactActivity)));
            };

            return RootView;
        }

        public override void OnResume()
        {
            base.OnResume();
        }
    }
}