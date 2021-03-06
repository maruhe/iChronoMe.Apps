﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Droid.Widgets;
using iChronoMe.Droid.Widgets.ActionButton;
using iChronoMe.Droid.Widgets.Calendar;
using iChronoMe.Droid.Widgets.Clock;
using iChronoMe.Droid.Widgets.Lifetime;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.GUI.Service
{
    [Activity(Label = "Contact and Feedback", Name = "me.ichrono.droid.GUI.Service.ContactActivity")]

    public class ContactActivity : BaseActivity
    {
        ViewGroup content;
        Spinner spTopic;
        EditText etName, etEmail, etSubject, etMessage;
        CheckBox cbIncludeDeviceinfo, cbIncludeSettings, cbIncludeLocation;

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
            cbIncludeSettings = content.FindViewById<CheckBox>(Resource.Id.sb_include_settings);
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
                var dlg = ProgressDlg.NewInstance("sending...");
                dlg.Show(SupportFragmentManager, "");

                new Thread(async () =>
                {
                    try
                    {
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

                        if (cbIncludeSettings.Checked)
                        {
                            try
                            {
                                cSendContent += "\n\n--------------------------------\n\nWidgets:";

                                var manager = AppWidgetManager.GetInstance(this);
                                int[] clockS = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(AnalogClockWidget)).Name));
                                int[] calendars = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name));
                                int[] buttons = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(ActionButtonWidget)).Name));
                                int[] chronos = manager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));

                                if (clockS.Length + calendars.Length + buttons.Length + chronos.Length == 0)
                                {
                                    cSendContent += "\nnothing found";
                                }
                                else
                                {

                                    var holder = new WidgetConfigHolder();

                                    var samples = new System.Collections.Generic.List<WidgetCfgSample<WidgetCfg>>();
                                    foreach (int i in clockS)
                                    {
                                        var cfg = holder.GetWidgetCfg<WidgetCfg_ClockAnalog>(i, false);
                                        cSendContent += string.Concat("\nAnalogClockWidget ", i, " cfg: ", cfg, " size: ", MainWidgetBase.GetWidgetSize(i, cfg, manager));
                                    }
                                    foreach (int i in calendars)
                                    {
                                        var cfg = holder.GetWidgetCfg<WidgetCfg_Calendar>(i, false);
                                        cSendContent += string.Concat("\nCalendarWidget ", i, " cfg: ", cfg, " size: ", MainWidgetBase.GetWidgetSize(i, cfg, manager));

                                    }
                                    foreach (int i in buttons)
                                    {
                                        var cfg = holder.GetWidgetCfg<WidgetCfg_ActionButton>(i, false);
                                        cSendContent += string.Concat("\nActionButtonWidget ", i, " cfg: ", cfg, " size: ", MainWidgetBase.GetWidgetSize(i, cfg, manager));

                                    }
                                    foreach (int i in chronos)
                                    {
                                        var cfg = holder.GetWidgetCfg<WidgetCfg_Lifetime>(i, false);
                                        cSendContent += string.Concat("\nLifetimeWidget ", i, " cfg: ", cfg, " size: ", MainWidgetBase.GetWidgetSize(i, cfg, manager));

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                cSendContent += "\n" + ex.GetType().Name + "\n" + ex.Message;
                            }

                            foreach (string cfgFile in Directory.GetFiles(sys.PathConfig, "*.cfg"))
                            {
                                try
                                {
                                    if (cfgFile.ToLower().Contains("locationconfig.cfg") && !cbIncludeLocation.Checked)
                                        continue;
                                    cSendContent += "\n\n--------------------------------\n\n" + Path.GetFileName(cfgFile) + "\n" + File.ReadAllText(cfgFile);
                                }
                                catch (Exception ex)
                                {
                                    cSendContent += "\n\n--------------------------------\n\n" + Path.GetFileName(cfgFile) + "\n" + ex.GetType().Name + "\n" + ex.Message;
                                }
                            }

                            cSendContent += "\n\n--------------------------------\n";
                        }

                        string cUrl = Secrets.zAppResponseUrl + "upload.php?app=iChronoMe&type=Feedback&os=" + sys.OsType.ToString();
#if DEBUG
                        cUrl += "&debug";
#endif
                        HttpClient client = new HttpClient();
                        HttpContent content = new StringContent(cSendContent);
                        HttpResponseMessage response = await client.PutAsync(cUrl, content);
                        string result = await response.Content.ReadAsStringAsync();

                        dlg.SetProgressDone();
                        Tools.ShowToast(this, result, !"done".Equals(result));
                        if ("done".Equals(result))
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