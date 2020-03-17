
using System;
using System.Globalization;
using System.IO;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Source.Adapters;

namespace iChronoMe.Droid.GUI.Service
{
    public class AboutFragment : ActivityFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_service_about, container, false);

            RootView.FindViewById<ListView>(Resource.Id.lv_contributors).Adapter = new ContributorAdapter(Activity);

            RootView.FindViewById<Button>(Resource.Id.btnContact).Click += (s, e) =>
            {
                StartActivity(new Intent(Context, typeof(ContactActivity)));
            };

            RootView.FindViewById<Button>(Resource.Id.btn_privacy).Click += (s, e) =>
            {
                var textView = new TextView(Context);
                textView.Text = "**text not found**\n" + "          :-(";
                try
                {
                    AssetManager assets = Context.Assets;
                    string assetId = "privacy_notice.html";
                    string localId = "privacy_notice_" + CultureInfo.CurrentCulture.TwoLetterISOLanguageName + ".html";
                    textView.Text += "\n" + assetId + "\n" + localId;
                    if (Array.IndexOf(assets.List(""), localId) >= 0)
                        assetId = localId;
                    textView.Text += "\n\n" + assetId;
                    using (StreamReader sr = new StreamReader(assets.Open(assetId)))
                    {
                        if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                        {
#pragma warning disable CS0618 // Typ oder Element ist veraltet
                            textView.TextFormatted = Android.Text.Html.FromHtml(sr.ReadToEnd());
#pragma warning restore CS0618 // Typ oder Element ist veraltet
                        }
                        else
                            textView.TextFormatted = Android.Text.Html.FromHtml(sr.ReadToEnd(), Android.Text.FromHtmlOptions.ModeLegacy);
                    }
                }
                catch (Exception ex)
                {
                    textView.Text += ex.Message;
                    sys.LogException(ex);
                }
                int pad = 5 * sys.DisplayDensity;
                textView.SetPadding(pad, pad, pad, pad);
                textView.MovementMethod = LinkMovementMethod.Instance;
                var scroll = new ScrollView(Context);
                scroll.AddView(textView);
                var dlgToClose = new AlertDialog.Builder(Context)
                            .SetView(scroll)
                            .SetPositiveButton(Resources.GetString(Resource.String.action_close), (s, e) => { })
                        .Create();
                dlgToClose.Show();
                dlgToClose.Window.SetLayout(WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent);
            };

            return RootView;
        }

        public override void OnResume()
        {
            base.OnResume();
        }
    }
}