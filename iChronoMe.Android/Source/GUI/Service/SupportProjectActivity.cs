using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
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

            var frame = FindViewById<FrameLayout>(Resource.Id.main_frame);

            var content = (ViewGroup)LayoutInflater.Inflate(Resource.Layout.fragment_support_project, frame);

            content.FindViewById<Button>(Resource.Id.btnContact).Click += (s, e) => {
                var intent = new Intent(this, typeof(ContactActivity));
                intent.PutExtra("Topic", 2);
                StartActivity(intent);
            };
        }
    }
}