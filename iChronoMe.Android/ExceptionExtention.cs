using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Extentions
{
    public static class ExceptionExtention
    {
        public static Java.Lang.Throwable AsTr(this Exception ex)
        {
            return Java.Lang.Throwable.FromException(ex);
        }
    }
}