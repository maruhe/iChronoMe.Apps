using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.DeviceCalendar;
using iChronoMe.Droid.Extentions;
using iChronoMe.Droid.Widgets.ActionButton;
using iChronoMe.Droid.Widgets.Calendar;
using iChronoMe.Widgets;
using Java.IO;

namespace iChronoMe.Droid.Widgets
{
    public class WidgetPreviewListAdapter : BaseAdapter
    {
        Activity mContext;
        Point wSize;
        EventCollection myEventsMonth, myEventsList;
        DynamicCalendarModel CalendarModel;
        Drawable WallpaperDrawable;
        LinearLayout llDummy;
        LayoutInflater inflater;
        float nScale;
        float nImgScale = 0.5F;
        int iHeightPreview;
        int iHeightPreviewWallpaper;

        public WidgetPreviewListAdapter(Activity context, System.Drawing.Point size, DynamicCalendarModel calendarModel, EventCollection eventsMonth, EventCollection eventsList, Drawable wallpaperDrawable)
        {
            mContext = context;
            wSize = new Point(size.X, size.Y);
            CalendarModel = calendarModel;
            myEventsMonth = eventsMonth;
            myEventsList = eventsList;
            WallpaperDrawable = wallpaperDrawable;

            nScale = Math.Min(1F, sys.DisplayShortSiteDp * .9F / wSize.X);
            iHeightPreview = iHeightPreviewWallpaper = (int)(wSize.Y * sys.DisplayDensity * nScale);
            if (WallpaperDrawable != null)
                iHeightPreviewWallpaper = iHeightPreview + 20 * sys.DisplayDensity;

            llDummy = new LinearLayout(context.ApplicationContext);
            llDummy.LayoutParameters = new LinearLayout.LayoutParams(wSize.X * (int)sys.DisplayDensity, wSize.Y * (int)sys.DisplayDensity);
            inflater = (LayoutInflater)mContext.GetSystemService(Context.LayoutInflaterService);
            CalendarWidgetService.InitEvents();
        }

        public bool ShowColorList = false;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mContext = null;
            wSize = null;
            CalendarModel = null;
            myEventsMonth = null;
            myEventsList = null;
            WallpaperDrawable = null;
            llDummy = null;
            Items.Clear();
            Items = null;
            ViewsToLoad.Clear();
            ViewsToLoad = null;
            ViewsInLoading.Clear();
            ViewsInLoading = null;
            PreviewCache.Clear();
            PreviewCache = null;
            ViewHolders.Clear();
            ViewHolders = null;
            inflater = null;
        }

        public Dictionary<string, WidgetCfg> Items { get; private set; } = new Dictionary<string, WidgetCfg>();
        OrderedDictionary ViewsToLoad = new OrderedDictionary();
        List<int> ViewsInLoading = new List<int>();
        OrderedDictionary PreviewCache = new OrderedDictionary();
        Dictionary<int, int> Times = new Dictionary<int, int>();
        Dictionary<int, ViewHolder> ViewHolders = new Dictionary<int, ViewHolder>();
        int RunningWidgetLoader = 0;

        public override int Count => Items.Count;

