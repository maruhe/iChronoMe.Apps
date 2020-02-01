using Android.App;
using Android.OS;
using Android.Support.V7.App;

namespace iChronoMe.Droid
{
    [Activity(Label = "ShortCutActivity", Name = "mobi.jonny.RealDateTime.ShortCutActivity", Theme = "@style/TransparentTheme", NoHistory = true)]
    public class ShortCutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
    }
}
/*
            BaseCrossActivity.currentActivity = this;
            SetTheme(Resource.Style.MainTheme);

            BaseInit.Check(this);

            if (Xamarin.Forms.Application.Current == null)
                global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Xamarin.FormsGoogleMaps.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            OxyPlot.Xamarin.Forms.Platform.Android.PlotViewRenderer.Init();
            
            Syncfusion.XForms.Android.PopupLayout.SfPopupLayoutRenderer.Init();

            PanCardView.Droid.CardsViewRenderer.Preserve();

            SetContentView(Resource.Layout.shortcutactivity_layout);
        }

        protected override void OnResume()
        {
            base.OnResume();

            //StartActivity(new Intent(this, typeof(MonnTestActivity)));

            Task.Factory.StartNew(() =>
            {
                Task.Delay(250);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    string cType = Intent.GetStringExtra("shortcut");
                    string cExtra = Intent.GetStringExtra("extra");

                    var mPage = new ShortCutHolderPage(cType, cExtra);
                    Android.Support.V4.App.Fragment mFragment = mPage.CreateSupportFragment(this);

                    SupportFragmentManager
                        .BeginTransaction()
                        .Replace(Resource.Id.fragment_frame_layout, mFragment)
                        .Commit();

                    await mPage.PagePoppedTask;
                    Finish();

                });
            });
        }

        public override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
            {
                // Do something if there are some pages in the `PopupStack`
            }
            else
            {
                // Do something if there are not any pages in the `PopupStack`
            }
            Task.Factory.StartNew(() =>
            {
                Task.Delay(100).Wait();
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count == 0)
                        base.OnBackPressed();
                });
            });
        }

        protected override void OnStop()
        {
            base.OnStop();
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}*/