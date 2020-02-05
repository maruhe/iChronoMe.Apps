using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

using Com.Syncfusion.Schedule;
using Com.Syncfusion.Schedule.Enums;

using iChronoMe.Core;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.DeviceCalendar;
using iChronoMe.Droid.Widgets;

namespace iChronoMe.Droid.GUI.Calendar
{
    public class CalendarFragment : ActivityFragment, IMenuItemOnMenuItemClickListener
    {
        private DrawerLayout Drawer;
        private SfSchedule schedule;
        private Spinner ViewTypeSpinner;
        private AppCompatActivity mContext = null;
        private EventCollection calEvents = null;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            calEvents = new EventCollection();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);

            mContext = (AppCompatActivity)container.Context;

            if (mContext.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) != Permission.Granted)
                ActivityCompat.RequestPermissions(mContext, new string[] { Android.Manifest.Permission.ReadCalendar, Android.Manifest.Permission.WriteCalendar }, 1);

            View view = inflater.Inflate(Resource.Layout.fragment_calendar, container, false);
            Drawer = view.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            ViewTypeSpinner = mContext?.FindViewById<Spinner>(Resource.Id.toolbar_spinner);
            if (ViewTypeSpinner != null)
                ViewTypeSpinner.ItemSelected += ViewSpinner_ItemSelected;

            schedule = view.FindViewById<SfSchedule>(Resource.Id.calendar_schedule);
            schedule.Locale = Resources.Configuration.Locale;
            schedule.ItemsSource = null;

            schedule.HeaderHeight = 0;
            schedule.AppointmentMapping = new AppointmentMapping() { Subject = "Name", StartTime = "javaDisplayStart", EndTime = "javaDisplayEnd", IsAllDay = "AllDay", Location = "Location", Notes = "Description", Color = "javaColor" };

            if (CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.StartsWith("HH"))
            {
                schedule.TimelineViewSettings.LabelSettings.TimeFormat = "HH";
                schedule.DayViewSettings.DayLabelSettings.TimeFormat = "HH";
                schedule.WeekViewSettings.WeekLabelSettings.TimeFormat = "HH";
                schedule.WorkWeekViewSettings.WorkWeekLabelSettings.TimeFormat = "HH";
                schedule.MonthViewSettings.AgendaViewStyle.TimeTextFormat = "HH:mm";
            }

            try
            {
                Color clTitleText = xColor.White.ToAndroid();
                Color clTitleBack = xColor.FromHex("#2c3e50").ToAndroid();
                Color clText = clTitleText;
                Color clBack = xColor.FromHex("#2c3e50").ToAndroid();
                Color clTodayText = clTitleText;
                Color clAccent = xColor.FromHex("#1B3147").ToAndroid();

                schedule.HeaderStyle = new HeaderStyle { TextColor = clTitleText, BackgroundColor = clTitleBack };

                schedule.ViewHeaderStyle = new ViewHeaderStyle
                {
                    DayTextColor = clText,
                    DateTextColor = clText,
                    CurrentDateTextColor = clTodayText,
                    CurrentDayTextColor = clTodayText,
                    BackgroundColor = clBack
                };

                schedule.MonthCellStyle = new MonthCellStyle
                {
                    TextColor = clText,
                    BackgroundColor = clBack,
                    TodayTextColor = clTodayText,
                    TodayBackgroundColor = clBack,
                    PreviousMonthBackgroundColor = clAccent,
                    NextMonthBackgroundColor = clAccent                    
                };

            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            try
            {
                var cfg = AppConfigHolder.CalendarViewConfig;

                schedule.TimelineViewSettings.StartHour = 6;
                schedule.TimelineViewSettings.EndHour = 22;

                schedule.MonthViewSettings.ShowAppointmentsInline = cfg.ShowInlineEvents;
                schedule.MonthViewSettings.ShowAgendaView = !schedule.MonthViewSettings.ShowAppointmentsInline;
                schedule.MonthViewSettings.AppointmentDisplayMode = (AppointmentDisplayMode)Enum.ToObject(typeof(AppointmentDisplayMode), cfg.AppointmentDisplayMode);
                schedule.MonthViewSettings.AppointmentIndicatorCount = cfg.AppointmentIndicatorCount;

                schedule.ScheduleViewChanged += Schedule_ScheduleViewChanged;
                schedule.VisibleDatesChanged += Schedule_VisibleDatesChanged;

                if (ViewTypeSpinner != null)
                {
                    mContext.Title = "";
                    ViewTypeSpinner.Visibility = ViewStates.Visible;
                    ViewTypeSpinner.Touch += ViewTypeSpinner_Touch;
                }
                if (cfg.DefaultViewType < 0)
                    SetViewType((ScheduleView)Enum.ToObject(typeof(ScheduleView), cfg.LastViewType));
                else
                    SetViewType((ScheduleView)Enum.ToObject(typeof(ScheduleView), cfg.DefaultViewType));
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
            //NavigationView navigationView = view.FindViewById<NavigationView>(Resource.Id.nav_view);
            //navigationView.SetNavigationItemSelectedListener(this);

            return view;
        }

        bool bPermissionCheckOk = false;
        public override void OnResume()
        {
            base.OnResume();
            bPermissionCheckOk = mContext.CheckSelfPermission(Android.Manifest.Permission.ReadCalendar) == Permission.Granted && mContext.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) == Permission.Granted;
        }

        bool bViewSpinnerActive = false;
        private void ViewSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (!bViewSpinnerActive)
                return;
            switch (e.Position)
            {
                case 1:
                    SetViewType(ScheduleView.Timeline);
                    break;
                case 2:
                    SetViewType(ScheduleView.DayView);
                    break;
                case 3:
                    SetViewType(ScheduleView.WeekView);
                    break;
                case 4:
                    SetViewType(ScheduleView.WorkWeekView);
                    break;
                case 5:
                    SetViewType(ScheduleView.MonthView);
                    break;
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            var spinner = mContext?.FindViewById<Spinner>(Resource.Id.toolbar_spinner);
            if (spinner != null)
            {
                spinner.Visibility = ViewStates.Gone;
                spinner.ItemSelected -= ViewSpinner_ItemSelected;
                mContext.Title = Resources.GetString(Resource.String.app_name);
            }
        }

        private void Schedule_ScheduleViewChanged(object sender, ScheduleViewChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Schedule_VisibleDatesChanged(object sender, VisibleDatesChangedEventArgs e)
        {
            DateTime tFirst = sys.DateTimeFromJava(e.VisibleDates.First());
            DateTime tLast = sys.DateTimeFromJava(e.VisibleDates.Last());

            ResetTitleSpinner(tFirst, tLast);
            LoadEvents(tFirst, tLast);
        }

        DateTime tFirstVisible, tLastVisible;

        private async void LoadEvents(DateTime? tVon = null, DateTime? tBis = null)
        {
            if (!bPermissionCheckOk)
                return;
            if (tVon != null)
            {
                tFirstVisible = (DateTime)tVon;
                tLastVisible = (DateTime)tBis;
                if (tLastVisible <= tFirstVisible)
                    tLastVisible = tFirstVisible.AddDays(1);
            }
            await calEvents.DoLoadCalendarEventsListed(tFirstVisible, tLastVisible);
            mContext.RunOnUiThread(() => { schedule.ItemsSource = new List<CalendarEvent>(calEvents.ListedDates); });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            LoadEvents();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        const int menu_options = 1001;
        const int menu_typetype_RealSunTime = 1101;
        const int menu_typetype_MiddleSunTime = 1102;
        const int menu_typetype_TimeZoneTime = 1103;
        const int menu_debug_create_events = 1208;
        const int menu_debug_delete_events = 1209;

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

            try
            {
                var item = menu.Add(0, menu_options, 1000, Resources.GetString(Resource.String.action_options));
                item.SetIcon(Resource.Drawable.icons8_view_quilt);
                item.SetShowAsAction(ShowAsAction.Always);
                item.SetOnMenuItemClickListener(this);

                var sub = menu.AddSubMenu(0, 0, 100, Resources.GetString(Resource.String.TimeType));
                sub.SetIcon(MainWidgetBase.GetTimeTypeIcon(calEvents.timeType, LocationTimeHolder.LocalInstance));
                sub.Item.SetShowAsAction(ShowAsAction.Always);

                if (calEvents.timeType != TimeType.RealSunTime)
                {
                    item = sub.Add(0, menu_typetype_RealSunTime, 0, Resources.GetString(Resource.String.TimeType_RealSunTime));
                    item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.RealSunTime, LocationTimeHolder.LocalInstance));
                    item.SetOnMenuItemClickListener(this);
                }
                if (calEvents.timeType != TimeType.MiddleSunTime)
                {
                    item = sub.Add(0, menu_typetype_MiddleSunTime, 0, Resources.GetString(Resource.String.TimeType_MiddleSunTime));
                    item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.MiddleSunTime, LocationTimeHolder.LocalInstance));
                    item.SetOnMenuItemClickListener(this);
                }
                if (calEvents.timeType != TimeType.TimeZoneTime)
                {
                    item = sub.Add(0, menu_typetype_TimeZoneTime, 0, Resources.GetString(Resource.String.TimeType_TimeZoneTime));
                    item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.TimeZoneTime, LocationTimeHolder.LocalInstance));
                    item.SetOnMenuItemClickListener(this);
                }
