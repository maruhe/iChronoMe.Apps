using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Source.Adapters;

namespace iChronoMe.Droid.Source.GUI.Dialogs
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
                .SetTitle("Error-Logs")
                .SetSingleChoiceItems(adapter, 0, ItemClicked)
                .SetPositiveButton("Select", (senderAlert, args) =>
                {
                })
                .SetNegativeButton("Cancel", (senderAlert, args) =>
                {
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

        public event EventHandler OnDialogDismiss;

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);

            OnDialogDismiss?.Invoke(dialog, new EventArgs());
        }
    }
}