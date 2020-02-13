using Android.OS;
using Android.Views;

namespace iChronoMe.Droid.GUI.Service
{
    public class FaqFragment : ActivityFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_service_faq, container, false);

            return RootView;
        }
    }
}