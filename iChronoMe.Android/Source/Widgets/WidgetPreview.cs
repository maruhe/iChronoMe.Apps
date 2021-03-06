﻿using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.Droid.Widgets.ActionButton;
using iChronoMe.Droid.Widgets.Calendar;
using iChronoMe.Widgets;

using SkiaSharp.Views.Android;

namespace iChronoMe.Droid.Widgets
{
    public class WidgetPreviewListAdapter<T> : BaseAdapter, IWidgetPreviewListAdapter
        where T : WidgetCfg
    {
        public static string GlobalCachePath { get; } = System.IO.Path.Combine(sys.PathCache, "WidgetPreview");
        public string AdapterCachePath { get; }

        List<WidgetCfgSample<T>> items = null;

        public Activity mContext { get; private set; }
        static IWidgetConfigAssistant<T> mAssistant;
        Point wSize;
        EventCollection myEventsMonth, myEventsList;
        DynamicCalendarModel CalendarModel;
        Drawable WallpaperDrawable;
        LinearLayout llDummy;
        LayoutInflater inflater;
        float nScale;
        //float nImgScale = 0.5F;
        int iHeightPreview;
        int iHeightPreviewWallpaper;
        PartialLoadHandler PartialLoadHandler = null;
        Random rnd = new Random(DateTime.Now.Millisecond);
        ViewGroup Parent = null;
        Color clIconTint = Color.Black;

        public WidgetPreviewListAdapter(Activity context, IWidgetConfigAssistant<T> assistant, System.Drawing.Point size, Drawable wallpaperDrawable, DynamicCalendarModel calendarModel = null, EventCollection eventsMonth = null, EventCollection eventsList = null)
        {
            AdapterCachePath = System.IO.Path.Combine(GlobalCachePath, this.GetHashCode().ToString());
            System.IO.Directory.CreateDirectory(AdapterCachePath);
            WidgetPreviewLoaderFS.StartTimes.Clear();

            mContext = context;
            mAssistant = assistant;
            items = new List<WidgetCfgSample<T>>(mAssistant.Samples);
            PartialLoadHandler = assistant.PartialLoadHandler;
            if (PartialLoadHandler != null)
                PartialLoadHandler.OnListChanged = ParialItemsChaned;
            wSize = new Point(size.X, size.Y);
            WallpaperDrawable = wallpaperDrawable;
            CalendarModel = calendarModel;
            myEventsMonth = eventsMonth;
            myEventsList = eventsList;

            clIconTint = Tools.GetThemeColor(context, Resource.Attribute.iconTint);
            nScale = Math.Min(1F, sys.DisplayShortSiteDp * .9F / wSize.X);
            iHeightPreview = iHeightPreviewWallpaper = (int)(wSize.Y * sys.DisplayDensity * nScale);
            if (WallpaperDrawable != null)
                iHeightPreviewWallpaper = (int)(iHeightPreview + 20 * sys.DisplayDensity);

            llDummy = new LinearLayout(context.ApplicationContext);
            llDummy.LayoutParameters = new LinearLayout.LayoutParams(wSize.X * (int)sys.DisplayDensity, wSize.Y * (int)sys.DisplayDensity);
            inflater = (LayoutInflater)mContext.GetSystemService(Context.LayoutInflaterService);
            CalendarWidgetService.InitEvents();
        }

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
            ViewHolders.Clear();
            ViewHolders = null;
            inflater = null;
        }

        Dictionary<int, int> Times = new Dictionary<int, int>();
        Dictionary<int, ViewHolder> ViewHolders = new Dictionary<int, ViewHolder>();

        public override int Count => PartialLoadHandler != null && !PartialLoadHandler.IsDone ? items.Count + 1 : items.Count;

        public override Java.Lang.Object GetItem(int position)
            => position < items.Count ? items[position] as Java.Lang.Object : rnd.Next(10000);