#if DEBUG
                sub = menu.AddSubMenu(0, 0, 0, "Debug");
                sub.SetIcon(Resource.Drawable.icons8_services);
                sub.Item.SetShowAsAction(ShowAsAction.Always);

                item = sub.Add(0, menu_debug_create_events, 0, "Create Events");
                item.SetIcon(Resource.Drawable.icons8_add);
                item.SetOnMenuItemClickListener(this);

                item = sub.Add(0, menu_debug_delete_events, 0, "Delete Events");
                item.SetIcon(Resource.Drawable.icons8_delete);
                item.SetOnMenuItemClickListener(this);
#endif
            }
            catch (Exception ex)
            {
                sys.LogException(ex);
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            try
            {
                if (item.ItemId == menu_options)
                {
                    if (Drawer.IsDrawerOpen((int)GravityFlags.End))
                        Drawer.CloseDrawer((int)GravityFlags.End);
                    else
                        Drawer.OpenDrawer((int)GravityFlags.End);
                }
#if DEBUG
                else if (item.ItemId == menu_debug_create_events)
                    createTestEvents();
                else if (item.ItemId == menu_debug_delete_events)
                    deleteAllEvents();
#endif
                else if (item.ItemId == menu_typetype_RealSunTime)
                    SetTimeType(TimeType.RealSunTime);
                else if (item.ItemId == menu_typetype_MiddleSunTime)
                    SetTimeType(TimeType.MiddleSunTime);
                else if (item.ItemId == menu_typetype_TimeZoneTime)
                    SetTimeType(TimeType.TimeZoneTime);
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
            return true;
        }

#if DEBUG
        private async void createTestEvents()
        {
            // testing all kinds of adding events
            await GenerateEvents(12, "Cool");
            await GenerateEvents(7, "Boring");
            //Task.Factory.StartNew(() => { LoadEvents(); });
            await GenerateEvents(10, "New");
            await GenerateEvents(10, "Test");
            await GenerateEvents(1, "geo?");
            LoadEvents();
        }

        private async void deleteAllEvents()
        {
            //if (!(await DisplayAlert("löschen?", "alle löschen?", "löschen!", "abbrechen")))
            //  return;

            DateTime calStart = new DateTime(2019, 01, 01);
            DateTime calEnd = calStart.AddDays(1000);
            var calendar = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();
            var calEvents = await DeviceCalendar.DeviceCalendar.GetEventsAsync(calendar, calStart, calEnd);
            foreach (var ev in calEvents)
            {
                try
                {
                    await DeviceCalendar.DeviceCalendar.DeleteEventAsync(calendar, ev);
                }
                catch { }
            }
            LoadEvents();
        }

        private async Task GenerateEvents(int count, string name)
        {
            var xDate = schedule.SelectedDate != null ? sys.DateTimeFromJava(schedule.SelectedDate) : sys.DateTimeFromJava(schedule.VisibleDates.First());
            string[] cLocations = new string[] { "Hyderabad, Pakistan", "Oran, Algeria", "Mexico City, Mexico", "Cairo, Egypt", "Barranquilla, Colombia", "Philadelphia, United States", "Tashkent, Uzbekistan", "Changchun, China", "Lima, Peru", "Brazzaville, Congo Republic", "Shiraz, Iran", "Los Angeles, United States", "Lagos, Nigeria", "Vienna, Austria", "Manila, Philippines", "Ankara, Turkey", "Hamburg, Germany", "Peshawar, Pakistan", "Kwangju,Korea, South", "Curitiba, Brazil", "Bengaluru, India", "Pune, India", "Patna, India", "Wenzhou, China", "Bandung, Indonesia", "Taichung, Taiwan", "Wuhan, China", "Davao City, Philippines", "Tijuana, Mexico", "Rosario, Argentina", "Lanzhou, China", "Barcelona, Spain", "Alexandria, Egypt", "Harare, Zimbabwe", "Singapore, Singapore", "Medan, Indonesia", "Saitama, Japan", "New York City, United States", "Bhopal, India", "Yerevan, Armenia", "Karachi, Pakistan", "Moscow, Russia", "Bulawayo, Zimbabwe", "Beijing, China", "Chennai, India", "Fukuoka, Japan", "Havana, Cuba", "Omsk, Russia", "Kolkata, India", "Kyoto, Japan", "Rome, Italy", "Surat, India", "Dhaka, Bangladesh", "Shijiazhuang, China", "Pyongyang, North", "Quanzhou, China", "Suzhou, China", "Cologne, Germany", "Cali, Colombia", "Harbin, China", "Shenzhen, China", "Ho Chi Minh City, Vietnam", "Shanghai, China", "Córdoba, Argentina", "Zhengzhou, China", "Recife, Brazil", "Vijayawada, India", "Surabaya, Indonesia", "Rio de Janeiro, Brazil", "Monterrey, Mexico", "Warsaw, Poland", "Santiago, Chile", "Kinshasa, DR Congo", "Jeddah, Saudi Arabia", "San Diego, United States", "Palembang, Indonesia", "Melbourne, Australia", "Fortaleza, Brazil", "Porto Alegre, Brazil", "Nanjing, China", "Ulsan, South", "Hyderabad, India", "Xi'an, China", "Kuala Lumpur, Malaysia", "Belo Horizonte, Brazil", "Kharkiv, Ukraine", "Seoul, Korea,South", "Yokohama, Japan", "Astana, Kazakhstan", "Ningbo, China", "Mandalay, Myanmar", "Phoenix, United States", "New Taipei City, Taiwan", "Birmingham, United Kingdom", "Kiev, Ukraine", "Xiamen, China", "Johannesburg, South Africa", "Tabriz, Iran", "Ekurhuleni, South Africa", "Rawalpindi, Pakistan", "Quezon City, Philippines", "Kanpur, India", "Hong Kong, China", "Khartoum, Sudan", "Rostov-on-Don, Russia", "Maputo, Mozambique", "Milan, Italy", "Busan, Korea,South", "Prague, Czech Republic", "Yekaterinburg, Russia", "Visakhapatnam, India", "Daejeon, South", "Kabul, Afghanistan", "Quito, Ecuador", "Kano, Nigeria", "Tripoli, Libya", "Munich, Germany", "Giza, Egypt", "São Paulo, Brazil", "Novosibirsk, Russia", "Foshan, China", "Dongguan, China", "Kampala, Uganda", "Yaoundé, Cameroon", "Ibadan, Nigeria", "Nagpur, India", "Hiroshima, Japan", "Fez, Morocco", "Sapporo, Japan", "Cape Town, South Africa", "Luanda, Angola", "Hangzhou, China", "Tianjin, China", "Douala, Cameroon", "Delhi, India", "Faisalabad, Pakistan", "Incheon, South", "Sana'a, Yemen", "Ahmedabad, India", "Accra, Ghana", "Basra, Iraq", "Kobe, Japan", "Tokyo, Japan", "London, United Kingdom", "Addis Ababa, Ethiopia", "Buenos Aires, Argentina", "Medellin, Colombia", "Jaipur, India", "Riyadh, Saudi Arabia", "Chongqing, China", "Isfahan, Iran", "Caracas, Venezuela", "Brisbane, Australia", "Bangkok, Thailand", "Caloocan, Philippines", "Guadalajara, Mexico", "Phnom Penh, Cambodia", "Daegu, South", "Santa Cruz de la Sierra, Bolivia", "Almaty, Kazakhstan", "Dalian, China", "Paris, France", "Hanoi, Vietnam", "Gujranwala, Pakistan", "Auckland, New Zealand", "Abuja, Nigeria", "Algiers, Algeria", "Guatemala City, Guatemala", "Semarang, Indonesia", "Kawasaki, Japan", "Brasília, Brazil", "Dakar, Senegal", "İzmir, Turkey", "Shantou, China", "Changsha, China", "Sofia, Bulgaria", "Tunis, Tunisia", "Ouagadougou, Burkina Faso", "Madrid, Spain", "Istanbul, Turkey", "Tehran, Iran", "Tainan, Taiwan", "Qingdao, China", "Saint Petersburg, Russia", "Montreal, Canada", "Abidjan, Ivory Coast", "Casablanca, Morocco", "Baku, Azerbaijan", "Baghdad, Iraq", "Jinan, China", "Mumbai, India", "Calgary, Canada", "Chittagong, Bangladesh", "Chaozhou, China", "Budapest, Hungary", "Suwon, South Korea", "Mashhad, Iran", "Lucknow, India", "Montevideo, Uruguay", "Karaj, Iran", "Tangshan, China", "Qom, Iran", "Sydney, Australia", "Guangzhou, China", "Zhongshan, China", "Taipei, Taiwan", "Nairobi, Kenya", "Dubai, United Arab Emirates", "Guayaquil, Ecuador", "Makassar, Indonesia", "Jakarta, Indonesia", "Toronto, Canada", "Houston, UnitedStates", "Dar es Salaam, Tanzania", "Shenyang, China", "Zunyi, China", "Chengdu, China", "Dallas, United States", "Osaka, Japan", "Belgrade, Serbia", "T'bilisi, Georgia", "Minsk, Belarus", "Berlin, Germany", "Nizhny Novgorod, Russia", "Kaohsiung, Taiwan", "Nagoya, Japan", "Campinas, Brazil", "Chicago, UnitedStates", "Fuzhou, China", "Islamabad, Pakistan", "Bucharest, Romania", "Managua, Nicaragua", "Lahore, Pakistan", "Hefei, China", "Yangon, Myanmar", "Durban, South Africa", "Abu Dhabi, United Arab Emirates", "Salvador, Brazil", "San Antonio, United States", "Ahvaz, Iran", "Lusaka, Zambia", "Bogotá, Colombia", "Kathmandu, Nepal", "Maracaibo, Venezuela" };
            var cal = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();
            for (int i = 0; i < count; i++)
            {
                Random rnd = new Random(DateTime.Now.Millisecond * DateTime.Now.Second * i);
                rnd = new Random(rnd.Next(851457));
                double dDec = rnd.Next(851457);
                while (dDec > 1)
                    dDec /= 10;
                CalendarEvent e = new CalendarEvent
                {
                    Name = $"{name} event {i}",
                    Description = $"This is {name} event{i}'s description!",
                    Start = xDate.Date.AddDays(new Random(DateTime.Now.Millisecond * DateTime.Now.Second).Next(80) - 50).AddHours(new Random().Next(8) + 10)
                };
                e.End = e.Start.AddHours(new Random().Next(5) + 1);
                e.Location = cLocations[rnd.Next(cLocations.Length - 1)];//(nLat + dDec / 2).ToString("#.000000", CultureInfo.InvariantCulture) + ", " + (nLng + dDec).ToString("#.000000", CultureInfo.InvariantCulture);
                e.EventColor = xColor.FromRgb(rnd.Next(200), rnd.Next(200), rnd.Next(200));

                await DeviceCalendar.DeviceCalendar.AddOrUpdateEventAsync(cal, e);

                /*if (!string.IsNullOrEmpty(e.ExternalID))
                {
                    TimeType TimeType = TimeType.RealSunTime;
                    //if (rnd.Next(2) == 1)
                    //TimeType = TimeType.MiddleSunTime;
                    var extEvent = CalendarEventExtention.GetExtention(e.ExternalID);
                    extEvent.TimeType = TimeType;
                    extEvent.TimeTypeStart = sys.GetTimeWithoutSeconds(e.Start);
                    extEvent.TimeTypeEnd = sys.GetTimeWithoutSeconds(e.End);
                    extEvent.UseTypedTime = TimeType == TimeType.MiddleSunTime || TimeType == TimeType.RealSunTime;
                    extEvent.CalendarTimeStart = sys.GetTimeWithoutSeconds(e.Start);
                    extEvent.CalendarTimeEnd = sys.GetTimeWithoutSeconds(e.End);
                    db.dbCalendarExtention.Insert(extEvent);
                }*/
            }
        }
#endif

        public void SetTimeType(TimeType tt)
        {
            calEvents.timeType = tt;
            mContext.InvalidateOptionsMenu();
            LoadEvents();
        }

        public void SetViewType(ScheduleView vType)
        {
            try
            {
                ResetTitleSpinner(tFirstVisible, tLastVisible);
                if (schedule.ScheduleView != vType)
                    schedule.ScheduleView = vType;
            } catch (Exception ex) { sys.LogException(ex); }
        }

        private void ViewTypeSpinner_Touch(object sender, View.TouchEventArgs e)
        {
            try
            {
                var lst = new List<string>();
                lst.Add(ViewTypeSpinner.Prompt);
                lst.AddRange(Resources.GetStringArray(Resource.Array.calendar_viewtypes));
                ViewTypeSpinner.Adapter = new ArrayAdapter<string>(mContext, Android.Resource.Layout.SimpleSpinnerDropDownItem, lst.ToArray());                
                ViewTypeSpinner.PerformClick();
                bViewSpinnerActive = true;
            } catch { }
        }

        private void ResetTitleSpinner(DateTime tFirstVisible, DateTime tLastVisible)
        {
            bViewSpinnerActive = false;
            if (ViewTypeSpinner != null)
            {
                string cTitle = tFirstVisible.ToShortDateString();
                switch (schedule.ScheduleView)
                {
                    case ScheduleView.Timeline:
                    case ScheduleView.DayView:
                        cTitle = tFirstVisible.ToString("MMMM yyyy");
                        break;
                    case ScheduleView.WeekView:
                    case ScheduleView.WorkWeekView:
                        cTitle = tFirstVisible.ToString("MMMM yyyy");
                        if (tLastVisible.Month != tFirstVisible.Month)
                            cTitle += " - " + tLastVisible.ToString("MMMM yyyy");
                        break;
                    case ScheduleView.MonthView:
                        cTitle = tFirstVisible.AddDays((tLastVisible-tFirstVisible).Days / 2).ToString("MMMM yyyy");
                        break;
                }

                ViewTypeSpinner.Prompt = cTitle;
                ViewTypeSpinner.Adapter = new ArrayAdapter<string>(mContext, Android.Resource.Layout.SimpleSpinnerDropDownItem, new string[] { cTitle });

            }
        }
    }
}