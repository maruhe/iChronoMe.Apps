using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Droid.Wallpaper.Controls;
using iChronoMe.Widgets;
using iChronoMe.Widgets.AndroidHelpers;
using Xamarin.Essentials;

namespace iChronoMe.Droid.Wallpaper
{
    public static class WallpaperConfigHolder
    {
        static WallpaperConfig cfgHomePort = null;
        static WallpaperConfig cfgHomeLand = null;
        static WallpaperConfig cfgLockPort = null;
        static WallpaperConfig cfgLockLand = null;

        public static WallpaperConfig GetConfig(WallpaperType type, DisplayOrientation? orientation = null, bool forceUnicObject = false)
        {
            if (orientation == null)
                orientation = DeviceDisplay.MainDisplayInfo.Orientation;

            WallpaperConfig cfg = null;

            if (type == WallpaperType.HomeScreen)
                cfg = orientation == DisplayOrientation.Landscape ? cfgHomeLand : cfgHomePort;
            else
            {
                cfg = orientation == DisplayOrientation.Landscape ? cfgLockLand : cfgLockPort; 
                if (cfg == null)
                    cfg = orientation == DisplayOrientation.Landscape ? cfgHomeLand : cfgHomePort; 
            }

            if (cfg == null)
            {
                cfg = new WallpaperConfig();
                cfg.Items.Add(new WallpaperItem
                {
                    X = 50,
                    Y = 50,
                    Width = sys.DisplayShortSite / 2,
                    Heigth = sys.DisplayShortSite / 2,
                    ClockCfg = new WidgetCfg_ClockAnalog()
                });

                cfg.Items.Add(new WallpaperItem
                {
                    X = 150 + sys.DisplayShortSite / 2,
                    Y = 50,
                    Width = sys.DisplayShortSite / 2,
                    Heigth = sys.DisplayShortSite / 2,
                    ClockCfg = new WidgetCfg_ClockAnalog()
                });
                SetConfig(type, orientation.Value, cfg);
            }

            if (forceUnicObject)
                return cfg.Clone();

            return cfg;
        }

        public static void SetConfig(WallpaperType type, DisplayOrientation orientation, WallpaperConfig cfg)
        {
            if (type == WallpaperType.HomeScreen)
            {
                if (orientation == DisplayOrientation.Landscape)
                    cfgHomeLand = cfg;
                else
                    cfgHomePort = cfg;
            }
            else
            {
                if (orientation == DisplayOrientation.Landscape)
                    cfgLockLand = cfg;
                else
                    cfgLockPort = cfg;
            }
        }
    }

    public class WallpaperConfig
    {
        public string BackgroundImage { get; set; }
        public List<WallpaperItem> Items { get; set; } = new List<WallpaperItem>();

        public WallpaperConfig Clone()
        {
            return (WallpaperConfig)MemberwiseClone();
        }
    }

    public class WallpaperItem
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Heigth { get; set; }

        
        [XmlIgnore] public WidgetCfg_ClockAnalog ClockCfg { get; set; }
        
        [XmlIgnore] public WidgetView_ClockAnalog ClockView { get; set; }
        [XmlIgnore] public SKCanvasMapper CanvasMapper { get; set; }

        [XmlIgnore] public ConfigLayout ConfigLayout { get; set; }

        [XmlIgnore] public Android.Graphics.Bitmap BackgroundCache { get; set; }
    }

    public enum WallpaperType
    {
        HomeScreen,
        LockScreen
    }
}