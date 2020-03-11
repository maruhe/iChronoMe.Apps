using System;
using System.Globalization;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Views;
using Android.Widget;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.DeviceCalendar;
using iChronoMe.Widgets;

using Xamarin.Essentials;

namespace iChronoMe.Droid.Widgets.Calendar
{
    [Service(Label = "Calendar Update-Service", Permission = "android.permission.BIND_REMOTEVIEWS")]
    public class CalendarEventListService : RemoteViewsService
    {
        public static bool ResetData = false;

        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            SetTheme(Resource.Style.AppTheme_iChronoMe_Dark);
            return new CalendarEventListRemoteViewsFactory(this, intent);
        }
    }

    public class CalendarEventListRemoteViewsFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        private Context mContext;
        private EventCollection myEvents = null;
        WidgetCfg_CalendarTimetable cfg;
        DynamicCalendarModel calendarModel;

        int iMyWidgetId;
        Point wSize;

        public CalendarEventListRemoteViewsFactory(Context context, Intent intent)
        {
            mContext = context;
            mContext.SetTheme(Resource.Style.AppTheme_iChronoMe_Dark);
            iMyWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, -1);
            cfg = new WidgetConfigHolder().GetWidgetCfg<WidgetCfg_CalendarTimetable>(iMyWidgetId);
        }

        public CalendarEventListRemoteViewsFactory(Context context, WidgetCfg_CalendarTimetable wcfg, Android.Graphics.Point size, DynamicCalendarModel model, EventCollection events)
        {
            mContext = context;
            cfg = wcfg;
            wSize = size;
            calendarModel = model;
            myEvents = events;
        }

        // Initialize the data set.
        public void OnCreate()
        {
            // In onCreate() you set up any connections / cursors to your data source. Heavy lifting,
            // for example downloading or creating content etc, should be deferred to onDataSetChanged()
            // or getViewAt(). Taking more than 20 seconds in this call will result in an ANR.
            myEvents = new EventCollection();
        }

        // Given the position (index) of a WidgetItem in the array, use the item's text value in
        // combination with the app widget item XML file to construct a RemoteViews object.

        public RemoteViews GetViewAt(int position)
        {
            return GetViewAt(position, true);
        }

        public RemoteViews GetViewAt(int position, bool IsRealWidget)
        {

            try
            {
                // position will always range from 0 to getCount() - 1.
                // Construct a RemoteViews item based on the app widget item XML file, and set the
                // text based on the position.
                object data = myEvents.AllDatesAndEvents[position];

                if (data is string) //Fehler
                {
                    RemoteViews rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_timetable_daysplitter);
                    rv.SetTextViewText(Resource.Id.item_title, data as string);
                    rv.SetTextColor(Resource.Id.item_title, cfg.ColorErrorText.ToAndroid());

                    if (IsRealWidget)
                    {
                        Intent cfgIntent = new Intent();
                        cfgIntent.PutExtra("_ClickCommand", "CheckCalendarWidget");
                        cfgIntent.PutExtra("_ConfigComponent", "me.ichrono.droid/me.ichrono.droid.Widgets.Calendar.CalendarWidgetConfigActivity");
                        cfgIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, iMyWidgetId);
                        rv.SetOnClickFillInIntent(Resource.Id.item_layout, cfgIntent);
                    }
                    return rv;
                }
                else if (data is DateTime) //Tages-Separator
                {
                    //DateTime tToday = EventCollection.GetTimeFromLocal((DateTime)data);
                    DateTime tToday = (DateTime)data;
                    RemoteViews rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_timetable_daysplitter);
                    try
                    {
                        var date = calendarModel.GetDateFromUtcDate(tToday);
                        var cDay = date.WeekDayNameShort;
                        var cMonth = date.MonthNameFull;
                        int i;
                        if (int.TryParse(cMonth.Substring(cMonth.Length - 2), out i))
                            cMonth += ",";

                        string cTitle = cMonth + " " + date.DayNumber.ToString();
                        if (tToday.Date == DateTime.Now.Date)
                            cTitle = localize.text_Today;
                        if (tToday.Date == DateTime.Now.Date.AddDays(1))
                            cTitle = localize.text_Tomorrow;
                        if (wSize.X > 100)
                            cTitle += " (" + cDay + ".)";
                        rv.SetTextViewText(Resource.Id.item_title, cTitle);
                        rv.SetTextColor(Resource.Id.item_title, cfg.ColorSeparatorText.ToAndroid());
                    }
                    catch { }

                    rv.SetViewVisibility(Resource.Id.top_separator, ViewStates.Gone);

                    if (IsRealWidget)
                    {
                        Intent fillInIntent = new Intent();
                        fillInIntent.PutExtra("_ClickCommand", "ActionView");
                        fillInIntent.SetData(Android.Net.Uri.Parse("content://com.android.calendar/time/"));
                        rv.SetOnClickFillInIntent(Resource.Id.item_layout, fillInIntent);
                    }

                    return rv;
                }
                else if (data is CalendarEvent)
                {
                    RemoteViews rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_timetable_item);
                    CalendarEvent calEvent = (CalendarEvent)data;
                    CalendarEventExtention extEvent = calEvent.Extention;

                    DateTime tStart = calEvent.DisplayStart;
                    DateTime tEnd = calEvent.DisplayEnd;

                    // feed row
                    string cBis = " " + mContext.Resources.GetString(Resource.String.calendar_event_to) + " ";
                    if (wSize.X < 120)
                        cBis = "-";
                    string cAddonText = "";
                    string cTime = tStart.ToString("HH:mm") + cBis + tEnd.ToString("HH:mm");
                    if (calEvent.AllDay || tEnd - tStart > TimeSpan.FromHours(25))
                    {
                        var dynStart = calendarModel.GetDateFromUtcDate(tStart);
                        var dynEnd = calendarModel.GetDateFromUtcDate(tEnd);
                        cTime = tStart.ToString("HH:mm");
                        cAddonText = cBis.TrimStart() + dynStart.WeekDayNameShort + tEnd.ToString(". HH:mm");
                        if (calEvent.AllDay)
                        {
                            cTime = mContext.Resources.GetString(Resource.String.calendar_event_all_day);
                            cAddonText = dynStart.ToString("dd.MM.");
                        }
                        if (tEnd - tStart > TimeSpan.FromHours(25))
                        {
                            if (calEvent.AllDay)
                            {
                                cTime = mContext.Resources.GetString(Resource.String.calendar_event_multi_day);
                                cAddonText = dynStart.ToString("d.MM.") + cBis + dynEnd.ToString("d.MM.");
                            }
                        }
                    }