        public override Java.Lang.Object GetItem(int position)
        {
            return this;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        private class ViewHolder : Java.Lang.Object
        {
            public int Position = -1;
            public View View;
            public LinearLayout rowlayout;
            public TextView title;
            public LinearLayout colors;
            public ProgressBar progress;
            public ImageView wallpaper;
            public ImageView preview;

            public ViewHolder(View view)
            {
                View = view;
                rowlayout = view.FindViewById<LinearLayout>(Resource.Id.row_layout);
                title = view.FindViewById<TextView>(Resource.Id.title_text);
                colors = view.FindViewById<LinearLayout>(Resource.Id.color_layout);
                progress = view.FindViewById<ProgressBar>(Resource.Id.loading_progress);
                wallpaper = view.FindViewById<ImageView>(Resource.Id.wallpaper_image);
                preview = view.FindViewById<ImageView>(Resource.Id.preview_image);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                title = null;
                colors = null;
                progress = null;
                wallpaper = null;
                preview = null;
            }
        }

        public override Android.Views.View GetView(int position, Android.Views.View convertView, ViewGroup parent)
        {
            Log.Debug("WidgetPreviewListAdapter", "GetView Widget " + position);
            ViewHolder viewHolder = null;
            if (convertView == null)
            {
                convertView = inflater.Inflate(Resource.Layout.widget_preview_row_layout, null);
                convertView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, iHeightPreviewWallpaper);
                viewHolder = new ViewHolder(convertView);
                convertView.Tag = viewHolder;
                if (WallpaperDrawable != null)
                {
                    viewHolder.wallpaper.SetMaxHeight(iHeightPreviewWallpaper);
                    viewHolder.wallpaper.SetImageDrawable(WallpaperDrawable);
                    viewHolder.preview.SetPadding(0, (iHeightPreviewWallpaper - iHeightPreview) / 2, 0, (iHeightPreviewWallpaper - iHeightPreview) / 2);
                }
            }
            try
            {
                lock (ViewHolders)
                {
                    viewHolder = (ViewHolder)convertView.Tag;
                    if (viewHolder.Position >= 0)
                        ViewHolders.Remove(viewHolder.Position);
                    viewHolder.Position = position;
                    ViewHolders.Remove(viewHolder.Position);
                    ViewHolders.Add(viewHolder.Position, viewHolder);
                }

                string cTitle = new List<string>(Items.Keys)[position];

                if (!cTitle.StartsWith("#"))
                {
                    viewHolder.title.Text = cTitle + ", " + RunningWidgetLoader;
                    viewHolder.title.TextAlignment = TextAlignment.Center;
                    viewHolder.title.Visibility = ViewStates.Visible;
                }
                else
                {
                    viewHolder.title.Visibility = ViewStates.Gone;
                }

                var cfg = Items[cTitle];

                viewHolder.colors.RemoveAllViews();
                viewHolder.colors.Visibility = ViewStates.Gone;
                if (cTitle.StartsWith("#"))
                {
                    int size = 20 * sys.DisplayDensity;
                    viewHolder.colors.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(size * 1.2));
                    viewHolder.colors.Visibility = ViewStates.Visible;
                    viewHolder.colors.SetGravity(GravityFlags.Center);

                    LinearLayout llClr = new LinearLayout(mContext);
                    llClr.LayoutParameters = new LinearLayout.LayoutParams(size * 2, size);

                    GradientDrawable shape = new GradientDrawable();
                    shape.SetShape(ShapeType.Rectangle);
                    shape.SetCornerRadii(new float[] { 2, 2, 2, 2, 2, 2, 2, 2 });
                    shape.SetColor(xColor.FromHex(cTitle).ToAndroid());
                    shape.SetStroke(sys.DisplayDensity, Color.Black);
                    llClr.Background = shape;

                    viewHolder.colors.AddView(llClr);
                }
                else if (ShowColorList && cfg != null)
                {
                    List<xColor> clrList = null;

                    if (cfg is WidgetCfg_CalendarCircleWave)
                        clrList = new List<xColor>((cfg as WidgetCfg_CalendarCircleWave).DayBackgroundGradient.GradientS[0].CustomColors);

                    if (clrList != null && clrList.Count > 0)
                    {
                        int size = 20 * sys.DisplayDensity;
                        viewHolder.colors.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(size * 1.2));
                        viewHolder.colors.Visibility = ViewStates.Visible;
                        viewHolder.colors.SetGravity(GravityFlags.Center);
                        int i = 0;
                        foreach (var clr in clrList)
                        {
                            if (i > 0)
                                viewHolder.colors.AddView(new LinearLayout(mContext) { LayoutParameters = new LinearLayout.LayoutParams(size / 2, size) });
                            LinearLayout llClr = new LinearLayout(mContext);
                            llClr.LayoutParameters = new LinearLayout.LayoutParams(size, size);

                            GradientDrawable shape = new GradientDrawable();
                            shape.SetShape(ShapeType.Rectangle);
                            shape.SetCornerRadii(new float[] { 2, 2, 2, 2, 2, 2, 2, 2 });
                            shape.SetColor(clr.ToAndroid());
                            shape.SetStroke(sys.DisplayDensity, Color.Black);
                            llClr.Background = shape;

                            viewHolder.colors.AddView(llClr);
                            i++;
                        }
                    }
                }

