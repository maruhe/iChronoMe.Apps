using System;
using System.Net.Http;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;

namespace iChronoMe.Droid.GUI.Service
{
    [Activity(Label = "Contact and Feedback", Name = "me.ichrono.droid.GUI.Service.ContactActivity")]

    public class ContactActivity : BaseActivity
    {
        ViewGroup content;
        Spinner spTopic;
        EditText etName, etEmail, etSubject, etMessage;
        CheckBox cbIncludeDeviceinfo, cbIncludeLocation;

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

            content = (ViewGroup)LayoutInflater.Inflate(Resource.Layout.fragment_service_contact, frame);

            spTopic = content.FindViewById<Spinner>(Resource.Id.sp_topic);
            etName = content.FindViewById<EditText>(Resource.Id.et_name);
            etEmail = content.FindViewById<EditText>(Resource.Id.et_email);
            etSubject = content.FindViewById<EditText>(Resource.Id.et_subject);
            etMessage = content.FindViewById<EditText>(Resource.Id.et_message);
            cbIncludeDeviceinfo = content.FindViewById<CheckBox>(Resource.Id.cb_include_deviceinfo);
            cbIncludeLocation = content.FindViewById<CheckBox>(Resource.Id.cb_include_location);

            content.FindViewById<Button>(Resource.Id.btn_send).Click += btnSend_Click; ;

            if (Intent.HasExtra("Topic"))
            {
                try
                {
                    spTopic.SetSelection(Intent.GetIntExtra("Topic", -1));
                }
                catch { }
            }
        }

        public override bool OnSupportNavigateUp()
        {
            OnBackPressed();
            return true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (spTopic.SelectedItemPosition < 0)
                {
                    Tools.ShowMessage(this, "hold on", "and select a topic");
                    spTopic.RequestFocus();
                    return;
                }
                if (string.IsNullOrEmpty(etMessage.Text) || etMessage.Text.Length < 3)
                {
                    Tools.ShowMessage(this, "hold on", "and enter a message");
                    etMessage.RequestFocus();
                    return;
                }
                if (string.IsNullOrEmpty(etMessage.Text))
                {
                    Tools.ShowMessage(this, "hold on", "and enter a supject");
                    etMessage.RequestFocus();
                    return;
                }
                string cSendContent = DateTime.Now.ToString("s") + "." + DateTime.Now.Millisecond + "\n" +
                    "topic: " + spTopic.SelectedItem.ToString() + "\n" +
                    "name: " + etName.Text + "\n" +
                    "email: " + etEmail.Text + "\n" +
                    "subject: " + etSubject.Text + "\n" +
                    "message: " + etMessage.Text + "\n\n";

                if (!string.IsNullOrEmpty(sys.cAppVersionInfo))
                    cSendContent += "\nApp: " + sys.cAppVersionInfo;

                if (cbIncludeDeviceinfo.Checked)
                {
                    if (!string.IsNullOrEmpty(sys.cDeviceInfo))
                        cSendContent += "\nDeviceInfo:\n" + sys.cDeviceInfo;
                }
                else
                    cSendContent += "\nDeviceToken: " + sys.cDeviceToken;

                if (cbIncludeLocation.Checked)
                    cSendContent += "\nLocation: " + sys.DezimalGradToGrad(sys.lastUserLocation.Latitude, sys.lastUserLocation.Longitude);

                var dlg = ProgressDlg.NewInstance("sending...");
                dlg.Show(SupportFragmentManager, "");

                new Thread(async () =>
                {
                    string cUrl = Secrets.zAppResponseUrl + "upload.php?app=iChronoMe&type=Feedback&os=" + sys.OsType.ToString();
#if DEBUG
                    cUrl += "&debug";
#endif
                    try
                    {
                        HttpClient client = new HttpClient();
                        HttpContent content = new StringContent(cSendContent);
                        HttpResponseMessage response = await client.PutAsync(cUrl, content);
                        string result = await response.Content.ReadAsStringAsync();

                        dlg.SetProgressDone();
                        Tools.ShowToast(this, result);
                        FinishAndRemoveTask();
                    }
                    catch (Exception ex)
                    {
                        dlg.SetProgressDone();
                        Tools.ShowToast(this, "error sending feedback\n" + ex.Message);
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this, "error", ex.Message);
            }
        }
    }
}