
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;

namespace iChronoMe.Droid.GUI
{
    public abstract class ActivityFragment : Fragment
    {
        public ViewGroup RootView { get; protected set; }

        public BaseActivity mContext { get; private set; } = null;

        public override void OnAttach(Android.Content.Context context)
        {
            base.OnAttach(context);
            if (context is BaseActivity)
                mContext = context as BaseActivity;
        }

        public override void OnResume()
        {
            base.OnResume();

            if (Activity is BaseActivity)
                mContext = Activity as BaseActivity;

            this.Activity?.InvalidateOptionsMenu();
        }

        public Task<bool> RequestPermissionsTask { get { return tcsRP == null ? Task.FromResult(false) : tcsRP.Task; } }
        private TaskCompletionSource<bool> tcsRP = null;

        protected async Task<bool> RequestPermissionsAsync(string[] permissions, int requestCode)
        {
            tcsRP = new TaskCompletionSource<bool>();
            RequestPermissions(permissions, requestCode);
            await RequestPermissionsTask;
            return RequestPermissionsTask.Result;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            var res = true;
            foreach (var grand in grantResults)
                res = res && grand == Permission.Granted;
            tcsRP?.TrySetResult(res);
        }
    }
}