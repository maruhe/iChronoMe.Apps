//java sample by https://stackoverflow.com/questions/8974088/how-to-create-a-resizable-rectangle-with-user-touch-events-on-android

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Wallpaper.Controls
{
    [Register("me.ichrono.droid.Wallpaper.Controls.UserLayout")]
    public class UserLayout : View
    {
        //contants strings
        private const string TAG = "UserLayout";

        //drawing objects
        private Paint paint;

        //point objects
        private Point[] points;
        private Point start;
        private Point offset;

        //variable ints
        private int minimumSideLength;
        private int side;
        private int halfCorner;
        private Color cornerColor;
        private Color edgeColor;
        private Color outsideColor;
        private int corner = -1;

        //variable booleans
        private bool initialized = false;

        //drawables
        private Drawable moveDrawable;
        private Drawable resizeDrawable1, resizeDrawable2, resizeDrawable3;

        //context
        Context context;

        public UserLayout(Context context) : base(context)
        {
            this.context = context;
            init(null);
        }

        public UserLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            this.context = context;
            init(attrs);
        }

        public UserLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            this.context = context;
            init(attrs);
        }

        public UserLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            this.context = context;
            init(attrs);
        }

        private void init(IAttributeSet attrs)
        {

            paint = new Paint();
            start = new Point();
            offset = new Point();

            TypedArray ta = context.Theme.ObtainStyledAttributes(attrs, Resource.Styleable.UserLayout, 0, 0);

            //initial dimensions
            minimumSideLength = ta.GetDimensionPixelSize(Resource.Styleable.UserLayout_minimumSide, 20);
            side = minimumSideLength;
            halfCorner = (ta.GetDimensionPixelSize(Resource.Styleable.UserLayout_cornerSize, 20)) / 2;

            //colors
            cornerColor = ta.GetColor(Resource.Styleable.UserLayout_cornerColor, Color.Black);
            edgeColor = ta.GetColor(Resource.Styleable.UserLayout_edgeColor, Color.White);
            outsideColor = ta.GetColor(Resource.Styleable.UserLayout_outsideCropColor, Color.ParseColor("#00000088"));

            //initialize corners;
            points = new Point[4];

            points[0] = new Point();
            points[1] = new Point();
            points[2] = new Point();
            points[3] = new Point();

            //init corner locations;
            //top left
            points[0].X = 0;
            points[0].Y = 0;

            //top right
            points[1].X = minimumSideLength;
            points[1].Y = 0;

            //bottom left
            points[2].X = 0;
            points[2].Y = minimumSideLength;

            //bottom right
            points[3].X = minimumSideLength;
            points[3].Y = minimumSideLength;

            //init drawables
            moveDrawable = ta.GetDrawable(Resource.Styleable.UserLayout_moveCornerDrawable);
            resizeDrawable1 = ta.GetDrawable(Resource.Styleable.UserLayout_resizeCornerDrawable);
            resizeDrawable2 = ta.GetDrawable(Resource.Styleable.UserLayout_resizeCornerDrawable);
            resizeDrawable3 = ta.GetDrawable(Resource.Styleable.UserLayout_resizeCornerDrawable);

            //set drawable colors
            moveDrawable.SetTint(cornerColor);
            resizeDrawable1.SetTint(cornerColor);
            resizeDrawable2.SetTint(cornerColor);
            resizeDrawable3.SetTint(cornerColor);

            //recycle attributes
            ta.Recycle();

            //set initialized to true
            initialized = true;

        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            //set paint to draw edge, stroke
            if (initialized)
            {
                paint.AntiAlias = true;
                paint.SetStyle(Paint.Style.Stroke);
                paint.StrokeJoin = Paint.Join.Round;
                paint.Color = edgeColor;
                paint.StrokeWidth = 4;

                //crop rectangle
                canvas.DrawRect(points[0].X, points[0].Y, points[3].X, points[3].Y, paint);

                //set paint to draw outside color, fill
                paint.SetStyle(Paint.Style.Fill);
                paint.Color = outsideColor;

                //top rectangle
                canvas.DrawRect(0, 0, canvas.Width, points[0].Y, paint);
                //left rectangle
                canvas.DrawRect(0, points[0].Y, points[0].X, canvas.Height, paint);
                //right rectangle
                canvas.DrawRect(points[1].X, points[1].Y, canvas.Width, points[3].Y, paint);
                //bottom rectangle
                canvas.DrawRect(points[0].X, points[3].Y, canvas.Width, canvas.Height, paint);

                //set bounds of drawables
                   moveDrawable.SetBounds(points[0].X - halfCorner, points[0].Y - halfCorner, points[0].X + halfCorner, points[0].Y + halfCorner);
                resizeDrawable1.SetBounds(points[1].X - halfCorner, points[1].Y - halfCorner, points[1].X + halfCorner, points[1].Y + halfCorner);
                resizeDrawable2.SetBounds(points[2].X - halfCorner, points[2].Y - halfCorner, points[2].X + halfCorner, points[2].Y + halfCorner);
                resizeDrawable3.SetBounds(points[3].X - halfCorner, points[3].Y - halfCorner, points[3].X + halfCorner, points[3].Y + halfCorner);

                //place corner drawables
                moveDrawable.Draw(canvas);
                resizeDrawable1.Draw(canvas);
                resizeDrawable2.Draw(canvas);
                resizeDrawable3.Draw(canvas);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            //return super.onTouchEvent(event);
            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:

                    //get the coordinates
                    start.X = (int)e.GetX();
                    start.Y = (int)e.GetY();

                    //get the corner touched if any
                    corner = getCorner(start.X, start.Y);

                    //get the offset of touch(X,Y) from corner top-left point
                    offset = getOffset(start.X, start.Y, corner);

                    //account for touch offset in starting point
                    start.X = start.X - offset.X;
                    start.Y = start.Y - offset.Y;

                    return corner >= 0;

                case MotionEventActions.Move:
                    if (corner >= 0)
                    {
                        if (corner == 0)
                        {
                            points[0].X = Math.Max(points[0].X + (int)Math.Min(Math.Floor((e.GetX() - start.X - offset.X)), Math.Floor((double)(Width - points[0].X - side))), 0);
                            points[1].X = Math.Max(points[1].X + (int)Math.Min(Math.Floor((e.GetX() - start.X - offset.X)), Math.Floor((double)(Width - points[1].X))), side);
                            points[2].X = Math.Max(points[2].X + (int)Math.Min(Math.Floor((e.GetX() - start.X - offset.X)), Math.Floor((double)(Width - points[2].X - side))), 0);
                            points[3].X = Math.Max(points[3].X + (int)Math.Min(Math.Floor((e.GetX() - start.X - offset.X)), Math.Floor((double)(Width - points[3].X))), side);

                            points[0].Y = Math.Max(points[0].Y + (int)Math.Min(Math.Floor((e.GetY() - start.Y - offset.Y)), Math.Floor((double)(Height - points[0].Y - side))), 0);
                            points[1].Y = Math.Max(points[1].Y + (int)Math.Min(Math.Floor((e.GetY() - start.Y - offset.Y)), Math.Floor((double)(Height - points[1].Y - side))), 0);
                            points[2].Y = Math.Max(points[2].Y + (int)Math.Min(Math.Floor((e.GetY() - start.Y - offset.Y)), Math.Floor((double)(Height - points[2].Y))), side);
                            points[3].Y = Math.Max(points[3].Y + (int)Math.Min(Math.Floor((e.GetY() - start.Y - offset.Y)), Math.Floor((double)(Height - points[3].Y))), side);

                            start.X = points[0].X;
                            start.Y = points[0].Y;
                            Invalidate();
                            UserChanged?.Invoke(this, null);
                        }
                        else if (corner == 1)
                        {
                            side = Math.Min((Math.Min((Math.Max(minimumSideLength, (int)(side + Math.Floor(e.GetX()) - start.X - offset.X))), side + (Width - points[1].X))), side + (Height - points[2].Y));
                            points[1].X = points[0].X + side;
                            points[3].X = points[0].X + side;
                            points[3].Y = points[0].Y + side;
                            points[2].Y = points[0].Y + side;
                            start.X = points[1].X;
                            Invalidate();
                            UserChanged?.Invoke(this, null);
                        }
                        else if (corner == 2)
                        {
                            side = Math.Min((Math.Min((Math.Max(minimumSideLength, (int)(side + Math.Floor(e.GetY()) - start.Y - offset.Y))), side + (Height - points[2].Y))), side + (Width - points[1].X));
                            points[2].Y = points[0].Y + side;
                            points[3].Y = points[0].Y + side;
                            points[3].X = points[0].X + side;
                            points[1].X = points[0].X + side;
                            start.Y = points[2].Y;
                            Invalidate();
                            UserChanged?.Invoke(this, null);
                        }
                        else if (corner == 3)
                        {
                            side = Math.Min((Math.Min((Math.Min((Math.Max(minimumSideLength, (int)(side + Math.Floor(e.GetX()) - start.X - offset.X))), side + (Width - points[3].X))), side + (Height - points[3].Y))), Math.Min((Math.Min((Math.Max(minimumSideLength, (int)(side + Math.Floor(e.GetY()) - start.Y - offset.Y))), side + (Height - points[3].Y))), side + (Width - points[3].X)));
                            points[1].X = points[0].X + side;
                            points[3].X = points[0].X + side;
                            points[3].Y = points[0].Y + side;
                            points[2].Y = points[0].Y + side;
                            start.X = points[3].X;

                            points[2].Y = points[0].Y + side;
                            points[3].Y = points[0].Y + side;
                            points[3].X = points[0].X + side;
                            points[1].X = points[0].X + side;
                            start.Y = points[3].Y;
                            Invalidate();
                            UserChanged?.Invoke(this, null);
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }

        private int getCorner(float X, float Y)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float dx = X - points[i].X + halfCorner;
                float dy = Y - points[i].Y + halfCorner;
                int max = halfCorner * 2;
                if (dx <= max && dx >= 0 && dy <= max && dy >= 0)
                {
                    //touch inside corner
                    return i;
                }
            }
            if (X > points[0].X && X < points[1].X &&
                Y > points[0].Y && Y < points[3].Y)
                //touch somewhere inside the area => move
                return 0;
            return -1;
        }

        private Point getOffset(int left, int top, int corner)
        {
            Point offset = new Point();
            if (corner < 0)
            {
                offset.X = 0;
                offset.Y = 0;
            }
            else
            {
                offset.X = left - points[corner].X;
                offset.Y = top - points[corner].Y;
            }
            return offset;
        }

        public void SetSize(int x, int y, int width, int height)
        {
            points[0].X = x;
            points[0].Y = y;

            //top right
            points[1].X = x + Math.Max(minimumSideLength, width);
            points[1].Y = y;

            //bottom left
            points[2].X = x;
            points[2].Y = y + +Math.Max(minimumSideLength, height);

            //bottom right
            points[3].X = points[1].X;
            points[3].Y = points[2].Y;

            Invalidate();
        }

        public (int x, int y, int width, int height) GetSize()
        {
            return (points[0].X, points[0].Y, points[3].X - points[0].X, points[3].Y - points[0].Y);
        }

        public event EventHandler UserChanged;
    }
}