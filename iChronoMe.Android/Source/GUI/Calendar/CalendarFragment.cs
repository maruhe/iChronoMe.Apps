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
using iChronoMe.Core.DataBinding;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.DeviceCalendar;
using iChronoMe.Droid.Source.Adapters;
using iChronoMe.Droid.Source.ViewModels;
using iChronoMe.Droid.Widgets;
using Xamarin.Essentials;

namespace iChronoMe.Droid.GUI.Calendar
{
    public class CalendarFragment : ActivityFragment, IMenuItemOnMenuItemClickListener
    {
        private DrawerLayout Drawer;
        private CoordinatorLayout coordinator;
        private SfSchedule schedule;
        private Spinner ViewTypeSpinner;
        private AppCompatActivity mContext = null;
        private EventCollection calEvents = null;
        private ArrayAdapter<string> ViewTypeAdapter = null;
        private FloatingActionButton fabTimeType;
        private CalendarConfigViewModel ConfigModel;
        private DataBinder ConfigBinder;
        private ListView lvCalendars;

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
            Drawer.DrawerStateChanged += Drawer_DrawerStateChanged;
            coordinator = view.FindViewById<CoordinatorLayout>(Resource.Id.coordinator_layout);

            ViewTypeAdapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleSpinnerDropDownItem, new List<string>(new string[] { "init.." }));
            ViewTypeSpinner = mContext?.FindViewById<Spinner>(Resource.Id.toolbar_spinner);
            if (ViewTypeSpinner != null)
            {
                ViewTypeSpinner.ItemSelected += ViewTypeSpinner_ItemSelected;
                ViewTypeSpinner.Adapter = ViewTypeAdapter;
            }
            lvCalendars = view.FindViewById<ListView>(Resource.Id.lv_calendars);

            schedule = view.FindViewById<SfSchedule>(Resource.Id.calendar_schedule);
            schedule.Locale = Resources.Configuration.Locale;
            schedule.ItemsSource = null;

            schedule.HeaderHeight = 0;
            schedule.AppointmentMapping = new AppointmentMapping() { Subject = "Name", StartTime = "javaDisplayStart", EndTime = "javaDisplayEnd", IsAllDay = "AllDay", Location = "Location", Notes = "Description", Color = "javaColor" };

            schedule.VisibleDatesChanged += Schedule_VisibleDatesChanged;
            schedule.CellDoubleTapped += Schedule_CellDoubleTapped;

            if (ViewTypeSpinner != null)
            {
                mContext.Title = "";
                ViewTypeSpinner.Visibility = ViewStates.Visible;
                ViewTypeSpinner.Touch += ViewTypeSpinner_Touch;
            }
            if (AppConfigHolder.CalendarViewConfig.DefaultViewType < 0)
                SetViewType((ScheduleView)Enum.ToObject(typeof(ScheduleView), AppConfigHolder.CalendarViewConfig.LastViewType));
            else
                SetViewType((ScheduleView)Enum.ToObject(typeof(ScheduleView), AppConfigHolder.CalendarViewConfig.DefaultViewType));
            //SearchScheduleColors(schedule, null);
            LoadCalendarConfig();

            ConfigModel = new CalendarConfigViewModel(Activity, schedule);
            ConfigBinder = ConfigModel.GetDataBinder(view.FindViewById<NavigationView>(Resource.Id.nav_view));

            fabTimeType = view.FindViewById<FloatingActionButton>(Resource.Id.btn_time_type);
            fabTimeType.Click += Fab_Click;

            //SearchScheduleColors(schedule, view.FindViewById<LinearLayout>(Resource.Id.ll_colorlist));
            cColorTree.ToString();

