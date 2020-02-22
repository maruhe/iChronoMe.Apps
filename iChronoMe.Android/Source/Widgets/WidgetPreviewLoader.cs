﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using static iChronoMe.Droid.Widgets.WidgetPreviewListAdapter;

namespace iChronoMe.Droid.Widgets
{
    public class WidgetPreviewLoaderUncached : AsyncTask<object, int, bool>
    {
        private Bitmap bmp = null;
        private ViewHolder viewHolder;
        private WidgetCfg myCfg;
        private WidgetPreviewListAdapter adapter;

        protected override bool RunInBackground(params object[] @params)
        {
            try
            {
                adapter = (WidgetPreviewListAdapter)@params[0];
                viewHolder = (ViewHolder)@params[1];
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
            } catch { }
        }        
    }
}