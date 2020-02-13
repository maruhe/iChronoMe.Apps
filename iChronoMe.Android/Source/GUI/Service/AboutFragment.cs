using System.Threading.Tasks;

using Android.OS;
using Android.Views;

namespace iChronoMe.Droid.GUI.Service
{
    public class AboutFragment : ActivityFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_service_about, container, false);

            Task.Factory.StartNew(() =>
            {
                Task.Delay(250).Wait();
                Tools.ShowToast(Context, "NotImplementedException");
            });

            return RootView;
        }

        public override void OnResume()
        {
            base.OnResume();
        }
    }
}