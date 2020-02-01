using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Support.V4.App;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.GUI
{
    public abstract class ActivityFragment : Fragment
    {
        public ViewGroup RootView { get; protected set; }

        public virtual void Refresh()
        {

        }

        public virtual void Reinit()
        {

        }
    }
}