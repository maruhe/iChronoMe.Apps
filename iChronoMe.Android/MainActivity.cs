using System;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Views;

using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Calendar;

using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;

namespace iChronoMe.Droid
{
    [Activity(Label = "@string/app_name", Theme = "@style/splashscreen", MainLauncher = true)]
    public class MainActivity : BaseActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        int iNavigationItem = Resource.Id.nav_clock;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.activity_main);

            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(Toolbar);

            //FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            //fab.Click += FabOnClick;

            Drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, Drawer, Toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            Drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            NavigationView.SetNavigationItemSelectedListener(this);

            if (savedInstanceState != null)
            {
                iNavigationItem = savedInstanceState.GetInt("NavigationItem", iNavigationItem);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (NeedsStartAssistant())
            {
                ShowStartAssistant();
            }
            else if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
            }
            OnNavigationItemSelected(iNavigationItem);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            if (intent.Extras != null)
            {
                iNavigationItem = intent.GetIntExtra("NavigationItem", iNavigationItem);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("NavigationItem", iNavigationItem);
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        ActivityFragment frClock = null;
        ActivityFragment frCalendar = null;
        ActivityFragment frSettings = null;

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            return OnNavigationItemSelected(item.ItemId);
        }

        public bool OnNavigationItemSelected(int id)
        {
            ActivityFragment fr = null;

            if (id == Resource.Id.nav_clock)
            {
                if (frClock == null)
                    frClock = new ClockFragment();
                fr = frClock;
            }
            else if (id == Resource.Id.nav_calendar)
            {
                if (frCalendar == null)
                    frCalendar = new CalendarFragment();
                fr = frCalendar;

            }
            else if (id == Resource.Id.nav_share)
            {

            }

            iNavigationItem = id;

            if (fr != null)
            {
                ActiveFragment = fr;
                SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.main_frame, fr)
                .Commit();
            }

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);

            this.InvalidateOptionsMenu();

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            ActiveFragment?.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

