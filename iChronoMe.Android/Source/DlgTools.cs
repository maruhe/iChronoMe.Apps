
using Android.App;
using Android.Content;

namespace iChronoMe.Droid
{
    class DlgTools
    {
    }

    public class FinishOnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        Activity myActivity;

        public FinishOnCancelListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            myActivity.FinishAndRemoveTask();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            myActivity = null;
        }
    }
}