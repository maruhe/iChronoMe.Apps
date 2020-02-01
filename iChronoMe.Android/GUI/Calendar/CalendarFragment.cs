using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Syncfusion.Schedule;
using Android.Support.V7.App;
using iChronoMe.DeviceCalendar;
using Android.Content.PM;
using Android.Support.V4.App;
using Com.Syncfusion.Schedule.Enums;
using iChronoMe.Core.Classes;
using Android.Support.V4.Widget;
using Android.Support.Design.Widget;
using iChronoMe.Core.DynamicCalendar;
using System.Threading.Tasks;
using iChronoMe.Core.Types;
using iChronoMe.Core;
using iChronoMe.Droid.Widgets;

namespace iChronoMe.Droid.GUI.Calendar
{
    public class CalendarFragment : ActivityFragment, IMenuItemOnMenuItemClickListener, NavigationView.IOnNavigationItemSelectedListener
    {
        private DrawerLayout Drawer;
        private SfSchedule schedule;
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
            {                
                ActivityCompat.RequestPermissions(mContext, new string[] { Android.Manifest.Permission.ReadCalendar, Android.Manifest.Permission.WriteCalendar }, 1);
            }

            View view = inflater.Inflate(Resource.Layout.fragment_calendar, container, false);
            Drawer = view.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            schedule = view.FindViewById<SfSchedule>(Resource.Id.calendar_schedule);

            schedule.ItemsSource = calEvents.ListedDates;
            SetViewType(ScheduleView.WeekView);
            //schedule.Locale. = new Locale("de");
            schedule.AppointmentMapping = new AppointmentMapping() { Subject = "Name", StartTime = "javaDisplayStart", EndTime = "javaDisplayEnd", IsAllDay = "AllDay", Location = "Location", Notes = "Description", Color = "javaColor" };
            
            schedule.DayViewSettings.DayLabelSettings.TimeFormat = "HH";
            schedule.WeekViewSettings.WeekLabelSettings.TimeFormat = "HH";
            schedule.WorkWeekViewSettings.WorkWeekLabelSettings.TimeFormat = "HH";
            schedule.MonthViewSettings.AgendaViewStyle.TimeTextFormat = "HH:mm";

            schedule.MonthViewSettings.AppointmentDisplayMode = AppointmentDisplayMode.Indicator;
            schedule.MonthViewSettings.AppointmentIndicatorCount = 4;
            schedule.MonthViewSettings.ShowAgendaView = true;

            schedule.ScheduleViewChanged += Schedule_ScheduleViewChanged;
            schedule.VisibleDatesChanged += Schedule_VisibleDatesChanged;

            NavigationView navigationView = view.FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            var spinner = mContext?.FindViewById<Spinner>(Resource.Id.toolbar_spinner);
            if (spinner != null)
            {
                mContext.Title = "";
                spinner.Visibility = ViewStates.Visible;
                spinner.Adapter = new ArrayAdapter<string>(mContext, Android.Resource.Layout.SimpleSpinnerDropDownItem, new string[] { "Timeline", "Tag", "Woche", "Arbeit", "Monat" });
                Task.Factory.StartNew(() => { 
                    Task.Delay(2500).Wait(); 
                    spinner.ItemSelected += ViewSpinner_ItemSelected; 
                });
            }
        }

