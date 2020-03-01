using System;
using System.Threading;

using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;

using iChronoMe.Core.Classes;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets
{
    public class WidgetPreviewLoaderUncached : AsyncTask<object, int, bool>
    {
        private Bitmap bmp = null;
        private IWidgetViewHolder viewHolder;
        private WidgetCfg myCfg;
        private IWidgetPreviewListAdapter adapter;

        protected override bool RunInBackground(params object[] @params)
        {
            try
            {
                adapter = (IWidgetPreviewListAdapter)@params[0];
                viewHolder = (IWidgetViewHolder)@params[1];
                myCfg = (WidgetCfg)@params[2];

                if (viewHolder.ConfigID != myCfg.GetHashCode())
                    return false;

                bmp = adapter.GenerateWidgetPreview(myCfg);

                if (viewHolder.ConfigID != myCfg.GetHashCode())
                {
                    bmp?.Recycle();
                    return false;
                }
                PublishProgress(new int[] { 100 });
                return bmp != null;
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                xLog.Error(e);
                Tools.ShowToast(adapter.mContext, e.Message);
            }
            return false;
        }

        protected override void OnPostExecute(bool result)
        {
            base.OnPostExecute(result);
            bool bBmpWasUsed = false;

            try
            {
                if (viewHolder.ConfigID != myCfg.GetHashCode())
                    return;

                if (!result)
                    return;

                if (viewHolder.preview.Drawable is BitmapDrawable)
                    (viewHolder.preview.Drawable as BitmapDrawable).Bitmap?.Recycle();
                if (bmp == null)
                    viewHolder.preview.SetImageResource(Resource.Drawable.icons8_error_clrd);
                else
                {
                    viewHolder.preview.SetImageBitmap(bmp);
                    bBmpWasUsed = true;
                }
                viewHolder.progress.Visibility = ViewStates.Gone;

                GC.Collect();
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                xLog.Error(e);
                Tools.ShowToast(adapter.mContext, e.Message);
            }
            finally
            {
                if (!bBmpWasUsed)
                    bmp?.Recycle();
            }

            try
            {
                bmp = null;
                viewHolder = null;
                myCfg = null;
                adapter = null;
            }
            catch { }
        }
    }
}