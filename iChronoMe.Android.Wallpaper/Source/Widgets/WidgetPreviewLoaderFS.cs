using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Android.Graphics;
using Android.OS;
using Android.Views;

using iChronoMe.Widgets;

namespace iChronoMe.Droid.Wallpaper.Widgets
{
    public class WidgetPreviewLoaderFS : AsyncTask<object, int, bool>
    {
        private string ImagePath;
        private IWidgetViewHolder viewHolder;
        private WidgetCfg myCfg;
        private IWidgetPreviewListAdapter adapter;
        string cInfo = string.Empty;
        public static Dictionary<int, DateTime> StartTimes { get; } = new Dictionary<int, DateTime>();
        DateTime tStart;

        protected override bool RunInBackground(params object[] @params)
        {
            tStart = DateTime.Now;
            Bitmap bmp = null;
            try
            {
                adapter = (IWidgetPreviewListAdapter)@params[0];
                viewHolder = (IWidgetViewHolder)@params[1];
                myCfg = (WidgetCfg)@params[2];

                if (viewHolder.ConfigID != myCfg.GetHashCode())
                    return false;

                if (!Directory.Exists(adapter.AdapterCachePath))
                    return false;

                ImagePath = System.IO.Path.Combine(adapter.AdapterCachePath, myCfg.GetHashCode() + ".prev");
                if (File.Exists(ImagePath))
                {
                    cInfo += "+";
                    return true;
                }
                lock (StartTimes)
                {
                    if (StartTimes.ContainsKey(myCfg.GetHashCode()) && StartTimes[myCfg.GetHashCode()].AddSeconds(1) > DateTime.Now)
                        return false; // another tread is already preparing..

                    if (StartTimes.ContainsKey(myCfg.GetHashCode()))
                        StartTimes.Remove(myCfg.GetHashCode());
                    StartTimes.Add(myCfg.GetHashCode(), DateTime.Now);
                }

                var swStart = DateTime.Now;

                bmp = adapter.GenerateWidgetPreview(myCfg);

                cInfo = (int)(DateTime.Now - swStart).TotalMilliseconds + "gen ";
                swStart = DateTime.Now;

                var stream = new FileStream(ImagePath, FileMode.Create);
                bmp.Compress(Bitmap.CompressFormat.Png, 100, stream);
                stream.Close();

                cInfo += (int)(DateTime.Now - swStart).TotalMilliseconds + "sav ";
                swStart = DateTime.Now;

                bmp.Recycle();
                bmp = null;
                GC.Collect();

                cInfo += (int)(DateTime.Now - swStart).TotalMilliseconds + "gc ";

                if (viewHolder.ConfigID != myCfg.GetHashCode())
                    return false;

                return true;
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                xLog.Error(e);
                Tools.ShowToast(adapter.mContext, e.Message);
            }
            finally
            {
                lock (StartTimes)
                {
                    if (StartTimes.ContainsKey(myCfg.GetHashCode()))
                        StartTimes.Remove(myCfg.GetHashCode());
                }
                bmp?.Recycle(); //to be sure in case of exeption while saving file etc..
            }
            return false;
        }

        protected override void OnPostExecute(bool result)
        {
            if (IsCancelled)
                return;

            try
            {
                if (viewHolder.ConfigID != myCfg.GetHashCode())
                    return;

                if (!result)
                    return;

                var file = new Java.IO.File(ImagePath);

                if (file.Exists())
                    viewHolder.preview.SetImageURI(Android.Net.Uri.FromFile(file));
                else
                    viewHolder.preview.SetImageResource(Resource.Drawable.icons8_error_clrd);
                //if (sys.Debugmode)
                //  viewHolder.title.Text = cInfo + (int)(DateTime.Now - tStart).TotalMilliseconds + "all "; ;
                viewHolder.progress.Visibility = ViewStates.Gone;
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                xLog.Error(e);
                Tools.ShowToast(adapter.mContext, e.Message);
            }

            viewHolder = null;
            myCfg = null;
            adapter = null;
        }
    }
}