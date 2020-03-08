using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using iChronoMe.Droid.Adapters;

namespace iChronoMe.Droid.GUI.Service
{
    public class FaqFragment : ActivityFragment
    {
        FaqAdapter adapter;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_service_faq, container, false);

            adapter = new FaqAdapter(Activity);
            RootView.FindViewById<ListView>(Resource.Id.lv_faq).Adapter = adapter;
            RootView.FindViewById<ListView>(Resource.Id.lv_faq).ItemClick += FaqFragment_ItemClick;

            return RootView;
        }

        private void FaqFragment_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            string desc = adapter.GetDescription(e.Position);
            if (string.IsNullOrEmpty(desc))
                return;

            new AlertDialog.Builder(Context)
                .SetTitle(adapter[e.Position])
                .SetMessage(desc)
                .SetPositiveButton(Resource.String.action_ok, (s, e) => { })
                .Create()
                .Show();
        }
    }
}