                if (viewHolder.preview.Drawable is BitmapDrawable && false)
                    ((BitmapDrawable)viewHolder.preview.Drawable).Bitmap?.Recycle();

                if (cfg == null)
                {
                    viewHolder.progress.Visibility = ViewStates.Invisible;
                    viewHolder.preview.ScaleX = viewHolder.preview.ScaleY = 1;
                    viewHolder.preview.SetImageResource(Resource.Drawable.icons8_delete);
                    return convertView;
                }

                byte[] byteArray = null;
                lock (PreviewCache)
                {
                    if (PreviewCache.Contains(position))
                        byteArray = (byte[])PreviewCache[(object)position];
                }

                //byteArray = new byte[0];

                if (byteArray != null)
                {
                    viewHolder.progress.Visibility = ViewStates.Invisible;

                    if (byteArray.Length > 0)
                    {
                        Bitmap bmp = BitmapFactory.DecodeByteArray(byteArray, 0, byteArray.Length);//, new BitmapFactory.Options() { InSampleSize = 2 });
                        viewHolder.preview.ScaleX = viewHolder.preview.ScaleY = 1;// .7F / nImgScale;
                        viewHolder.preview.SetImageBitmap(bmp);
                        viewHolder.title.Text = cTitle + ", " + Times[position] + "ms";
                        //if (sys.Debugmode)
                          //  viewHolder.title.Text = byteArray.Length.ToString("N0") + " : " + bmp.AllocationByteCount.ToString("N0") + ", " + Times[position] + "ms";
                    }
                    else
                    {
                        viewHolder.preview.SetImageResource(Resource.Drawable.icons8_delete);
                        viewHolder.title.Text = "empty..";
                    }
                }
                else
                {
                    viewHolder.progress.Visibility = ViewStates.Visible;
                    viewHolder.preview.SetImageBitmap(null);

                    if (!ViewsToLoad.Contains(position) && !ViewsInLoading.Contains(position))
                    {
                        Log.Debug("WidgetPreviewListAdapter", "Add generate Widget " + position);
                        lock (ViewsToLoad)
                        {
                            ViewsToLoad.Add(position, viewHolder);
                        }

                        StartALoader(parent);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("WidgetPreviewListAdapter", ex.AsTr(), "GetView " + position);
            }
            return convertView;
        }

