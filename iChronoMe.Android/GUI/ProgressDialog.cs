using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

using Xamarin.Essentials;

namespace iChronoMe.Droid.GUI
{
    public class ProgressDialog : DialogFragment
    {
        public static ProgressDialog NewInstance(string cTitle)
        {
            var b = new Bundle();
            b.PutString("Title", cTitle);
            return NewInstance(b);
        }

        public static ProgressDialog NewInstance(Bundle bundle)
        {
            ProgressDialog fragment = new ProgressDialog();
            fragment.Arguments = bundle;
            return fragment;
        }

        ProgressBar progress;
        TextView message;

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            if (savedInstanceState != null)
                Arguments = savedInstanceState;

            this.Cancelable = false;

            progress = new ProgressBar(Context, null, Android.Resource.Attribute.ProgressBarStyleHorizontal);
            progress.Indeterminate = true;
            message = new TextView(Context) { Visibility = ViewStates.Gone };
            message.TextAlignment = TextAlignment.Center;
            LinearLayout ll = new LinearLayout(Context) { Orientation = Orientation.Vertical };
            ll.AddView(progress);
            ll.AddView(message, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));

            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Arguments.GetString("Title", Resources.GetString(Resource.String.just_a_moment)));
            alert.SetView(ll);
            alert.SetCancelable(false);

            return alert.Create();
        }

        public void SetProgress(int pos, int max, string cMessage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (max > 0)
                {
                    progress.Max = max;
                    progress.Progress = pos;
                    progress.Indeterminate = false;
                }
                else
                    progress.Indeterminate = true;

                if (string.IsNullOrEmpty(cMessage))
                    message.Visibility = ViewStates.Gone;
                else
                {
                    message.Text = cMessage;
                    message.Visibility = ViewStates.Visible;
                }
            });
        }

        public void SetProgressDone()
        {
            if (this.Dialog != null)
                MainThread.BeginInvokeOnMainThread(() => this.Dialog.Cancel());
        }
    }
}