        private void ViewSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            switch(e.Position)
            {
                case 0:
                    SetViewType(ScheduleView.Timeline);
                    break;
                case 1:
                    SetViewType(ScheduleView.DayView);
                    break;
                case 2:
                    SetViewType(ScheduleView.WeekView);
                    break;
                case 3:
                    SetViewType(ScheduleView.WorkWeekView);
                    break;
                case 4:
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
                mContext.Title = "iChronoMe";
            }
        }

        private void Schedule_ScheduleViewChanged(object sender, ScheduleViewChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Schedule_VisibleDatesChanged(object sender, VisibleDatesChangedEventArgs e)
        {
            LoadEvents(e.VisibleDates.First(), e.VisibleDates.Last());
        }

        DateTime tFirstVisible, tLastVisible;

        private void LoadEvents(Java.Util.Calendar tVon, Java.Util.Calendar tBis)
        {
            LoadEvents(sys.DateTimeFromJava(tVon), sys.DateTimeFromJava(tBis));
        }

        private async void LoadEvents(DateTime? tVon = null, DateTime? tBis = null)
        {
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
        const int menu_debug = 1200;
        const int menu_debug_create_events = 1208;
        const int menu_debug_delete_events = 1209;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            var item = menu.Add(0, menu_options, 1000, "Options");
            item.SetIcon(Resource.Drawable.icons8_view_quilt);
            item.SetShowAsAction(ShowAsAction.Always);
            item.SetOnMenuItemClickListener(this);

            var sub = menu.AddSubMenu(0, 0, 100, "TimeType");
            sub.SetIcon(MainWidgetBase.GetTimeTypeIcon(calEvents.timeType, calEvents.locationTimeHolder));
            sub.Item.SetShowAsAction(ShowAsAction.Always);

            if (calEvents.timeType != TimeType.RealSunTime)
            {
                item = sub.Add(0, menu_typetype_RealSunTime, 0, "RealSunTime");
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.RealSunTime, calEvents.locationTimeHolder));
                item.SetOnMenuItemClickListener(this);
            }
            if (calEvents.timeType != TimeType.MiddleSunTime)
            {
                item = sub.Add(0, menu_typetype_MiddleSunTime, 0, "MiddleSunTime");
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.MiddleSunTime, calEvents.locationTimeHolder));
                item.SetOnMenuItemClickListener(this);
            }
            if (calEvents.timeType != TimeType.TimeZoneTime)
            {
                item = sub.Add(0, menu_typetype_TimeZoneTime, 0, "TimeZoneTime");
                item.SetIcon(MainWidgetBase.GetTimeTypeIcon(TimeType.TimeZoneTime, calEvents.locationTimeHolder));
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

        public bool OnMenuItemClick(IMenuItem item)
        {
            if (item.ItemId == menu_options)
            {
                if (Drawer.IsDrawerOpen((int)GravityFlags.Right))
                    Drawer.CloseDrawer((int)GravityFlags.Right);
                else
                    Drawer.OpenDrawer((int)GravityFlags.Right);
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
                    Start = sys.DateTimeFromJava(schedule.SelectedDate).Date.AddDays(new Random(DateTime.Now.Millisecond * DateTime.Now.Second).Next(80) - 50).AddHours(new Random().Next(8) + 10)
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
            if (schedule.ScheduleView == vType)
                return;
            schedule.ScheduleView = vType;
            //pViewType.SelectedItem = vType;
            //HeaderEndLayout.Children.Clear();

            switch (vType)
            {
                case ScheduleView.MonthView:
                    break;

                case ScheduleView.WeekView:
                    break;

                case ScheduleView.WorkWeekView:
                    break;

                case ScheduleView.DayView:
                    break;
            }
        }

        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            return OnNavigationItemSelected(menuItem.ItemId);
        }

        public bool OnNavigationItemSelected(int id)
        {
            if (id == Resource.Id.view_DayView)
            {
                SetViewType(ScheduleView.DayView);
            }
            else if (id == Resource.Id.view_WeekView)
            {
                SetViewType(ScheduleView.WeekView);
            }
            else if (id == Resource.Id.view_WorkWeekView)
            {
                SetViewType(ScheduleView.WorkWeekView);
            }
            else if (id == Resource.Id.view_MonthView)
            {
                SetViewType(ScheduleView.MonthView);
            }
            else if (id == Resource.Id.view_Timeline)
            {
                SetViewType(ScheduleView.Timeline);
            }
            Drawer.CloseDrawer((int)GravityFlags.Right);
            return true;
        }
    }
}