#if DEBUG
                    //if (extEvent.UseTypedTime)
                    //cTime += "\t" + extEvent.TimeType.ToString() + ": " + extEvent.TimeTypeStart.ToString("HH:mm:ss");
                    //cTime = position.ToString() + "/" + myEvents.AllDatesAndEvents.Count.ToString() + " " + cTime;
#endif
                    int iShapeWidth = 5;
                    int iShapeHeigth = 30;

                    rv.SetTextViewText(Resource.Id.item_title, calEvent.Title);// + "     \t" + DateTime.Now.ToString("HH:mm:ss.fff"));
                    rv.SetTextViewText(Resource.Id.item_time, cTime + " " + cAddonText);

                    rv.SetTextColor(Resource.Id.item_title, cfg.ColorEventNameText.ToAndroid());
                    rv.SetTextColor(Resource.Id.item_time, cfg.ColorEventTimeText.ToAndroid());

                    rv.SetViewVisibility(Resource.Id.item_posoffset, ViewStates.Gone);

                    if (cfg.ShowLocation && !string.IsNullOrEmpty(calEvent.Location))
                    {
                        Color clLocation = cfg.ColorEventLocationText.ToAndroid();
                        iShapeHeigth = 45;
                        if (IsRealWidget && cfg.ShowLocationSunOffset)
                        {
                            rv.SetImageViewBitmap(Resource.Id.item_posicon, DrawableHelper.GetIconBitmap(mContext, Resource.Drawable.icons8_sun_18, 16, cfg.ColorEventSymbols));

                            iShapeHeigth = 45;
                            string cPosInfo = (extEvent.LocationString.Equals(calEvent.Location) ? mContext.Resources.GetString(Resource.String.calendar_event_unclear_position_time) + ": " : mContext.Resources.GetString(Resource.String.calendar_event_position_is_determined) + ": ");
                            Color clPosInfo = cfg.ColorErrorText.ToAndroid();
                            if (extEvent.GotCorrectPosition)
                            {
                                clPosInfo = cfg.ColorEventLocationOffsetText.ToAndroid();
                                TimeSpan tsDiff = LocationTimeHolder.GetUTCGeoLngDiff(extEvent.Longitude - sys.lastUserLocation.Longitude);
                                string cDiffDirection = tsDiff.TotalMilliseconds > 0 ? "+" : "-";
                                if (tsDiff.TotalMilliseconds < 0)
                                    tsDiff = TimeSpan.FromMilliseconds(tsDiff.TotalMilliseconds * -1);

                                cPosInfo = cDiffDirection;
                                if (tsDiff.TotalMilliseconds < 0)
                                    tsDiff = TimeSpan.FromMilliseconds(tsDiff.TotalMilliseconds * -1);

                                if (tsDiff.TotalHours > 1)
                                    cPosInfo += ((int)tsDiff.TotalHours).ToString() + ":" + tsDiff.Minutes.ToString("00") + "h";
                                else if (tsDiff.TotalMinutes > 1)
                                    cPosInfo += ((int)tsDiff.TotalHours).ToString() + ":" + tsDiff.Minutes.ToString("00") + "h";
                                else if (tsDiff.TotalSeconds > 3)
                                    cPosInfo += tsDiff.Seconds.ToString("00") + "sec";
                                else
                                    cPosInfo += ":-)";
                                cPosInfo = cPosInfo.Trim();
                                cPosInfo += ", ";

                                if (IsRealWidget)
                                {
                                    Intent mapIntent = new Intent();
                                    mapIntent.PutExtra("_ClickCommand", "ActionView");
                                    mapIntent.SetData(Android.Net.Uri.Parse("geo:" + extEvent.Latitude.ToString(CultureInfo.InvariantCulture) + "," + extEvent.Longitude.ToString(CultureInfo.InvariantCulture)));
                                    rv.SetOnClickFillInIntent(Resource.Id.item_posoffset, mapIntent);
                                    rv.SetOnClickFillInIntent(Resource.Id.item_location, mapIntent);
                                }
                            }
                            else
                            {
                                if (IsRealWidget)
                                {
                                    Intent mapIntent = new Intent();
                                    mapIntent.PutExtra("_ClickCommand", "ActionView");
                                    mapIntent.SetData(Android.Net.Uri.Parse("geo:?q=" + calEvent.Location));
                                    rv.SetOnClickFillInIntent(Resource.Id.item_posoffset, mapIntent);
                                    rv.SetOnClickFillInIntent(Resource.Id.item_location, mapIntent);
                                }
                            }
                            rv.SetTextViewText(Resource.Id.item_posoffset, cPosInfo);
                            rv.SetTextColor(Resource.Id.item_posoffset, clPosInfo);
                            rv.SetViewVisibility(Resource.Id.item_posoffset, ViewStates.Visible);
                        }
                        else
                        {
                            rv.SetImageViewBitmap(Resource.Id.item_posicon, DrawableHelper.GetIconBitmap(mContext, Resource.Drawable.icons8_marker, 16, cfg.ColorEventSymbols));
                        }
                        rv.SetViewVisibility(Resource.Id.item_poslayout, ViewStates.Visible);
                        rv.SetTextViewText(Resource.Id.item_location, calEvent.Location);
                        rv.SetTextColor(Resource.Id.item_location, clLocation);
                    }
                    else
                        rv.SetViewVisibility(Resource.Id.item_poslayout, ViewStates.Gone);

                    if (cfg.ShowDesciption && !string.IsNullOrEmpty(calEvent.Description))
                    {
                        iShapeHeigth = 45;
                        rv.SetTextViewText(Resource.Id.item_description, calEvent.Description);
                        rv.SetTextColor(Resource.Id.item_description, cfg.ColorEventDescriptionText.ToAndroid());
                        rv.SetInt(Resource.Id.item_description, "setMaxLines", cfg.ShowDesciptionMaxLines);
                        rv.SetViewVisibility(Resource.Id.item_desclayout, ViewStates.Visible);
                    }
                    else
                        rv.SetViewVisibility(Resource.Id.item_desclayout, ViewStates.Gone);

                    if (extEvent.UseTypedTime && (wSize.X >= 150))
                        rv.SetViewVisibility(Resource.Id.img_sun_controlled, ViewStates.Visible);
                    else
                        rv.SetViewVisibility(Resource.Id.img_sun_controlled, ViewStates.Invisible);

                    rv.SetViewVisibility(Resource.Id.shape_overlay, ViewStates.Gone);
                    rv.SetTextViewText(Resource.Id.shape_overlay_text_allday, "");
                    if (!cfg.ShowEventColor || wSize.X < 120)
                        rv.SetViewVisibility(Resource.Id.color_shape, ViewStates.Gone);
                    else
                    {
                        try
                        {
                            GradientDrawable shape = new GradientDrawable();
                            shape.SetShape(ShapeType.Rectangle);
                            int i = 8;
                            shape.SetCornerRadii(new float[] { i, i, i, i, i, i, i, i });
                            shape.SetColor(calEvent.DisplayColor.ToAndroid());
                            shape.SetStroke((int)(3 * sys.DisplayDensity), calEvent.CalendarColor.ToAndroid());


                            if (wSize.X > 200)
                            {
                                iShapeWidth = 45;

                                rv.SetViewVisibility(Resource.Id.shape_overlay, ViewStates.Visible);
                                rv.SetTextViewText(Resource.Id.shape_overlay_text_h, "");
                                rv.SetTextViewText(Resource.Id.shape_overlay_text_m, "");
                                Color clShapeOverlay = Color.Black;
                                if (ColorUtils.CalculateLuminance(calEvent.DisplayColor.ToAndroid()) < 0.3)
                                    clShapeOverlay = Color.White;
                                rv.SetTextColor(Resource.Id.shape_overlay_text_h, clShapeOverlay);
                                rv.SetTextColor(Resource.Id.shape_overlay_text_m, clShapeOverlay);
                                rv.SetTextColor(Resource.Id.shape_overlay_text_allday, clShapeOverlay);

                                if (calEvent.AllDay)
                                    rv.SetTextViewText(Resource.Id.shape_overlay_text_allday, "~");
                                else
                                {
                                    rv.SetTextViewText(Resource.Id.shape_overlay_text_h, tStart.Hour.ToString());
                                    rv.SetTextViewText(Resource.Id.shape_overlay_text_m, tStart.Minute.ToString("00"));
                                }
                            }

                            rv.SetViewVisibility(Resource.Id.color_shape, ViewStates.Visible);
                            rv.SetImageViewBitmap(Resource.Id.color_shape, MainWidgetBase.GetDrawableBmp(shape, iShapeWidth, iShapeHeigth));

                        }
                        catch (Exception ex)
                        { xLog.Debug(ex.Message); }
                    }

                    // end feed row
                    // Next, set a fill-intent, which will be used to fill in the pending intent template
                    // that is set on the collection view in ListViewWidgetProvider.

                    if (IsRealWidget)
                    {
                        // Set the onClickFillInIntent  
                        Intent xxIntent = new Intent();
                        xxIntent.PutExtra("_ClickCommand", "StartActivityByComponentName");
                        xxIntent.PutExtra("_ComponentName", "me.ichrono.droid/me.ichrono.droid.ShortCutActivity");
                        xxIntent.PutExtra("shortcut", "edit_calender_event");
                        xxIntent.PutExtra("extra", calEvent.ExternalID);
                        rv.SetOnClickFillInIntent(Resource.Id.item_layout, xxIntent);
                    }

                    // Return the RemoteViews object.
                    return rv;
                }
                else
                {
                    RemoteViews rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_timetable_daysplitter);
                    rv.SetTextViewText(Resource.Id.item_title, "??????????: " + data.GetType().Name + ": " + data.ToString());
                    return rv;
                }
            }
            catch (Exception ex)
            {
                RemoteViews rv = new RemoteViews(mContext.PackageName, Resource.Layout.widget_calendar_timetable_daysplitter);
                rv.SetTextViewText(Resource.Id.item_title, ex.Message);
                rv.SetTextColor(Resource.Id.item_title, cfg.ColorErrorText.ToAndroid());
                rv.SetTextViewText(Resource.Id.item_title, ex.GetType().ToString());
                return rv;
            }
        }

        public int Count => myEvents?.AllDatesAndEvents.Count ?? 0;

        public bool HasStableIds => true;

        public RemoteViews LoadingView => null;

        public int ViewTypeCount
        {
            get
            {
                int count = 2;
                if (cfg != null)
                {
                    if (cfg.ShowLocation)
                        count++;
                    if (cfg.ShowDesciption)
                        count += cfg.ShowDesciptionMaxLines;
                }
                return count;
            }
        }

        public long GetItemId(int position)
        {
            return position;
        }

        int iLastCount = 0;
        public void OnDataSetChanged()
        {
            try
            {
                DateTime swLoadStart = DateTime.Now;
                xLog.Verbose("start");
                if (myEvents == null)
                    myEvents = new EventCollection();

                cfg = new WidgetConfigHolder().GetWidgetCfg<WidgetCfg_CalendarTimetable>(iMyWidgetId);
                myEvents.timeType = cfg.CurrentTimeType;
                calendarModel = new CalendarModelCfgHolder().GetModelCfg(cfg.CalendarModelId);

                wSize = MainWidgetBase.GetWidgetSize(iMyWidgetId, cfg, AppWidgetManager.GetInstance(mContext));

                //sichtbares Widget leeren und wieder füllen
                if (CalendarEventListService.ResetData)
                {
                    CalendarEventListService.ResetData = false;
                    myEvents.AllDatesAndEvents.Clear();
                    myEvents.Clear();
                    xLog.Debug("Did Data-Reset CalendarWidget: " + iMyWidgetId.ToString());

                    Task.Factory.StartNew(() =>
                    {
                        Task.Delay(100).Wait();
                        var mgr = AppWidgetManager.GetInstance(mContext);
                        mgr.NotifyAppWidgetViewDataChanged(iMyWidgetId, Resource.Id.event_list);
                    });

                    return;
                }
                xLog.Debug("LoadData CalendarWidget: " + iMyWidgetId.ToString());

                bool bPermissionError = false;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    try
                    {
                        if (mContext.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) != Android.Content.PM.Permission.Granted)
                        {
                            myEvents.AllDatesAndEvents.Add(mContext.Resources.GetString(Resource.String.widget_error_permission_calendar));
                            bPermissionError = true;
                        }

                        if (mContext.CheckSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted)
                        {
                            myEvents.AllDatesAndEvents.Add(mContext.Resources.GetString(Resource.String.widget_error_permission_location));
                            bPermissionError = true;
                        }

                    }
                    catch { }
                }

                if (bPermissionError)
                    return;

                Location mLoc = null;
                try { mLoc = Geolocation.GetLastKnownLocationAsync().Result; } catch { }
                if (mLoc != null)
                    sys.lastUserLocation = mLoc;
                else
                    mLoc = sys.lastUserLocation;

                string cCalendarFilter = "";
                if (!cfg.ShowAllCalendars)
                {
                    cCalendarFilter = "|";
                    foreach (string c in cfg.ShowCalendars)
                        cCalendarFilter += c + "|";
                }
                myEvents.CalendarFilter = cCalendarFilter;

                myEvents.DoLoadCalendarEventsGrouped(DateTime.Now, DateTime.Today.AddDays(cfg.MaxFutureDays + 1)).Wait();

                if (mLoc.Latitude == 0 && mLoc.Longitude == 0)
                    myEvents.AllDatesAndEvents.Insert(0, mContext.Resources.GetString(Resource.String.widget_warning_location_unknown));
                else if (mLoc.Timestamp.AddHours(1) < DateTime.Now)
                    myEvents.AllDatesAndEvents.Insert(0, mContext.Resources.GetString(Resource.String.widget_warning_location_out_of_date));

                iLastCount = Count;

#if DEBUG
                //myEvents.AllDatesAndEvents.Insert(0, wSize.X+" - "+wSize.Y);
                //myEvents.AllDatesAndEvents.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " loadet " + iLastCount.ToString() + " in " + (DateTime.Now - swLoadStart).TotalMilliseconds.ToString() + "ms");
#endif
            }
            catch (Exception ex)
            {
                myEvents = new EventCollection();
                myEvents.AllDatesAndEvents.Add(ex.Message);
            }
        }

        public new void Dispose()
        {
            myEvents.Clear();
            base.Dispose();
        }

        public void OnDestroy()
        {
            myEvents.Clear();
        }
    }
}