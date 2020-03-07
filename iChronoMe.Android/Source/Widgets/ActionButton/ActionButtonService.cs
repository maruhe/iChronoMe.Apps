using System;
using System.Collections.Generic;
using System.Threading;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.App;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Widgets;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [Service(Label = "ActionButton Update-Service", Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    public class ActionButtonService : JobIntentService
    {
        public override void OnCreate()
        {
            base.OnCreate();

            SetTheme(Resource.Style.AppTheme_iChronoMe_Dark);
            lth = LocationTimeHolder.LocalInstance;
        }

        public static bool ResetData = false;
        const int MY_JOB_ID = 2124;
        public static DateTime LastWorkStart { get; private set; } = DateTime.MinValue;

        public static void EnqueueWork(Context context, Intent work)
        {
            Java.Lang.Class cls = Java.Lang.Class.FromType(typeof(ActionButtonService));
            try
            {
                EnqueueWork(context, cls, MY_JOB_ID, work);
            }
            catch (Exception ex)
            {
                xLog.Debug(ex, "Exception: {0}");
            }
        }

        static Dictionary<int, Thread> RunningTaskS = new Dictionary<int, Thread>();
        public static LocationTimeHolder lth;

        protected override void OnHandleWork(Intent intent)
        {
            xLog.Debug("OnUpdate");
            DateTime swStart = DateTime.Now;

            var cfgHolder = new WidgetConfigHolder();

            var appWidgetManager = AppWidgetManager.GetInstance(this);
            int[] appWidgetIDs = intent.GetIntArrayExtra("appWidgetIds");
            if (appWidgetIDs == null || appWidgetIDs.Length < 1)
                appWidgetIDs = appWidgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(ActionButtonWidget)).Name));

            foreach (int iWidgetId in appWidgetIDs)
            {
                lock (RunningTaskS)
                {
                    if (RunningTaskS.ContainsKey(iWidgetId))
                    {
                        try
                        {
                            ResetData = false;
                            RunningTaskS[iWidgetId].Abort();
                        }
                        catch { };
                        if (RunningTaskS.ContainsKey(iWidgetId))
                            RunningTaskS.Remove(iWidgetId);
                        //continue;
                    }
                }

                var tr = new Thread(() =>
                {
                    xLog.Debug("UpdateWidget: " + iWidgetId);
                    try
                    {
                        LastWorkStart = DateTime.Now;

                        TimeSpan tsPreInit = DateTime.Now - swStart;
                        swStart = DateTime.Now;
                        var cfg = cfgHolder.GetWidgetCfg<WidgetCfg_ActionButton>(iWidgetId, false);

                        Point wSize = MainWidgetBase.GetWidgetSize(iWidgetId, cfg, appWidgetManager);

                        var calendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();
                        DynamicDate dToday = calendarModel.GetDateFromUtcDate(DateTime.Now);

                        int iDayCount = calendarModel.GetDaysOfMonth(dToday.Year, dToday.Month);
                        int iDay = dToday.DayOfYear;
                        iDayCount = calendarModel.GetDaysOfYear(dToday.Year);
                        float nHour = (float)lth.GetTime(cfg.CurrentTimeType).TimeOfDay.TotalHours;

                        if (ResetData && cfg.Style == ActionButton_Style.iChronEye && cfg.AnimateOnFirstClick)
                        {
                            ResetData = false;

                            DateTime tRunStart = DateTime.Now;
                            TimeSpan tsRun = TimeSpan.FromSeconds(Math.Max(2, cfg.AnimationDuriation));
                            DateTime tRunEnd = tRunStart.Add(tsRun);

                            while (DateTime.Now < tRunEnd)
                            {
                                double nMsDone = (DateTime.Now - tRunStart).TotalMilliseconds;
                                float nFact = (float)(nMsDone / tsRun.TotalMilliseconds);

                                DrawButton(this, cfg, wSize, appWidgetManager, iWidgetId, nHour + ((24.0F * cfg.AnimationRounds) - (24.0F * cfg.AnimationRounds) * nFact), (int)(iDay - ((iDayCount * cfg.AnimationRounds) - (iDayCount * cfg.AnimationRounds) * nFact)), iDayCount, false);

                                if (!Thread.CurrentThread.IsAlive || Thread.CurrentThread.ThreadState == ThreadState.AbortRequested || Thread.CurrentThread.ThreadState == ThreadState.Aborted)
                                    return;

                                Thread.Sleep(5);
                            }

                            iDay = dToday.DayOfYear;
                            nHour = (float)lth.GetTime(TimeType.RealSunTime).Hour;
                        }
                        DrawButton(this, cfg, wSize, appWidgetManager, iWidgetId, nHour, iDay, iDayCount, cfg.Style == ActionButton_Style.iChronEye && cfg.AnimateOnFirstClick);

                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        sys.DebugLogException(ex);
                        xLog.Error(ex, "Update Widget Error: " + iWidgetId);
                        RemoteViews rv = new RemoteViews(PackageName, Resource.Layout.widget_unconfigured);
                        rv.SetTextViewText(Resource.Id.message, "error loading widget:\n" + ex.Message);
                        rv.SetTextColor(Resource.Id.message, Color.IndianRed);
                        appWidgetManager.UpdateAppWidget(iWidgetId, rv);
                    }
                    lock (RunningTaskS)
                    {
                        if (RunningTaskS.ContainsKey(iWidgetId) && RunningTaskS[iWidgetId] == Thread.CurrentThread)
                            RunningTaskS.Remove(iWidgetId);
                    }
                    xLog.Debug("UpdateDone: " + iWidgetId);
                });
                tr.IsBackground = true;
                RunningTaskS.Add(iWidgetId, tr);

                tr.Start();
            }
        }

        public static RemoteViews DrawButton(Context context, WidgetCfg_ActionButton cfg, Point wSize, AppWidgetManager appWidgetManager, int iWidgetId, float hour, int iDay, int iDayCount, bool bAnimateOnClick)
        {
            //the Color-Circle
            int iWidgetShortSide = Math.Min(wSize.X, wSize.Y);
            var max = MainWidgetBase.GetMaxXY(wSize.X * sys.DisplayDensity, wSize.Y * sys.DisplayDensity);
            int iImgCX = max.x / 2;
            int iImgCY = max.y / 2;

            int iImgShortSize = Math.Min(max.x, max.y);

            RemoteViews rv = new RemoteViews(context.PackageName, Resource.Layout.widget_action_button);

            int iIconSize = iWidgetShortSide;
            if (string.IsNullOrEmpty(cfg.WidgetTitle))
            {
                rv.SetTextViewText(Resource.Id.widget_title, "");
                rv.SetViewPadding(Resource.Id.circle_image, 0, 0, 0, 0);
            }
            else
            {
                if ("iChronoMe".Equals(cfg.WidgetTitle))
                {
                    var span = new SpannableString("iChronoMe");
                    span.SetSpan(new AbsoluteSizeSpan(19, true), 0, 1, SpanTypes.ExclusiveExclusive);
                    rv.SetTextViewText(Resource.Id.widget_title, span);
                }
                else
                    rv.SetTextViewText(Resource.Id.widget_title, cfg.WidgetTitle);
                rv.SetViewPadding(Resource.Id.circle_image, 0, 0, 0, 20 * sys.DisplayDensity);
                iIconSize -= 20;
                rv.SetTextColor(Resource.Id.widget_title, cfg.ColorTitleText.ToAndroid());
            }

            if (bAnimateOnClick || cfg.ClickAction.Type == ClickActionType.Animate)
            {
                Intent itClick = new Intent(context, typeof(ActionButtonWidget));
                itClick.SetAction(MainWidgetBase.ActionManualRefresh);
                itClick.PutExtra(AppWidgetManager.ExtraAppwidgetId, iWidgetId);
                rv.SetOnClickPendingIntent(Resource.Id.widget, PendingIntent.GetBroadcast(context, iWidgetId, itClick, PendingIntentFlags.UpdateCurrent));
            }
            else
            {
                rv.SetOnClickPendingIntent(Resource.Id.widget, MainWidgetBase.GetClickActionIntent(context, cfg.ClickAction, iWidgetId, null));
            }

            if (cfg.Style == ActionButton_Style.iChronEye)
            {
                Bitmap bmp = GetIChronoEye(iImgShortSize, max.x, max.y, iImgCX, iImgCY, hour, iDay, iDayCount);

                rv.SetImageViewBitmap(Resource.Id.circle_image, bmp);
            }
            else
            {
                if (cfg.ColorBackground.ToAndroid() != Color.Transparent)
                {
                    int iXR = 8;
                    GradientDrawable back = new GradientDrawable();
                    back.SetShape(ShapeType.Rectangle);
                    back.SetCornerRadii(new float[] { iXR, iXR, iXR, iXR, iXR, iXR, iXR, iXR });
                    back.SetColor(cfg.ColorBackground.ToAndroid());
                    //back.SetStroke(1, Color.Black);
                    rv.SetImageViewBitmap(Resource.Id.background_image, MainWidgetBase.GetDrawableBmp(back, wSize.X, wSize.Y));
                    rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Visible);
                }
                else
                    rv.SetViewVisibility(Resource.Id.background_image, ViewStates.Gone);

                int iIconRes = Resource.Mipmap.ic_launcher;
                try
                {
                    rv.SetImageViewBitmap(Resource.Id.circle_image, DrawableHelper.GetIconBitmap(context, cfg.IconName, iIconSize, cfg.IconColor));
                }
                catch (Exception ex)
                {
                    xLog.Error(ex, "SetImageViewBitmap: " + cfg.IconName);
                    rv.SetImageViewResource(Resource.Id.circle_image, iIconRes);
                    sys.DebugLogException(ex);
                }
            }
            appWidgetManager?.UpdateAppWidget(iWidgetId, rv);

            return rv;
        }

        public static Bitmap GetIChronoEye(int iImgShortSize, int iImgX, int iImgY, int iImgCX, int iImgCY, float hour, int iDay, int iDayCount)
        {
            float nOuterCircle = iImgShortSize * .45F;// (iImgShortSize * .75F + Math.Min(iImgShortSize * .2F, iImgShortSize * .2F * iDayCount / 366)) / 2;
            float nColorWheelThickness = nOuterCircle * 2 / 3;
            float nColorWheelRadius = nOuterCircle * 2 / 3;

            float nCircleRadius = Math.Min(iImgCX, iImgCY);


            Bitmap bmp = Bitmap.CreateBitmap(iImgX, iImgY, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bmp);

            int iBaseRotate = -180;

            float nAngleStart = -(360.0F / iDayCount * .5F) + iBaseRotate;
            float nAnglePart = 360.0F / iDayCount;
            RectF box = new RectF(iImgCX - nOuterCircle, iImgCY - nOuterCircle, iImgCX + nOuterCircle, iImgCY + nOuterCircle);

            //der Farbkuchen
            Color clr = DynamicColors.RandomColor().ToAndroid();

            var paint = new Paint();
            paint.SetStyle(Paint.Style.Fill);
            paint.AntiAlias = true;
            for (int i = 0; i < iDayCount; i++)
            {
                clr = DynamicColors.RandomColor().ToAndroid();

                paint.Color = clr;
                canvas.DrawArc(box, nAngleStart, nAnglePart, true, paint);
                nAngleStart += nAnglePart;
            }

            //the black Center
            var pEarse = new Paint();
            //pEarse.Color = Color.Transparent;
            //pEarse.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
            pEarse.SetStyle(Paint.Style.Fill);
            for (float nRad = .43F; nRad >= .27F; nRad -= .03F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle * nRad, Color.Black, new Color(0, 0, 0, 10), Shader.TileMode.Mirror));
                canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }

            paint.Color = Color.Black;
            //canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle, paint);

            //Rand auswischen
            pEarse.SetStyle(Paint.Style.Stroke);
            pEarse.StrokeWidth = nOuterCircle * .05F;
            for (float nRad = 1.0F; nRad >= .92F; nRad -= .02F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle, new Color(0, 0, 0, 10), new Color(0, 0, 0, 30), Shader.TileMode.Mirror));
                canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }

            //ein Schimmer
            pEarse.SetStyle(Paint.Style.Stroke);
            pEarse.StrokeWidth = nOuterCircle * .2F;

            for (float nRad = .4F; nRad <= .7F; nRad += 0.03F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle * nRad, new Color(255, 255, 255, 0), new Color(255, 255, 255, 10), Shader.TileMode.Mirror));
                canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }
            for (float nRad = .6F; nRad <= .95F; nRad += 0.025F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle * 1.4F * nRad, new Color(255, 255, 255, 20), new Color(255, 255, 255, 7), Shader.TileMode.Mirror));
                canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }

            for (float nRad = .75F; nRad <= 1.15F; nRad += 0.035F)
            {
                pEarse.SetShader(new RadialGradient(iImgCX, iImgCY, nOuterCircle * 1.4F * nRad, new Color(255, 255, 255, 30), new Color(255, 255, 255, 10), Shader.TileMode.Mirror));
                //canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle * nRad, pEarse);
            }

            //paint.Color = Color.Black;
            //canvas.DrawCircle(iImgCX, iImgCY, nOuterCircle / 3, paint);


            //die Stunde
            float hourangle = 360.0F / 24 * hour + 90;
            float hourX = (float)(iImgCX + nOuterCircle / 6 * Math.Cos((hourangle) * Math.PI / 180));
            float hourY = (float)(iImgCY + nOuterCircle / 6 * Math.Sin((hourangle) * Math.PI / 180));

            var pCenter = new Paint();
            pCenter.SetStyle(Paint.Style.Fill);
            pCenter.SetShader(new RadialGradient(hourX, hourY, nOuterCircle / 4.5F, new Color(255, 255, 255, 180), Color.Transparent, Shader.TileMode.Mirror));
            canvas.DrawCircle(hourX, hourY, nOuterCircle / 4.5F, pCenter);

            //Heute hervorheben
            if (iDay >= 0)
            {
                float len = iImgShortSize * .68F;
                box.Set(iImgCX - len, iImgCY - len, iImgCX + len, iImgCY + len);

                int iNrToday = iDay;
                nAngleStart = -(360.0F / iDayCount * .5F) + iBaseRotate;
                nAngleStart += nAnglePart * iNrToday;
                if (nAnglePart < 5)
                {
                    nAngleStart -= (5 - nAnglePart) / 2;
                    nAnglePart = 5;
                }

                clr = DynamicColors.RandomColor().ToAndroid();

                paint.Color = clr;
                canvas.DrawArc(box, nAngleStart, nAnglePart, true, paint);
            }

            return bmp;
        }
    }
}