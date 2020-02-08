using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.Content;
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
        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            try
            {

                AlertDialog dialog = new AlertDialog.Builder(Context)
                .SetTitle("Error-Logs")
                .SetSingleChoiceItems(new ErrorLogAdapter(Activity), 0, ItemClicked)
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

        }
    }
}