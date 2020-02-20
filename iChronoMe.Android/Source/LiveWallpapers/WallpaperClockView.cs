using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;

namespace iChronoMe.Droid.LiveWallpapers
{
    public class WallpaperClockView
    {
        private string cfgInstance = Guid.NewGuid().ToString();
        public string BackgroundImage { get; set; } = string.Empty;

        public xColor ColorBackground { get; set; } = xColor.Transparent;
        public xColor ColorTickMarks { get; set; } = xColor.Black;
        public xColor ColorHourHandStorke { get; set; } = xColor.Black;
        public xColor ColorHourHandFill { get; set; } = xColor.Blue;
        public xColor ColorMinuteHandStorke { get; set; } = xColor.Black;
        public xColor ColorMinuteHandFill { get; set; } = xColor.Blue;
        public xColor ColorSecondHandStorke { get; set; } = xColor.Black;
        public xColor ColorSecondHandFill { get; set; } = xColor.Blue;

        public bool ShowHourHand { get; set; } = true;
        public bool ShowMinuteHand { get; set; } = true;
        public bool ShowSecondHand { get; set; } = true;

        public bool FlowHourHand { set; get; } = true;
        public bool FlowMinuteHand { set; get; } = true;
        public bool FlowSecondHand { set; get; } = false;

        TimeSpan tsMin = TimeSpan.FromHours(1);
        TimeSpan tsMax = TimeSpan.FromTicks(0);
        TimeSpan tsAllSum = TimeSpan.FromTicks(0);
        int iAllCount = 0;

        public void ReadConfig(WidgetCfg_ClockAnalog cfg)
        {
            cfgInstance = Guid.NewGuid().ToString();

            BackgroundImage = cfg.BackgroundImage;

            ColorBackground = cfg.ColorBackground;
            ColorTickMarks = cfg.ColorTickMarks;
            ColorHourHandStorke = cfg.ColorHourHandStorke;
            ColorHourHandFill = cfg.ColorHourHandFill;
            ColorMinuteHandStorke = cfg.ColorMinuteHandStorke;
            ColorMinuteHandFill = cfg.ColorMinuteHandFill;
            ColorSecondHandStorke = cfg.ColorSecondHandStorke;
            ColorSecondHandFill = cfg.ColorSecondHandFill;

            ShowHourHand = cfg.ShowHours;
            ShowMinuteHand = cfg.ShowMinutes;
            ShowSecondHand = cfg.ShowSeconds;

            FlowHourHand = cfg.FlowHourHand;
            FlowMinuteHand = cfg.FlowMinuteHand;
            FlowSecondHand = cfg.FlowSecondHand;
        }

        Path tickMarks;
        Path hourHand;
        Path minuteHand;
        Path secondHand;

        Paint paint = new Paint();
        DashPathEffect minuteTickDashEffect =
            new DashPathEffect(new float[] { 0.1f, 3 * (float)Math.PI - 0.1f }, 0);

        DashPathEffect hourTickDashEffect =
            new DashPathEffect(new float[] { 0.1f, 15 * (float)Math.PI - 0.1f }, 0);

        float hourAngle, minuteAngle, secondAngle;

        public WallpaperClockView()
        {
            Initialize();
        }

        void Initialize()
        {
            // Create Paint for all drawing
            paint = new Paint();

            // All paths are based on 100-unit clock radius
            //		centered at (0, 0).

            // Define circle for tick marks.
            tickMarks = new Path();
            tickMarks.AddCircle(0, 0, 90, Path.Direction.Cw);

            // Hour, minute, second hands defined to point straight up.

            // Define hour hand.
            hourHand = new Path();
            hourHand.MoveTo(0, -60);
            hourHand.CubicTo(0, -30, 20, -30, 5, -20);
            hourHand.LineTo(5, 0);
            hourHand.CubicTo(5, 7.5f, -5, 7.5f, -5, 0);
            hourHand.LineTo(-5, -20);
            hourHand.CubicTo(-20, -30, 0, -30, 0, -60);
            hourHand.Close();

            // Define minute hand.
            minuteHand = new Path();
            minuteHand.MoveTo(0, -80);
            minuteHand.CubicTo(0, -75, 0, -70, 2.5f, -60);
            minuteHand.LineTo(2.5f, 0);
            minuteHand.CubicTo(2.5f, 5, -2.5f, 5, -2.5f, 0);
            minuteHand.LineTo(-2.5f, -60);
            minuteHand.CubicTo(0, -70, 0, -75, 0, -80);
            minuteHand.Close();

            // Define second hand.
            secondHand = new Path();
            secondHand.MoveTo(0, 10);
            secondHand.LineTo(0, -80);
        }