        public override long GetItemId(int position)
            => position < items.Count ? items[position].GetHashCode() : rnd.Next(10000);

        public override int ViewTypeCount => 1 + (mAssistant.ShowColors ? 1 : 0) + (!mAssistant.ShowPreviewImage ? 1 : 0) + (PartialLoadHandler == null ? 0 : 1);

        public class ViewHolder : Java.Lang.Object, IWidgetViewHolder
        {
            public int Position { get; set; } = -1;
            public long ConfigID { get; set; } = -1;
            public View View { get; private set; }
            public LinearLayout rowlayout { get; private set; }
            public TextView title { get; private set; }
            public LinearLayout colors { get; private set; }
            public ProgressBar progress { get; private set; }
            public ImageView wallpaper { get; private set; }
            public ImageView backimage { get; private set; }
            public ImageView backcolor { get; private set; }
            public ImageView preview { get; private set; }
            public SKCanvasView skia { get; private set; }

            public ViewHolder(View view)
            {
                View = view;
                rowlayout = view.FindViewById<LinearLayout>(Resource.Id.row_layout);
                title = view.FindViewById<TextView>(Resource.Id.title_text);
                colors = view.FindViewById<LinearLayout>(Resource.Id.color_layout);
                progress = view.FindViewById<ProgressBar>(Resource.Id.loading_progress);
                wallpaper = view.FindViewById<ImageView>(Resource.Id.wallpaper_image);
                backimage = view.FindViewById<ImageView>(Resource.Id.background_image);
                backcolor = view.FindViewById<ImageView>(Resource.Id.background_color);
                preview = view.FindViewById<ImageView>(Resource.Id.preview_image);
                skia = view.FindViewById<SKCanvasView>(Resource.Id.preview_skia);
                skia.PaintSurface += Skia_PaintSurface;
            }

