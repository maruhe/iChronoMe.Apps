using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;

namespace iChronoMe.Droid.Widgets
{
    public abstract class BaseWidgetActivity : BaseActivity
    {
        protected Drawable wallpaperDrawable;
        public int appWidgetId = -1;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Intent launchIntent = Intent;
            Bundle extras = launchIntent.Extras;

            if (extras != null)
            {
                appWidgetId = extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                Intent resultValue = new Intent();
                resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                SetResult(Result.Canceled, resultValue);
            }
            if (appWidgetId < 0)
            {
                Toast.MakeText(this, Resource.String.error_message_paramers, ToastLength.Long).Show();
                FinishAndRemoveTask();
                return;
            }
            LoadAppTheme();
            SetTheme(Resource.Style.TransparentTheme);
        }

        protected void TryGetWallpaper()
        {
            try
            {
                WallpaperManager wpMgr = WallpaperManager.GetInstance(this);
                wallpaperDrawable = wpMgr.FastDrawable;
                wpMgr.Dispose();
            }
            catch (System.Exception ex)
            {
                try
                {
                    WallpaperManager wpMgr = WallpaperManager.GetInstance(this);
                    wallpaperDrawable = wpMgr.BuiltInDrawable;
                    wpMgr.Dispose();
                }
                catch (System.Exception ex2)
                {
                    ex2.ToString();
                }

                ex.ToString();
            }

            if (wallpaperDrawable == null)
                wallpaperDrawable = Resources.GetDrawable(Resource.Mipmap.dummy_wallpaper, Theme);
        }
    }
}