        public void DrawClock(Canvas canvas, DateTime tNow, int width, int height)
        {
            var swStart = DateTime.Now;

            this.hourAngle = 30 * tNow.Hour + (this.FlowHourHand ? 0.5f * tNow.Minute : 0);
            this.minuteAngle = 6 * tNow.Minute + (this.FlowMinuteHand ? 0.1f * tNow.Second : 0);
            this.secondAngle = 6 * tNow.Second + (this.FlowSecondHand ? 0.006f * tNow.Millisecond : 0);

            // Clear screen to pink.
            //paint.Color = Color.WhiteSmoke;// new Color (255, 204, 204);
            //canvas.DrawPaint(paint);

            //paint.Color = ColorBackground;
            //paint.SetStyle(Paint.Style.Fill);
            //canvas.DrawCircle(this.Width / 2, this.Height / 2, this.Height / 2, paint);

            // Overall transforms to shift (0, 0) to center and scale.
            canvas.Translate(width / 2, height / 2);
            float scale = Math.Min(width, height) / 2.0f / 100;
            canvas.Scale(scale, scale);

            // Attributes for tick marks.
            paint.Color = ColorTickMarks.ToAndroid();
            paint.StrokeCap = Paint.Cap.Round;
            paint.SetStyle(Paint.Style.Stroke);

            // Set line dash to draw tick marks for every minute.
            paint.StrokeWidth = 3;
            paint.SetPathEffect(minuteTickDashEffect);
            canvas.DrawPath(tickMarks, paint);

            // Set line dash to draw tick marks for every hour.
            paint.StrokeWidth = 6;
            paint.SetPathEffect(hourTickDashEffect);
            canvas.DrawPath(tickMarks, paint);

            // Set attributes common to all clock hands.
            paint.StrokeWidth = 2;
            paint.SetPathEffect(null);

            // Draw hour hand.
            if (ShowHourHand)
            {
                canvas.Save();
                canvas.Rotate(this.hourAngle);
                paint.Color = ColorHourHandFill.ToAndroid();
                paint.SetStyle(Paint.Style.Fill);
                canvas.DrawPath(hourHand, paint);
                paint.Color = ColorHourHandStorke.ToAndroid();
                paint.SetStyle(Paint.Style.Stroke);
                canvas.DrawPath(hourHand, paint);
                canvas.Restore();
            }

            if (ShowMinuteHand)
            {
                // Draw minute hand.
                canvas.Save();
                canvas.Rotate(this.minuteAngle);
                paint.Color = ColorMinuteHandFill.ToAndroid();
                paint.SetStyle(Paint.Style.Fill);
                canvas.DrawPath(minuteHand, paint);
                paint.Color = ColorMinuteHandStorke.ToAndroid();
                paint.SetStyle(Paint.Style.Stroke);
                canvas.DrawPath(minuteHand, paint);
                canvas.Restore();
            }

            paint.StrokeWidth = 3;
            if (ShowSecondHand)
            {
                // Draw second hand.
                canvas.Save();
                canvas.Rotate(this.secondAngle);
                paint.Color = ColorSecondHandStorke.ToAndroid();
                paint.SetStyle(Paint.Style.Stroke);
                canvas.DrawPath(secondHand, paint);
                canvas.Restore();
            }


            TimeSpan tsDraw = DateTime.Now - swStart;
            iAllCount++;
            tsAllSum += tsDraw;
            if (tsMin > tsDraw)
                tsMin = tsDraw;
            if (tsMax < tsDraw)
                tsMax = tsDraw;
        }

        public string PerformanceInfo
        {
            get
            {
                if (iAllCount > 0)
                {
                    return "min: " + tsMin.TotalMilliseconds.ToString() + "\n" +
                           "max: " + tsMax.TotalMilliseconds.ToString() + "\n" +
                           "avg: " + TimeSpan.FromTicks(tsAllSum.Ticks / iAllCount).ToString();
                }
                return "init..";
            }
        }
    }
}