            private void Skia_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
            {
                e.Surface.Canvas.Clear();
                if (Position < 0)
                    return;
                try
                {
                    var sample = mAssistant.Samples[Position];
                    var cfg = sample.PreviewConfig ?? sample.WidgetConfig;
                    if (cfg is WidgetCfg_ClockAnalog)
                    {
                        var view = new WidgetView_ClockAnalog();
                        view.ReadConfig(cfg as WidgetCfg_ClockAnalog);
                        view.DrawCanvas(e.Surface.Canvas, DateTime.Today.AddHours(14).AddMinutes(53).AddSeconds(36), e.Info.Width, e.Info.Height);
                    }
                }
                catch { }
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

        public override View GetView(int position, Android.Views.View convertView, ViewGroup parent)
        {
            if (position >= items.Count)
            {
                var v = inflater.Inflate(Resource.Layout.listitem_loading, null);
                v.FindViewById<TextView>(Resource.Id.title).Text = PartialLoadHandler?.LoadingText ?? localize.loading;
                return v;
            }

            xLog.Debug("GetView Widget " + position);

            if (mAssistant.ShowPreviewImage || (mAssistant.ShowFirstPreviewImage && position == 0))
            {
                ViewHolder viewHolder = null;
                if (convertView == null || convertView.Id != Resource.Id.row_layout_widgetprev)
                {
                    Parent = parent;
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
                        viewHolder.ConfigID = -1;
                        ViewHolders.Remove(viewHolder.Position);
                        ViewHolders.Add(viewHolder.Position, viewHolder);
                    }

                    var sample = items[position];
                    string cTitle = sample.Title;

                    if (!cTitle.StartsWith("#"))
                    {
                        viewHolder.title.Text = cTitle;// + ", " + RunningWidgetLoader;
                        viewHolder.title.TextAlignment = TextAlignment.Center;
                        viewHolder.title.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        viewHolder.title.Visibility = ViewStates.Gone;
                    }

                    var cfg = sample.PreviewConfig ?? sample.WidgetConfig;

                    viewHolder.ConfigID = cfg.GetHashCode();
                    viewHolder.colors.RemoveAllViews();
                    viewHolder.colors.Visibility = ViewStates.Gone;
                    if (cTitle.StartsWith("#"))
                    {
                        int size = (int)(20 * sys.DisplayDensity);
                        viewHolder.colors.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(size * 1.2));
                        viewHolder.colors.Visibility = ViewStates.Visible;
                        viewHolder.colors.SetGravity(GravityFlags.Center);

                        LinearLayout llClr = new LinearLayout(mContext);
                        llClr.LayoutParameters = new LinearLayout.LayoutParams(size * 2, size);

                        GradientDrawable shape = new GradientDrawable();
                        shape.SetShape(ShapeType.Rectangle);
                        shape.SetCornerRadii(new float[] { 2, 2, 2, 2, 2, 2, 2, 2 });
                        shape.SetColor(xColor.FromHex(cTitle).ToAndroid());
                        shape.SetStroke((int)Math.Round(sys.DisplayDensity, 0), clIconTint);
                        llClr.Background = shape;

                        viewHolder.colors.AddView(llClr);
                    }
                    else if (mAssistant.ShowColors && sample.Colors != null)
                    {
                        xColor[] clrList = sample.Colors;

                        if (cfg is WidgetCfg_CalendarCircleWave)
                            clrList = (cfg as WidgetCfg_CalendarCircleWave).DayBackgroundGradient.GradientS[0].CustomColors;

                        if (clrList != null && clrList.Length > 0)
                        {
                            int size = (int)(20 * sys.DisplayDensity);
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
                                shape.SetStroke((int)Math.Round(sys.DisplayDensity, 0), clIconTint);
                                llClr.Background = shape;

                                viewHolder.colors.AddView(llClr);
                                i++;
                            }
                        }
                    }

                    viewHolder.preview.SetImageBitmap(null);
                    viewHolder.backimage.SetImageURI(null);
                    viewHolder.backcolor.SetImageDrawable(null);
                    viewHolder.skia.Invalidate();
                    if (cfg is WidgetCfg_ClockAnalog)
                    {
                        var clockCfg = cfg as WidgetCfg_ClockAnalog;
                        if (!string.IsNullOrEmpty(clockCfg.BackgroundImage))
                        {
                            viewHolder.backimage.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(clockCfg.BackgroundImage)));
                            if (clockCfg.BackgroundImageTint == xColor.Transparent)
                                viewHolder.backimage.SetColorFilter(null);
                            else
                                viewHolder.backimage.SetColorFilter(clockCfg.BackgroundImageTint.ToAndroid());
                        }
                        if (clockCfg.ColorBackground.A > 0)
                        {
                            viewHolder.backcolor.SetImageDrawable(DrawableHelper.GetIconDrawable(mContext, Resource.Drawable.circle_shape_max, clockCfg.ColorBackground.ToAndroid()));
                        }
                        viewHolder.progress.Visibility = ViewStates.Gone;
                    }
                    else
                    {

                        if (viewHolder.preview.Drawable is BitmapDrawable && false)
                            ((BitmapDrawable)viewHolder.preview.Drawable).Bitmap?.Recycle();

                        if (cfg == null)
                        {
                            viewHolder.progress.Visibility = ViewStates.Invisible;
                            viewHolder.preview.ScaleX = viewHolder.preview.ScaleY = 1;
                            viewHolder.preview.SetImageResource(Resource.Drawable.icons8_delete);
                            return convertView;
                        }

                        viewHolder.progress.Visibility = ViewStates.Visible;

                        new WidgetPreviewLoaderFS().Execute(this, viewHolder, cfg);
                    }
                }
                catch (Exception ex)
                {
                    xLog.Error(ex, "GetView " + position);
                }
                return convertView;
            }
            else
            {
                var sample = items[position];
                var v = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
                if (sample.Title.StartsWith("#"))
                {
                    GradientDrawable shape = new GradientDrawable();
                    shape.SetShape(ShapeType.Rectangle);
                    shape.SetCornerRadii(new float[] { 2, 2, 2, 2, 2, 2, 2, 2 });
                    shape.SetColor(xColor.FromHex(sample.Title).ToAndroid());
                    shape.SetStroke((int)Math.Round(sys.DisplayDensity, 0), clIconTint);
                    v.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(shape);
                    v.FindViewById<TextView>(Resource.Id.title).Text = sample.Title.Replace("#FF", "#");
                }
                else
                {
                    if (sample.Icon != 0)
                        v.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(sample.Icon);
                    v.FindViewById<TextView>(Resource.Id.title).Text = sample.Title;
                }
                return v;
            }
        }

        public void ParialItemsChaned()
        {
            mContext.RunOnUiThread(() =>
            {
                items = new List<WidgetCfgSample<T>>(mAssistant.Samples);
                NotifyDataSetChanged();
            });
        }

        #region obosled
        /*
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
                                xLog.Debug("thread load Widget Preview " + iPos);
                                var vWidget = GetWidgetView(iPos);
                                xLog.Debug("thread load Widget Preview done " + iPos);
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
                                            this.NotifyDataSetChanged();
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
                                        xLog.Debug(ex, "push generated Widget " + iPos);
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
                xLog.Debug("Generate new Widget Preview " + position);

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
                    xLog.Debug("Generate new Widget Preview " + position + " RVActionButton " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                }
                else if (cfg is WidgetCfg_Clock)
                {
                    if (cfg is WidgetCfg_ClockAnalog)
                    {
                        WidgetView_ClockAnalog wv = new WidgetView_ClockAnalog();
                        wv.ReadConfig((WidgetCfg_ClockAnalog)cfg);
                        bmp = BitmapFactory.DecodeStream(wv.GetBitmap(DateTime.Today.AddHours(14).AddMinutes(53).AddSeconds(36), iWidthPx, iHeightPx, true));
                    }
                }
                else if (cfg is WidgetCfg_Calendar)
                {
                    if (cfg is WidgetCfg_CalendarTimetable && myEventsList.EventsCheckerIsActive)
                    {
                        (cfg as WidgetCfg_CalendarTimetable).ShowLocationSunOffset = false;
                    }
                    rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_universal);
                    xLog.Debug("Generate new Widget Preview " + position + " Start RVCalendarHeader");
                    CalendarWidgetService.GenerateWidgetTitle(mContext, rv, cfg as WidgetCfg_Calendar, wSize, CalendarModel);
                    xLog.Debug("Generate new Widget Preview " + position + " RVCalendarHeader " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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
                        xLog.Debug("Generate new Widget Preview " + position + " RVMonthView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                    }
                    else if (cfg is WidgetCfg_CalendarCircleWave)
                    {
                        CalendarWidgetService.GenerateCircleWaveView(mContext, null, rv, cfg as WidgetCfg_CalendarCircleWave, wSize, 42, CalendarModel, null, myEventsMonth);
                        xLog.Debug("Generate new Widget Preview " + position + " RVCircleWave " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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
                        xLog.Debug("Generate new Widget Preview " + position + " RvToView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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
                                * /
        xLog.Debug("Generate new Widget Preview " + position + " TimeTableData " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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

                        xLog.Debug("Generate new Widget Preview " + position + " MeasureView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;


                        bmp = Bitmap.CreateBitmap(iWidthPx, iHeightPx, Bitmap.Config.Argb8888);
                        Canvas canvas = new Canvas(bmp);
                        vWidget.Draw(canvas);
                        vWidget.Dispose();
                        vWidget = null;

                        xLog.Debug("Generate new Widget Preview " + position + " DrawView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;
                        GC.Collect();
                        xLog.Debug("Generate new Widget Preview " + position + " GC.Collect " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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

                        xLog.Debug("Generate new Widget Preview " + position + " SaveCache " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                        swStart = DateTime.Now;

                        return scale;
                    }
                    catch (IOException e)
                    {
                        xLog.Error(e, "save Widget Preview Cache " + position);

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
                xLog.Error(ex, "Generate new Widget Preview " + position);
                lock (PreviewCache)
                {
                    PreviewCache.Clear();
                    PreviewCache.Add(position, new byte[0]);
                }
            }
            finally
            {
                xLog.Debug("Generate new Widget Preview " + position + " took " + (int)((DateTime.Now - swGenerateStart).TotalMilliseconds) + "ms");
            }
            return null;
        }
        */
        #endregion

        public Bitmap GenerateWidgetPreview(WidgetCfg cfg)
        {
            var swStart = DateTime.Now;

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
                xLog.Debug("Generate new Widget Preview RVActionButton " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                swStart = DateTime.Now;
            }
            else if (cfg is WidgetCfg_Clock)
            {
                if (cfg is WidgetCfg_ClockAnalog)
                {
                    WidgetView_ClockAnalog wv = new WidgetView_ClockAnalog();
                    wv.ReadConfig((WidgetCfg_ClockAnalog)cfg);
                    bmp = BitmapFactory.DecodeStream(wv.GetBitmap(DateTime.Today.AddHours(14).AddMinutes(53).AddSeconds(36), iWidthPx, iHeightPx, true));
                }
            }
            else if (cfg is WidgetCfg_Calendar)
            {
                rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_universal);
                xLog.Debug("Generate new Widget Preview Start RVCalendarHeader");
                CalendarWidgetService.GenerateWidgetTitle(mContext, rv, cfg as WidgetCfg_Calendar, wSize, CalendarModel);
                xLog.Debug("Generate new Widget Preview RVCalendarHeader " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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
                    xLog.Debug("Generate new Widget Preview RVMonthView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                }
                else if (cfg is WidgetCfg_CalendarCircleWave)
                {
                    CalendarWidgetService.GenerateCircleWaveView(mContext, null, rv, cfg as WidgetCfg_CalendarCircleWave, wSize, 42, CalendarModel, null, myEventsMonth);
                    xLog.Debug("Generate new Widget Preview RVCircleWave " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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
                    xLog.Debug("Generate new Widget Preview RvToView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;

                    if (cfg is WidgetCfg_CalendarTimetable)
                    {
                        if (myEventsList.Count > 0)
                        {
                            xLog.Debug("Generate new Widget Preview TimeTableData " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
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

                    xLog.Debug("Generate new Widget Preview MeasureView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;

                    bmp = Bitmap.CreateBitmap(iWidthPx, iHeightPx, Bitmap.Config.Argb8888);
                    Canvas canvas = new Canvas(bmp);
                    vWidget.Draw(canvas);
                    vWidget.Dispose();
                    vWidget = null;

                    xLog.Debug("Generate new Widget Preview DrawView " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                    GC.Collect();
                    xLog.Debug("Generate new Widget Preview GC.Collect " + (int)((DateTime.Now - swStart).TotalMilliseconds) + "ms");
                    swStart = DateTime.Now;
                }
            }
            return bmp;
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
            //WidgetPreviewListAdapter.RemoveClickListeners(v);
            return v;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mContext = null;
            viewsFactory = null;
        }
    }

    public interface IWidgetPreviewListAdapter
    {
        Activity mContext { get; }
        string AdapterCachePath { get; }
        Bitmap GenerateWidgetPreview(WidgetCfg cfg);
    }

    public interface IWidgetViewHolder
    {
        int Position { get; }
        long ConfigID { get; }
        View View { get; }
        LinearLayout rowlayout { get; }
        TextView title { get; }
        LinearLayout colors { get; }
        ProgressBar progress { get; }
        ImageView wallpaper { get; }
        ImageView backimage { get; }
        ImageView preview { get; }
    }
}