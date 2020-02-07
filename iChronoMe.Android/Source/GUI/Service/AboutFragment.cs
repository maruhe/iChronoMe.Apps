using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.GUI;

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