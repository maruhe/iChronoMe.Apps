using System;

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