            return view;
        }

        private void Schedule_CellDoubleTapped(object sender, CellTappedEventArgs e)
        {
            if (e.ScheduleAppointment != null)
                return;
            switch (schedule.ScheduleView)
            {
                case ScheduleView.MonthView:
                    SetViewType(ScheduleView.WeekView);
                    break;
                case ScheduleView.WorkWeekView:
                    SetViewType(ScheduleView.DayView);
                    break;
                case ScheduleView.WeekView:
                    SetViewType(ScheduleView.DayView);
                    break;
            }
        }

        private void Drawer_DrawerStateChanged(object sender, DrawerLayout.DrawerStateChangedEventArgs e)
        {
            if (e.NewState == 2)
            {
                if (Drawer.IsDrawerOpen((int)GravityFlags.End))
                    ConfigBinder.Stop();
                else
                    ConfigBinder.Start();
            }
        }

        bool bPermissionCheckOk = false;
        public override void OnResume()
        {
            base.OnResume();
            bPermissionCheckOk = mContext.CheckSelfPermission(Android.Manifest.Permission.ReadCalendar) == Permission.Granted && mContext.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) == Permission.Granted;
            if (bPermissionCheckOk)
            {
                lvCalendars.ChoiceMode = ChoiceMode.Multiple;
                lvCalendars.ItemsCanFocus = false;
                var adapter = new CalendarListAdapter(Activity);
                lvCalendars.Adapter = adapter;
                lvCalendars.ItemClick += adapter.ListView_ItemClick;
                adapter.HiddenCalendarsChanged += Adapter_HiddenCalendarsChanged;
            }
        }

        private void Adapter_HiddenCalendarsChanged(object sender, EventArgs e)
        {
            calEvents.RefreshCalendarFilter(AppConfigHolder.CalendarViewConfig.HideCalendars);
            LoadEvents();
        }

        public override void OnPause()
        {
            base.OnPause();
            var spinner = mContext?.FindViewById<Spinner>(Resource.Id.toolbar_spinner);
            if (spinner != null)
            {
                spinner.Visibility = ViewStates.Gone;
                spinner.ItemSelected -= ViewTypeSpinner_ItemSelected;
                mContext.Title = Resources.GetString(Resource.String.app_name);
            }
            SaveCalendarConfig();
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
            mContext.RunOnUiThread(() => fabTimeType.SetImageResource(MainWidgetBase.GetTimeTypeIcon(calEvents.timeType, LocationTimeHolder.LocalInstance)));
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

            CheckCalendarWelcomeAssistant();
        }

        private async void CheckCalendarWelcomeAssistant()
        {
            if (AppConfigHolder.CalendarViewConfig.WelcomeScreenDone < 1)
            {
                var def = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();
                if (def == null)
                {
                    if (await Tools.ShowYesNoMessage(Context, "Kein Calender gefunden", "sollen wir einen anlegen?"))
                    {
                        await DeviceCalendar.DeviceCalendar.AddOrUpdateCalendarAsync(new DeviceCalendar.Calendar { Name = "iChronoMe" });
                    }
                    else
                        return;
                }
                AppConfigHolder.CalendarViewConfig.WelcomeScreenDone = 1;
                AppConfigHolder.SaveCalendarViewConfig();
            }
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

                /*var sub = menu.AddSubMenu(0, 0, 100, Resources.GetString(Resource.String.TimeType));
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
                */
#if DEBUG
                var sub = menu.AddSubMenu(0, 0, 0, "Debug");
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

        #region debug stuff
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
            Tools.ShowToast(Context, "events created");
            LoadEvents();
        }

        private async void deleteAllEvents()
        {
            var calendar = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();
            if (calendar == null)
                return;

            new AlertDialog.Builder(Context).
                SetMessage("alle Termine in " + calendar.Name + " löschen?")
                .SetPositiveButton("ja", (s, e) =>
                {
                    Task.Factory.StartNew(async () =>
                    {
                        int iDeleted = 0;
                        DateTime calStart = new DateTime(2019, 01, 01);
                        DateTime calEnd = calStart.AddDays(1000);
                        var calEvents = await DeviceCalendar.DeviceCalendar.GetEventsAsync(calendar, calStart, calEnd);
                        foreach (var ev in calEvents)
                        {
                            try
                            {
                                await DeviceCalendar.DeviceCalendar.DeleteEventAsync(calendar, ev);
                                iDeleted++;
                            }
                            catch { }
                        }
                        Toast.MakeText(Context, iDeleted + " events deleted", ToastLength.Short).Show();
                        LoadEvents();
                    });
                })
                .SetNegativeButton("neeeeiin!", (s, e) => { })
                .Create().Show();
        }

        private async Task GenerateEvents(int count, string name)
        {
            xLog.Debug("GenerateEvents: " + count + " : " + name);

            var xDate = schedule.SelectedDate != null ? sys.DateTimeFromJava(schedule.SelectedDate) : sys.DateTimeFromJava(schedule.VisibleDates.First());
            string[] cLocations = new string[] { "Hyderabad, Pakistan", "Oran, Algeria", "Mexico City, Mexico", "Cairo, Egypt", "Barranquilla, Colombia", "Philadelphia, United States", "Tashkent, Uzbekistan", "Changchun, China", "Lima, Peru", "Brazzaville, Congo Republic", "Shiraz, Iran", "Los Angeles, United States", "Lagos, Nigeria", "Vienna, Austria", "Manila, Philippines", "Ankara, Turkey", "Hamburg, Germany", "Peshawar, Pakistan", "Kwangju,Korea, South", "Curitiba, Brazil", "Bengaluru, India", "Pune, India", "Patna, India", "Wenzhou, China", "Bandung, Indonesia", "Taichung, Taiwan", "Wuhan, China", "Davao City, Philippines", "Tijuana, Mexico", "Rosario, Argentina", "Lanzhou, China", "Barcelona, Spain", "Alexandria, Egypt", "Harare, Zimbabwe", "Singapore, Singapore", "Medan, Indonesia", "Saitama, Japan", "New York City, United States", "Bhopal, India", "Yerevan, Armenia", "Karachi, Pakistan", "Moscow, Russia", "Bulawayo, Zimbabwe", "Beijing, China", "Chennai, India", "Fukuoka, Japan", "Havana, Cuba", "Omsk, Russia", "Kolkata, India", "Kyoto, Japan", "Rome, Italy", "Surat, India", "Dhaka, Bangladesh", "Shijiazhuang, China", "Pyongyang, North", "Quanzhou, China", "Suzhou, China", "Cologne, Germany", "Cali, Colombia", "Harbin, China", "Shenzhen, China", "Ho Chi Minh City, Vietnam", "Shanghai, China", "Córdoba, Argentina", "Zhengzhou, China", "Recife, Brazil", "Vijayawada, India", "Surabaya, Indonesia", "Rio de Janeiro, Brazil", "Monterrey, Mexico", "Warsaw, Poland", "Santiago, Chile", "Kinshasa, DR Congo", "Jeddah, Saudi Arabia", "San Diego, United States", "Palembang, Indonesia", "Melbourne, Australia", "Fortaleza, Brazil", "Porto Alegre, Brazil", "Nanjing, China", "Ulsan, South", "Hyderabad, India", "Xi'an, China", "Kuala Lumpur, Malaysia", "Belo Horizonte, Brazil", "Kharkiv, Ukraine", "Seoul, Korea,South", "Yokohama, Japan", "Astana, Kazakhstan", "Ningbo, China", "Mandalay, Myanmar", "Phoenix, United States", "New Taipei City, Taiwan", "Birmingham, United Kingdom", "Kiev, Ukraine", "Xiamen, China", "Johannesburg, South Africa", "Tabriz, Iran", "Ekurhuleni, South Africa", "Rawalpindi, Pakistan", "Quezon City, Philippines", "Kanpur, India", "Hong Kong, China", "Khartoum, Sudan", "Rostov-on-Don, Russia", "Maputo, Mozambique", "Milan, Italy", "Busan, Korea,South", "Prague, Czech Republic", "Yekaterinburg, Russia", "Visakhapatnam, India", "Daejeon, South", "Kabul, Afghanistan", "Quito, Ecuador", "Kano, Nigeria", "Tripoli, Libya", "Munich, Germany", "Giza, Egypt", "São Paulo, Brazil", "Novosibirsk, Russia", "Foshan, China", "Dongguan, China", "Kampala, Uganda", "Yaoundé, Cameroon", "Ibadan, Nigeria", "Nagpur, India", "Hiroshima, Japan", "Fez, Morocco", "Sapporo, Japan", "Cape Town, South Africa", "Luanda, Angola", "Hangzhou, China", "Tianjin, China", "Douala, Cameroon", "Delhi, India", "Faisalabad, Pakistan", "Incheon, South", "Sana'a, Yemen", "Ahmedabad, India", "Accra, Ghana", "Basra, Iraq", "Kobe, Japan", "Tokyo, Japan", "London, United Kingdom", "Addis Ababa, Ethiopia", "Buenos Aires, Argentina", "Medellin, Colombia", "Jaipur, India", "Riyadh, Saudi Arabia", "Chongqing, China", "Isfahan, Iran", "Caracas, Venezuela", "Brisbane, Australia", "Bangkok, Thailand", "Caloocan, Philippines", "Guadalajara, Mexico", "Phnom Penh, Cambodia", "Daegu, South", "Santa Cruz de la Sierra, Bolivia", "Almaty, Kazakhstan", "Dalian, China", "Paris, France", "Hanoi, Vietnam", "Gujranwala, Pakistan", "Auckland, New Zealand", "Abuja, Nigeria", "Algiers, Algeria", "Guatemala City, Guatemala", "Semarang, Indonesia", "Kawasaki, Japan", "Brasília, Brazil", "Dakar, Senegal", "İzmir, Turkey", "Shantou, China", "Changsha, China", "Sofia, Bulgaria", "Tunis, Tunisia", "Ouagadougou, Burkina Faso", "Madrid, Spain", "Istanbul, Turkey", "Tehran, Iran", "Tainan, Taiwan", "Qingdao, China", "Saint Petersburg, Russia", "Montreal, Canada", "Abidjan, Ivory Coast", "Casablanca, Morocco", "Baku, Azerbaijan", "Baghdad, Iraq", "Jinan, China", "Mumbai, India", "Calgary, Canada", "Chittagong, Bangladesh", "Chaozhou, China", "Budapest, Hungary", "Suwon, South Korea", "Mashhad, Iran", "Lucknow, India", "Montevideo, Uruguay", "Karaj, Iran", "Tangshan, China", "Qom, Iran", "Sydney, Australia", "Guangzhou, China", "Zhongshan, China", "Taipei, Taiwan", "Nairobi, Kenya", "Dubai, United Arab Emirates", "Guayaquil, Ecuador", "Makassar, Indonesia", "Jakarta, Indonesia", "Toronto, Canada", "Houston, UnitedStates", "Dar es Salaam, Tanzania", "Shenyang, China", "Zunyi, China", "Chengdu, China", "Dallas, United States", "Osaka, Japan", "Belgrade, Serbia", "T'bilisi, Georgia", "Minsk, Belarus", "Berlin, Germany", "Nizhny Novgorod, Russia", "Kaohsiung, Taiwan", "Nagoya, Japan", "Campinas, Brazil", "Chicago, UnitedStates", "Fuzhou, China", "Islamabad, Pakistan", "Bucharest, Romania", "Managua, Nicaragua", "Lahore, Pakistan", "Hefei, China", "Yangon, Myanmar", "Durban, South Africa", "Abu Dhabi, United Arab Emirates", "Salvador, Brazil", "San Antonio, United States", "Ahvaz, Iran", "Lusaka, Zambia", "Bogotá, Colombia", "Kathmandu, Nepal", "Maracaibo, Venezuela" };
            var cal = await DeviceCalendar.DeviceCalendar.GetDefaultCalendar();
            if (cal == null)
            {
                Tools.ShowMessage(Context, "Fehler", "Kein Kalender gefunden!");
                return;
            }
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

                xLog.Debug("save Event: " + e.Name);

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
        #endregion

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
                if (Drawer.IsDrawerOpen((int)GravityFlags.End))
                    ConfigBinder.PushDataToViewOnce();
            }
            catch (Exception ex) { sys.LogException(ex); }
        }

        bool bViewSpinnerActive = false;
        private void ViewTypeSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
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

        private void ViewTypeSpinner_Touch(object sender, View.TouchEventArgs e)
        {
            try
            {
                bViewSpinnerActive = true;
                var lst = new List<string>();
                lst.Add(ViewTypeSpinner.Prompt);
                lst.AddRange(Resources.GetStringArray(Resource.Array.calendar_viewtypes));
                ViewTypeAdapter.Clear();
                ViewTypeAdapter.AddAll(lst);
                ViewTypeAdapter.NotifyDataSetChanged();
                ViewTypeSpinner.PerformClick();
            }
            catch { }
        }

        private void ResetTitleSpinner(DateTime tFirstVisible, DateTime tLastVisible)
        {
            bViewSpinnerActive = false;
            if (ViewTypeSpinner != null)
            {
                string cTitle = tFirstVisible.ToString("MMM yyyy");
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
                {
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
                            cTitle = tFirstVisible.AddDays((tLastVisible - tFirstVisible).Days / 2).ToString("MMMM yyyy");
                            break;
                    }
                }

                ViewTypeSpinner.Prompt = cTitle;
                ViewTypeAdapter.Clear();
                ViewTypeAdapter.Add(cTitle);
                ViewTypeAdapter.NotifyDataSetChanged();

            }
        }

        bool isFABOpen = false;
        private void Fab_Click(object sender, EventArgs e)
        {
            if (!isFABOpen)
            {
                showFABMenu();
            }
            else
            {
                closeFABMenu();
            }
        }

        List<FloatingActionButton> fabs;

        private void showFABMenu()
        {
            isFABOpen = true;
            List<TimeType> menu = new List<TimeType>(new TimeType[] { TimeType.TimeZoneTime, TimeType.MiddleSunTime, TimeType.RealSunTime });
            menu.Remove(this.calEvents.timeType);

            int margin = (int)Resources.GetDimension(Resource.Dimension.fab_margin);
            var lp = new CoordinatorLayout.LayoutParams(CoordinatorLayout.LayoutParams.WrapContent, CoordinatorLayout.LayoutParams.WrapContent);
            lp.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
            lp.SetMargins(margin, margin, margin, margin);

            float fAnimate = Resources.GetDimension(Resource.Dimension.standard_60);

            fabs = new List<FloatingActionButton>();
            foreach (TimeType tt in menu)
            {
                var fab = new FloatingActionButton(mContext);
                fab.SetImageResource(MainWidgetBase.GetTimeTypeIcon(tt, LocationTimeHolder.LocalInstance));
                fab.Tag = new Java.Lang.String(tt.ToString());
                fab.Click += FabMenu_Click;
                coordinator.AddView(fab, lp);
                fabs.Add(fab);

                fab.Animate().TranslationY(-(fAnimate));
                fAnimate += Resources.GetDimension(Resource.Dimension.standard_60);
            }
            fabTimeType.BringToFront();
        }

        private void FabMenu_Click(object sender, EventArgs e)
        {
            try
            {
                string tag = (string)(sender as FloatingActionButton).Tag;
                var tt = Enum.Parse<TimeType>(tag);
                SetTimeType(tt);
            }
            finally
            {
                closeFABMenu();
            }
        }

        private void closeFABMenu()
        {
            isFABOpen = false;
            if (fabs == null)
                return;
            foreach (var fab in fabs)
            {
                fab.Animate().TranslationY(0).WithEndAction(new Java.Lang.Runnable(() => { coordinator.RemoveView(fab); }));
            }
        }

        public void LoadCalendarConfig()
        {
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
                var theme = Context.Theme;
                //Tools.GetAllThemeColors(theme);

                Color clTitleText = Color.White;
                Color clTitleBack = Color.ParseColor("#2c3e50");
                Color clText = clTitleText;
                Color clBack = Color.ParseColor("#2c3e50");
                Color clTodayText = clTitleText;
                Color clAccent = Color.ParseColor("#1B3147");

                Color clSlotBack = Tools.GetThemeColor(theme, Android.Resource.Attribute.WindowBackground).Value;
                Color clSlotAccent = Tools.GetThemeColor(theme, Android.Resource.Attribute.ColorSecondary).Value;

                schedule.HeaderStyle = new HeaderStyle { TextColor = clTitleText, BackgroundColor = clTitleBack };

                schedule.ViewHeaderStyle = new ViewHeaderStyle
                {
                    DayTextColor = clText,
                    DateTextColor = clText,
                    CurrentDateTextColor = clTodayText,
                    CurrentDayTextColor = clTodayText,
                    BackgroundColor = clBack
                };

                schedule.AppointmentStyle = new AppointmentStyle
                {
                    TextColor = Color.White,
                    BorderColor = clSlotAccent,
                    SelectionBorderColor = clText == Color.White ? Color.WhiteSmoke : clText,
                    SelectionTextColor = Color.White
                };

                schedule.SelectionStyle = new SelectionStyle
                {
                  //  BackgroundColor = clBack
                };

                schedule.TimelineViewSettings.Color = clSlotBack;
                schedule.TimelineViewSettings.LabelSettings.TimeLabelColor = clText;

                //schedule.DayViewSettings.TimeSlotColor = clSlotBack;
                //schedule.DayViewSettings.NonWorkingHoursTimeSlotColor = clSlotAccent; 
                //schedule.DayViewSettings.DayLabelSettings.TimeLabelColor = clText;

                schedule.WeekViewSettings.TimeSlotColor = clSlotBack;
                schedule.WeekViewSettings.NonWorkingHoursTimeSlotColor = clSlotAccent;
                schedule.WeekViewSettings.WeekLabelSettings.TimeLabelColor = clText;

                schedule.WorkWeekViewSettings.TimeSlotColor = clSlotBack;
                schedule.WorkWeekViewSettings.NonWorkingHoursTimeSlotColor = clSlotAccent;
                schedule.WorkWeekViewSettings.WorkWeekLabelSettings.TimeLabelColor = clText;

                schedule.MonthViewSettings.AgendaViewStyle = new AgendaViewStyle
                {
                    DateTextColor = clText,
                    TimeTextColor = clText,
                    SubjectTextColor = clText,
                    BackgroundColor = clSlotBack
                };

                schedule.MonthCellStyle = new MonthCellStyle
                {
                    TextColor = clText,
                    BackgroundColor = clSlotBack,
                    TodayTextColor = clTodayText,
                    TodayBackgroundColor = clBack,
                    PreviousMonthBackgroundColor = clSlotAccent,
                    NextMonthBackgroundColor = clSlotAccent
                };

                schedule.MonthViewSettings.WeekNumberStyle = new WeekNumberStyle
                {
                    TextColor = clText,
                    BackgroundColor = clBack
                };
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            try
            {
                var cfg = AppConfigHolder.CalendarViewConfig.SfScheduldeConfig;

                schedule.TimelineViewSettings.StartHour = cfg.TimeLineHourStart;
                schedule.TimelineViewSettings.EndHour = cfg.TimeLineHourEnd;
                schedule.TimelineViewSettings.DaysCount = cfg.TimeLineDaysCount;

                schedule.DayViewSettings.StartHour = cfg.DayViewHourStart;
                schedule.DayViewSettings.EndHour = cfg.DayViewHourEnd;
                schedule.DayViewSettings.WorkStartHour = cfg.DayViewWorkHourStart;
                schedule.DayViewSettings.WorkEndHour = cfg.DayViewWorkHourEnd;
                schedule.DayViewSettings.ShowAllDay = cfg.DayViewShowAllDay;

                schedule.WeekViewSettings.StartHour = cfg.WeekViewHourStart;
                schedule.WeekViewSettings.EndHour = cfg.WeekViewHourEnd;
                schedule.WeekViewSettings.WorkStartHour = cfg.WeekViewWorkHourStart ;
                schedule.WeekViewSettings.WorkEndHour = cfg.WeekViewWorkHourEnd;
                schedule.WeekViewSettings.ShowAllDay = cfg.WeekViewShowAllDay;

                schedule.WorkWeekViewSettings.StartHour = cfg.WorkWeekHourStart;
                schedule.WorkWeekViewSettings.EndHour = cfg.WorkWeekHourEnd;
                schedule.WorkWeekViewSettings.WorkStartHour = cfg.WorkWeekWorkHourStart;
                schedule.WorkWeekViewSettings.WorkEndHour = cfg.WorkWeekWorkHourEnd;
                schedule.WorkWeekViewSettings.ShowAllDay = cfg.WorkWeekShowAllDay;

                schedule.MonthViewSettings.ShowWeekNumber = cfg.MonthViewShowWeekNumber;
                schedule.MonthViewSettings.AppointmentDisplayMode = (AppointmentDisplayMode)Enum.ToObject(typeof(AppointmentDisplayMode), cfg.MonthViewAppointmentDisplayMode);
                schedule.MonthViewSettings.AppointmentIndicatorCount = cfg.MonthViewAppointmentIndicatorCount;
                schedule.MonthViewSettings.MonthNavigationDirection = (MonthNavigationDirections)Enum.ToObject(typeof(MonthNavigationDirections), cfg.MonthViewNavigationDirection);
                schedule.MonthViewSettings.ShowAppointmentsInline = cfg.MonthViewShowInlineEvents;
                schedule.MonthViewSettings.ShowAgendaView = !cfg.MonthViewShowInlineEvents && cfg.MonthViewShowAgenda;
            }
            catch (Exception ex)
            {
                xLog.Error(ex);
            }
        }
        public void SaveCalendarConfig()
        {
            AppConfigHolder.CalendarViewConfig.LastViewType = (int)schedule.ScheduleView;
            var cfg = AppConfigHolder.CalendarViewConfig.SfScheduldeConfig;

            cfg.TimeLineHourStart = (int)schedule.TimelineViewSettings.StartHour;
            cfg.TimeLineHourEnd = (int)schedule.TimelineViewSettings.EndHour;
            cfg.TimeLineDaysCount = schedule.TimelineViewSettings.DaysCount;

            cfg.DayViewHourStart = (int)schedule.DayViewSettings.StartHour;
            cfg.DayViewHourEnd = (int)schedule.DayViewSettings.EndHour;
            cfg.DayViewWorkHourStart = (int)schedule.DayViewSettings.WorkStartHour;
            cfg.DayViewWorkHourEnd = (int)schedule.DayViewSettings.WorkEndHour;
            cfg.DayViewShowAllDay = schedule.DayViewSettings.ShowAllDay;

            cfg.WeekViewHourStart = (int)schedule.WeekViewSettings.StartHour;
            cfg.WeekViewHourEnd = (int)schedule.WeekViewSettings.EndHour;
            cfg.WeekViewWorkHourStart = (int)schedule.WeekViewSettings.WorkStartHour;
            cfg.WeekViewWorkHourEnd = (int)schedule.WeekViewSettings.WorkEndHour;
            cfg.WeekViewShowAllDay = schedule.WeekViewSettings.ShowAllDay;

            cfg.WorkWeekHourStart = (int)schedule.WorkWeekViewSettings.StartHour;
            cfg.WorkWeekHourEnd = (int)schedule.WorkWeekViewSettings.EndHour;
            cfg.WorkWeekWorkHourStart = (int)schedule.WorkWeekViewSettings.WorkStartHour;
            cfg.WorkWeekWorkHourEnd = (int)schedule.WorkWeekViewSettings.WorkEndHour;
            cfg.WorkWeekShowAllDay = schedule.WorkWeekViewSettings.ShowAllDay;

            cfg.MonthViewShowWeekNumber = schedule.MonthViewSettings.ShowWeekNumber;
            cfg.MonthViewAppointmentDisplayMode = (int)schedule.MonthViewSettings.AppointmentDisplayMode;
            cfg.MonthViewAppointmentIndicatorCount = schedule.MonthViewSettings.AppointmentIndicatorCount;
            cfg.MonthViewNavigationDirection = (int)schedule.MonthViewSettings.MonthNavigationDirection;
            cfg.MonthViewShowInlineEvents = schedule.MonthViewSettings.ShowAppointmentsInline;
            cfg.MonthViewShowAgenda = schedule.MonthViewSettings.ShowAgendaView;

            AppConfigHolder.SaveCalendarViewConfig();
        }

        String cColorTree = "";
        public void SearchScheduleColors(object o, LinearLayout ll)
        {
            foreach (var prop in o.GetType().GetProperties())
            {
                try
                {
                    if (prop.PropertyType == typeof(Android.Graphics.Color))
                    {
                        xColor clr = ((Android.Graphics.Color)prop.GetValue(o)).ToColor();
                        EditText btn = new EditText(Context);
                        btn.SetBackgroundColor(clr.ToAndroid());
                        btn.SetTextColor(Color.Red);
                        btn.Text = prop.Name;
                        ll?.AddView(btn);
                        cColorTree += "\n\t" + prop.Name + "\t\t" + clr.HexString+"\tclr";

                        clr = clr.Luminosity < 0.5 ? xColor.HotPink : xColor.IndianRed;
                        if (prop.CanWrite)
                            prop.SetValue(o, clr.ToAndroid());
                    }
                    else if (prop.PropertyType == typeof(int) && prop.Name.ToLower().Contains("color"))
                    {
                        xColor clr = new Android.Graphics.Color((int)prop.GetValue(o)).ToColor();
                        Button btn = new Button(Context);
                        btn.SetBackgroundColor(clr.ToAndroid());
                        btn.Text = prop.Name;
                        btn.SetTextColor(Color.Red);
                        ll?.AddView(btn);
                        cColorTree += "\n\t" + prop.Name + "\t\t" + clr.HexString+"\tint";

                        clr = clr.Luminosity < 0.5 ? xColor.Aqua : xColor.Green;
                        if (prop.CanWrite)// && prop.Name.ToLower().Contains("back"))
                            prop.SetValue(o, clr.ToAndroid().ToArgb());
                    }
                }
                catch (Exception ex)
                {
                    xLog.Error(ex);
                }
            }
            foreach (var prop in o.GetType().GetProperties())
            {
                if (prop.Name.ToLower().Contains("settings") || prop.Name.ToLower().Contains("style"))
                {
                    cColorTree += "\n" + prop.Name;
                    ll?.AddView(new TextView(Context) { Text = prop.Name });
                    if (prop.GetValue(o) == null && prop.CanWrite)
                        prop.SetValue(o, Activator.CreateInstance(prop.PropertyType));
                    if (prop.GetValue(o) != null)
                        SearchScheduleColors(prop.GetValue(o), ll);
                }
            }
        }
    }
}