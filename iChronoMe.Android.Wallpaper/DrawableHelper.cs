using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V4.Graphics.Drawable;

using iChronoMe.Core.Classes;
using iChronoMe.Core.Types;

namespace iChronoMe.Droid.Wallpaper
{
    public static class DrawableHelper
    {
        private static List<int> _coloredIcons = null;
        public static List<int> ColoredIcons
        {
            get
            {
                if (_coloredIcons == null)
                {
                    List<int> list = new List<int>();
                    foreach (var prop in typeof(Resource.Drawable).GetFields())
                    {
                        if (prop.Name.Contains("_clrd") || "real_sun_time".Equals(prop.Name))
                            list.Add((int)prop.GetValue(null));
                    }
                    _coloredIcons = list;
                }
                return _coloredIcons;
            }
        }

        public static Drawable GetIconDrawable(Context context, string drawableName, xColor color)
        {
            return GetIconDrawable(context, (int)typeof(Resource.Drawable).GetField(drawableName).GetValue(null), color);
        }
        public static Drawable GetIconDrawable(Context context, string drawableName, Color color)
        {
            return GetIconDrawable(context, (int)typeof(Resource.Drawable).GetField(drawableName).GetValue(null), color);
        }

        public static Drawable GetIconDrawable(Context context, int drawableRes, xColor color)
        {
            return GetIconDrawable(context, drawableRes, color.ToAndroid());
        }

        public static Drawable GetIconDrawable(Context context, int drawableRes, Color color)
        {
            try
            {
                var mDrawable = ContextCompat.GetDrawable(context, drawableRes);
                if (!(mDrawable is VectorDrawable) || ColoredIcons.Contains(drawableRes))
                    return mDrawable;
                try
                {
                    var mWrappedDrawable = mDrawable.Mutate();
                    mWrappedDrawable = DrawableCompat.Wrap(mWrappedDrawable);
                    DrawableCompat.SetTint(mWrappedDrawable, color);
                    DrawableCompat.SetTintMode(mWrappedDrawable, PorterDuff.Mode.SrcIn);
                    return mWrappedDrawable;
                }
                catch (Exception ex)
                {
                    xLog.Error(ex);
                    return mDrawable;
                }
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
                return null;
            }
        }

        public static Bitmap GetIconBitmap(Context context, int drawableRes, double nSizeDP, xColor color)
        {
            return GetDrawableBmp(GetIconDrawable(context, drawableRes, color), nSizeDP, nSizeDP);
        }

        public static Bitmap GetIconBitmap(Context context, string drawableName, double nSizeDP, xColor color)
        {
            return GetDrawableBmp(GetIconDrawable(context, drawableName, color), nSizeDP, nSizeDP);
        }

        public static Bitmap GetDrawableBmp(Drawable drw, double iShapeWidthDp, double iShapeHeigthDp)
        {
            if (drw == null)
                return null;
            try
            {
                var max = GetMaxXY((int)(iShapeWidthDp * sys.DisplayDensity), (int)(iShapeHeigthDp * sys.DisplayDensity), sys.DisplayShortSite);
                int iShapeWidth = max.x;
                int iShapeHeigth = max.y;

                Bitmap bmp = Bitmap.CreateBitmap(iShapeWidth, iShapeHeigth, Bitmap.Config.Argb8888);
                Canvas canvas = new Canvas(bmp);
                drw.SetBounds(0, 0, iShapeWidth, iShapeHeigth);
                drw.Draw(canvas);
                return bmp;
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
                return null;
            }
        }

        public static (int x, int y, float n) GetMaxXY(double x, double y, int max = 1000)
            => GetMaxXY((int)x, (int)y, max);

        public static (int x, int y, float n) GetMaxXY(int x, int y, int max = 1000)
        {
            if (max < 1)
                return (x, y, 1);
            max = (int)(max * .9);
            float n = 1;
            if (x > y)
            {
                if (x > max)
                {
                    n = (float)max / x;
                    y = (int)((double)y * n);
                    x = max;
                }
            }
            else
            {
                if (y > max)
                {
                    n = (float)max / y;
                    x = (int)((double)x * n);
                    y = max;
                }
            }
            return (x, y, n);
        }