        void StartALoader(ViewGroup parent)
        {
            if (RunningWidgetLoader < Math.Max(1, System.Environment.ProcessorCount - 1))
            {
                RunningWidgetLoader++;
                var loader = new Thread(() =>
                {
                    Task.Delay(100 * RunningWidgetLoader).Wait();
                    try
                    {
                        while (ViewsToLoad.Count > 0)
                        {
                            int iPos = 0;
                            ViewHolder xHolder = null;
                            lock (ViewsToLoad)
                            {
                                xHolder = (ViewHolder)ViewsToLoad[ViewsToLoad.Count - 1];
                                iPos = xHolder.Position;
                                ViewsToLoad.RemoveAt(ViewsToLoad.Count - 1);
                                while (ViewsToLoad.Count > 7 && false)
                                {
                                    ViewsToLoad.RemoveAt(0);
                                }
                            }

                            if (PreviewCache.Contains(iPos))
                                continue;

                            lock (ViewsInLoading)
                                ViewsInLoading.Add(iPos);

                            var tsk = new Thread(() =>
                            {
                                Log.Debug("WidgetPreviewListAdapter", "thread load Widget Preview " + iPos);
                                var vWidget = GetWidgetView(iPos);
                                Log.Debug("WidgetPreviewListAdapter", "thread load Widget Preview done " + iPos);
                                if (mContext == null)
                                    return;
                                lock (ViewsInLoading)
                                {
                                    if (ViewsInLoading.Contains(iPos))
                                        ViewsInLoading.Remove(iPos);
                                }
                                if (vWidget == null)
                                    return;

                                mContext.RunOnUiThread(() =>
                                {
                                    try
                                    {
                                        if (parent is ListView && (ViewsInLoading.Count == 0 || ViewsToLoad.Count == 0 || xHolder.Position == iPos))
                                            (parent as ListView).InvalidateViews();
                                        vWidget.Recycle();
                                        return;

                                        xHolder = null;
                                        lock (ViewHolders)
                                        {
                                            if (ViewHolders.ContainsKey(iPos))
                                                xHolder = ViewHolders[iPos];
                                        }
                                        if (xHolder != null)
                                        {
                                            xHolder.preview.ScaleX = xHolder.preview.ScaleY = .7F / nImgScale;
                                            xHolder.preview.SetImageBitmap(vWidget);
                                            xHolder.title.Text += ", direct, " + Times[iPos] + "ms";
                                        }
                                        else
                                        {
                                            vWidget.Recycle();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Debug("WidgetPreviewListAdapter", ex.AsTr(), "push generated Widget " + iPos);
                                    }
                                });
                            });
                            try
                            {
                                tsk.IsBackground = true;
                                tsk.Start();
                                tsk.Join();
                            }
                            catch { }

                            Thread.Sleep(1);
                        }
                    }
                    catch { }
                    finally
                    {
                        RunningWidgetLoader--;
                    }
                });
                loader.IsBackground = true;
                loader.Start();
            }
        }

        public Bitmap GetWidgetView(int position)
        {
            if (false)
            {
                PreviewCache.Add(position, new byte[0]);
                Times.Add(position, 7);
                return null;
            }

            var swStart = DateTime.Now;
            var swGenerateStart = swStart;
            try
            {
                Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position);

                var cfg = new List<WidgetCfg>(Items.Values)[position];
                if (cfg == null) //gelöschte Widgets anzeigen, schönere Lösung anstreben
                {
                    PreviewCache.Clear();
                    PreviewCache.Add(position, new byte[0]);
                    return null;
                }

                cfg = (WidgetCfg)cfg.Clone();

                int iWidthPx = (int)(wSize.X * sys.DisplayDensity);
                int iHeightPx = (int)(wSize.Y * sys.DisplayDensity);

                Bitmap bmp = null;
                RemoteViews rv = null;
                if (cfg is WidgetCfg_ActionButton)
                {
                    var dToday = CalendarModel.GetDateFromUtcDate(DateTime.Now);
                    int iDayCount = CalendarModel.GetDaysOfMonth(dToday.Year, dToday.Month);
                    int iDay = dToday.DayOfYear;
                    iDayCount = CalendarModel.GetDaysOfYear(dToday.Year);
                    float nHour = (float)DateTime.Now.TimeOfDay.TotalHours;

                    rv = ActionButtonService.DrawButton(mContext, cfg as WidgetCfg_ActionButton, wSize, null, -1, nHour, iDay, iDayCount, true);
                    Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " RVActionButton " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                }
                else if (cfg is WidgetCfg_Clock)
                {
                    if (cfg is WidgetCfg_ClockAnalog)
                    {
                        WidgetView_ClockAnalog wv = new WidgetView_ClockAnalog();
                        wv.ReadConfig((WidgetCfg_ClockAnalog)cfg);
                        bmp = BitmapFactory.DecodeStream(wv.GetBitmap(DateTime.Today.AddHours(14).AddMinutes(53).AddSeconds(36), iWidthPx, iHeightPx));
                    }
                }
                else if (cfg is WidgetCfg_Calendar)
                {
                    if (cfg is WidgetCfg_CalendarTimetable && myEventsList.EventsCheckerIsActive)
                    {
                        (cfg as WidgetCfg_CalendarTimetable).ShowLocationSunOffset = false;
                    }
                    rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_universal);
                    Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " Start RVCalendarHeader");
                    CalendarWidgetService.GenerateWidgetTitle(mContext, rv, cfg as WidgetCfg_Calendar, wSize, CalendarModel);
                    Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " RVCalendarHeader " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                    if (cfg is WidgetCfg_CalendarTimetable)
                    {
                        //CalendarWidgetService.AddDummyListEvents(mContext, rv, cfg as WidgetCfg_CalendarTimetable, wSize, CalendarModel, myEventsList);
                    }
                    else if (cfg is WidgetCfg_CalendarMonthView)
                    {
                        rv.SetViewVisibility(Resource.Id.header_layout, ViewStates.Visible);
                        rv.SetViewVisibility(Resource.Id.list_layout, ViewStates.Visible);
                        CalendarWidgetService.GenerateWidgetMonthView(mContext, rv, cfg as WidgetCfg_CalendarMonthView, wSize, 42, CalendarModel, null, myEventsMonth);
                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " RVMonthView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                    }
                    else if (cfg is WidgetCfg_CalendarCircleWave)
                    {
                        CalendarWidgetService.GenerateCircleWaveView(mContext, null, rv, cfg as WidgetCfg_CalendarCircleWave, wSize, 42, CalendarModel, null, myEventsMonth);
                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " RVCircleWave " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                    }
                }
                if (bmp == null)
                {
                    if (rv != null)
                    {
                        swStart = DateTime.Now;
                        var vWidget = rv.Apply(mContext.ApplicationContext, llDummy);
                        rv.Dispose();

                        rv = null;
                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " RvToView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;

                        if (cfg is WidgetCfg_CalendarTimetable)
                        {
                            if (myEventsList.Count > 0)
                            {
                                /*vWidget.FindViewById(Resource.Id.empty_view).Visibility = ViewStates.Gone;
                                var list = vWidget.FindViewById<LinearLayout>(Resource.Id.list_layout);
                                list.Visibility = ViewStates.Visible;
                                var viewsFactory = new CalendarEventListRemoteViewsFactory(mContext, cfg as WidgetCfg_CalendarTimetable, wSize, CalendarModel, myEventsList);
                                for (int i = 0; i < viewsFactory.Count; i++)
                                {
                                    var v = viewsFactory.GetViewAt(i, false).Apply(mContext, list);
                                    RemoveClickListeners(v);
                                    if (v is RelativeLayout)
                                        (v as ViewGroup).LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 60 * sys.DisplayDensity);
                                    else
                                        (v as ViewGroup).LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 25 * sys.DisplayDensity);
                                    list.AddView(v);
                                }
                                */
                                Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " TimeTableData " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                                swStart = DateTime.Now;
                                vWidget.FindViewById(Resource.Id.empty_view).Visibility = ViewStates.Gone;
                                vWidget.FindViewById(Resource.Id.event_list).Visibility = ViewStates.Visible;
                                vWidget.FindViewById<ListView>(Resource.Id.event_list).Adapter = new EventListFakeAdapter(mContext, cfg as WidgetCfg_CalendarTimetable, wSize, CalendarModel, myEventsList);
                            }
                        }

                        var iLp = new RelativeLayout.LayoutParams(iWidthPx, iHeightPx);
                        vWidget.LayoutParameters = iLp;

                        vWidget.Measure(View.MeasureSpec.MakeMeasureSpec(View.MeasureSpec.GetSize(iWidthPx), MeasureSpecMode.Exactly), View.MeasureSpec.MakeMeasureSpec(View.MeasureSpec.GetSize(iHeightPx), MeasureSpecMode.Exactly));
                        vWidget.Layout(0, 0, iWidthPx, iHeightPx);

                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " MeasureView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;


                        bmp = Bitmap.CreateBitmap(iWidthPx, iHeightPx, Bitmap.Config.Argb8888);
                        Canvas canvas = new Canvas(bmp);
                        vWidget.Draw(canvas);
                        vWidget.Dispose();
                        vWidget = null;

                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " DrawView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                        GC.Collect();
                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " GC.Collect " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                    }
                }
                if (bmp != null) 
                {
                    try
                    {
                        var scale = bmp;
                        //var scale = Bitmap.CreateScaledBitmap(bmp, (int)(iWidthPx * nImgScale), (int)(iHeightPx * nImgScale), false);
                        //bmp.Recycle();
                        var stream = new System.IO.MemoryStream();
                        scale.Compress(Bitmap.CompressFormat.Png, 100, stream);
                        byte[] byteArray = stream.ToArray();
                        //scale.Recycle();

                        if (mContext == null)
                            return null;

                        lock (PreviewCache)
                        {
                            PreviewCache.Remove((object)position);
                            PreviewCache.Add(position, byteArray);

                            Times.Remove(position);
                            Times.Add(position, (int)(DateTime.Now - swGenerateStart).TotalMilliseconds);

                            //while (PreviewCache.Count > 50)
                            //    PreviewCache.RemoveAt(8);
                        }

                        Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " SaveCache " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;

                        return scale;
                    }
                    catch (IOException e)
                    {
                        Log.Error("WidgetPreviewListAdapter", e, "save Widget Preview Cache " + position);

                        lock (PreviewCache)
                        {
                            PreviewCache.Clear();
                            PreviewCache.Add(position, new byte[0]);

                            Times.Remove(position);
                            Times.Add(position, (int)(DateTime.Now - swGenerateStart).TotalMilliseconds);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("WidgetPreviewListAdapter", ex.AsTr(), "Generate new Widget Preview " + position);
                lock (PreviewCache)
                {
                    PreviewCache.Clear();
                    PreviewCache.Add(position, new byte[0]);
                }
            }
            finally
            {
                Log.Debug("WidgetPreviewListAdapter", "Generate new Widget Preview " + position + " took " + (int)((DateTime.Now - swGenerateStart).TotalMilliseconds) + "ms");
            }
            return null;
        }

        public static void RemoveClickListeners(View v)
        {
            v.Clickable = false;
            if (v is ViewGroup)
            {
                var vg = (v as ViewGroup);
                for (int i = 0; i < vg.ChildCount; i++)
                    RemoveClickListeners(vg.GetChildAt(i));
            }
        }
    }

    public class EventListFakeAdapter : BaseAdapter
    {
        Context mContext;
        CalendarEventListRemoteViewsFactory viewsFactory;

        public EventListFakeAdapter(Context context, WidgetCfg_CalendarTimetable cfg, Android.Graphics.Point size, DynamicCalendarModel calendarModel, EventCollection events)
        {
            mContext = context;
            viewsFactory = new CalendarEventListRemoteViewsFactory(context, cfg, size, calendarModel, events);
        }

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override bool IsEnabled(int position)
        {
            return false;
        }

        public override int Count => viewsFactory.Count;

        public override Java.Lang.Object GetItem(int position)
            => viewsFactory.GetItemId(position);

        public override long GetItemId(int position)
            => viewsFactory.GetItemId(position);

        public override Android.Views.View GetView(int position, Android.Views.View convertView, ViewGroup parent)
        {
            var v = viewsFactory.GetViewAt(position, false).Apply(mContext.ApplicationContext, parent);
            WidgetPreviewListAdapter.RemoveClickListeners(v);
            return v;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mContext = null;
            viewsFactory = null;
        }
    }
}