
using Android.Support.V4.App;
using Android.Views;

namespace iChronoMe.Droid.GUI
{
    public abstract class ActivityFragment : Fragment
    {
        public ViewGroup RootView { get; protected set; }
    }
}