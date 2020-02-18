using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Droid.Adapters;

namespace iChronoMe.Droid.GUI.Dialogs
{
    public class ErrorLogDlg : DialogFragment
    {
        ErrorLogAdapter adapter;
        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            try
            {
                adapter = new ErrorLogAdapter(Activity);
                AlertDialog dialog = new AlertDialog.Builder(Context)
                .SetTitle(Resource.String.title_send_errorlog)
                .SetSingleChoiceItems(adapter, 0, ItemClicked)
                .SetPositiveButton(Resource.String.action_send_errorlog_once, (s, e) => { SendLogs(); })
                .SetNegativeButton(Resource.String.action_close, (s, e) =>
                {
                    OnDialogCancel?.Invoke(e, new EventArgs());
                })
                .Create();
                dialog.Show();

                return dialog;
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
                return null;
            }
        }

        protected void ItemClicked(object sender, DialogClickEventArgs e)
        {
            try
            {
                var oFile = adapter.Items[e.Which];
                if (oFile.Tag.ToString().EndsWith(".log"))
                {
                    var dlg = new AlertDialog.Builder(Activity)
                        .SetTitle(oFile.Title1)
                        .SetMessage(File.ReadAllText((string)oFile.Tag))
                        .SetNegativeButton(Resource.String.action_close, (s, e) => { })
                        .Create();

                    dlg.Show();
                    dlg.Window.SetLayout(WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent);
                    dlg.FindViewById<TextView>(Android.Resource.Id.Message).Typeface = Typeface.Monospace;
                }
                else if (oFile.Tag.ToString().EndsWith(".png"))
                {
                    ImageView img = new ImageView(Context);
                    img.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File((string)oFile.Tag)));
                    var dlg = new AlertDialog.Builder(Context)
                        .SetTitle(oFile.Title1)
                        .SetView(img)
                        .SetNegativeButton(Resource.String.action_close, (s, e) => { })
                        .Create();

                    dlg.Show();
                    dlg.Window.SetLayout(WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent);
                }
            }
            catch (Exception ex)
            {
                Tools.ShowToast(Context, ex.Message);
            }
        }

        public event EventHandler OnDialogCancel;

        public override void OnCancel(IDialogInterface dialog)
        {
            base.OnCancel(dialog);
            OnDialogCancel?.Invoke(dialog, new EventArgs());
        }

        public static void SendLogs()
        {
            string cErrorPath = sys.ErrorLogPath;

            if (Directory.Exists(cErrorPath))
            {

                new Thread(async () =>
                {
                    var logS = Directory.GetFiles(cErrorPath);
                    string cUrl = "https://apps.ichrono.me/bugs/upload.php?os=" + sys.OsType.ToString();
#if DEBUG
                    cUrl += "&debug";
#endif
                    foreach (string log in logS)
                    {
                        try
                        {
                            if (!log.EndsWith(".png") || !AppConfigHolder.MainConfig.DenyErrorScreens)
                            {
                                string cFileUrl = cUrl + "&title=" + WebUtility.UrlEncode(System.IO.Path.GetFileName(log));
                                HttpClient client = new HttpClient();
                                HttpContent content = new StreamContent(new FileStream(log, FileMode.Open));// log.EndsWith(".png") ? (HttpContent)new StreamContent(new FileStream(log, FileMode.Open)) : (HttpContent)new StringContent(File.ReadAllText(log));
                                HttpResponseMessage response = await client.PutAsync(cFileUrl, content);
                                string result = await response.Content.ReadAsStringAsync();
                            }
                            File.Delete(log);

                            await Task.Delay(50);
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }

                    Directory.Delete(cErrorPath, true);
                }).Start();
            }
        }
    }
}