        private static Dictionary<string, bool> isColored = new Dictionary<string, bool>();
        public static bool IsIconColored(Context context, string cName)
        {
            if (string.IsNullOrEmpty(cName))
                return false;
            if (isColored.ContainsKey(cName))
                return isColored[cName];

            bool bRes = false;
            try
            {
                var mDrawable = ContextCompat.GetDrawable(context, (int)typeof(Resource.Drawable).GetField(cName).GetValue(null));
                if (!(mDrawable is VectorDrawable))
                    return false;

                var bmp = GetDrawableBmp(mDrawable, 10, 10);

                for (int iX = 0; iX < bmp.Width / 2; iX++)
                {
                    for (int iY = 0; iY < bmp.Height / 2; iY++)
                    {
                        var clr = new Color(bmp.GetPixel(iX, iY));
                        if (clr.A != 0 && (clr.R != 0 || clr.G != 0 || clr.B != 0))
                        {
                            bRes = true;
                            return bRes;
                        }
                    }
                }
            }
            finally
            {
                if (!isColored.ContainsKey(cName))
                    isColored.Add(cName, bRes);
            }
            return bRes;
        }
    }
}


/*
 * 
 * https://stackoverflow.com/questions/32924986/change-fill-color-on-vector-asset-in-android-studio/33987406
import android.content.Context;
import android.graphics.PorterDuff;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.support.annotation.ColorRes;
import android.support.annotation.DrawableRes;
import android.support.annotation.NonNull;
import android.support.v4.content.ContextCompat;
import android.support.v4.graphics.drawable.DrawableCompat;
import android.view.MenuItem;
import android.view.View;
import android.widget.ImageView;

/**
 * {@link Drawable} helper class.
 *
 * @author Filipe Bezerra
 * @version 18/01/2016
 * @since 18/01/2016
 * /
public class DrawableHelper
{
    @NonNull Context mContext;
    @ColorRes private int mColor;
    private Drawable mDrawable;
    private Drawable mWrappedDrawable;

    public DrawableHelper(@NonNull Context context)
    {
        mContext = context;
    }

    public static DrawableHelper withContext(@NonNull Context context)
    {
        return new DrawableHelper(context);
    }

    public DrawableHelper withDrawable(@DrawableRes int drawableRes)
    {
        mDrawable = ContextCompat.getDrawable(mContext, drawableRes);
        return this;
    }

    public DrawableHelper withDrawable(@NonNull Drawable drawable)
    {
        mDrawable = drawable;
        return this;
    }

    public DrawableHelper withColor(@ColorRes int colorRes)
    {
        mColor = ContextCompat.getColor(mContext, colorRes);
        return this;
    }

    public DrawableHelper tint()
    {
        if (mDrawable == null)
        {
            throw new NullPointerException("É preciso informar o recurso drawable pelo método withDrawable()");
        }

        if (mColor == 0)
        {
            throw new IllegalStateException("É necessário informar a cor a ser definida pelo método withColor()");
        }

        mWrappedDrawable = mDrawable.mutate();
        mWrappedDrawable = DrawableCompat.wrap(mWrappedDrawable);
        DrawableCompat.setTint(mWrappedDrawable, mColor);
        DrawableCompat.setTintMode(mWrappedDrawable, PorterDuff.Mode.SRC_IN);

        return this;
    }

    @SuppressWarnings("deprecation")
    public void applyToBackground(@NonNull View view)
    {
        if (mWrappedDrawable == null)
        {
            throw new NullPointerException("É preciso chamar o método tint()");
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN)
        {
            view.setBackground(mWrappedDrawable);
        }
        else
        {
            view.setBackgroundDrawable(mWrappedDrawable);
        }
    }

    public void applyTo(@NonNull ImageView imageView)
    {
        if (mWrappedDrawable == null)
        {
            throw new NullPointerException("É preciso chamar o método tint()");
        }

        imageView.setImageDrawable(mWrappedDrawable);
    }

    public void applyTo(@NonNull MenuItem menuItem)
    {
        if (mWrappedDrawable == null)
        {
            throw new NullPointerException("É preciso chamar o método tint()");
        }

        menuItem.setIcon(mWrappedDrawable);
    }

    public Drawable get()
    {
        if (mWrappedDrawable == null)
        {
            throw new NullPointerException("É preciso chamar o método tint()");
        }

        return mWrappedDrawable;
    }
}
*/
