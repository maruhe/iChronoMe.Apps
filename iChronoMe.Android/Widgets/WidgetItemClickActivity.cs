using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Content;
using Android;
using System;
using Android.Content.PM;
using iChronoMe.Droid.Widgets.Calendar;

namespace iChronoMe.Droid.Widgets
{
    [Activity(Label = "WidgetItemClickActivity", Name = "me.ichrono.droid.Widgets.WidgetItemClickActivity", Theme = "@style/TransparentTheme", NoHistory = true)]
    public class WidgetItemClickActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                Bundle extras = Intent.Extras;
                string cClickCommand = extras.GetString("_ClickCommand");

                Intent iCommand = null;
                switch (cClickCommand)
                {
                    case "ActionView":
                        iCommand = new Intent(Intent.ActionView);
                        if (extras.GetBoolean("_NoHistory"))
                            iCommand.SetFlags(ActivityFlags.NoHistory);
                        break;

                    case "CheckCalendarWidget":
                        if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteCalendar) != Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                        {
                            ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadCalendar, Manifest.Permission.WriteCalendar, Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 2);
                            return;
                        }
                        else if (extras.ContainsKey("_ConfigComponent"))
                        {
                            iCommand = new Intent(Intent.ActionMain);
                            iCommand.SetComponent(ComponentName.UnflattenFromString(extras.GetString("_ConfigComponent")));
                            if (extras.GetBoolean("_NoHistory"))
                                iCommand.SetFlags(ActivityFlags.NoHistory);
                        }
                        break;

                    case "StartActivityByComponentName":
                        iCommand = new Intent(Intent.ActionMain);
                        iCommand.SetComponent(ComponentName.UnflattenFromString(extras.GetString("_ComponentName")));
                        if (extras.GetBoolean("_NoHistory"))
                            iCommand.SetFlags(ActivityFlags.NoHistory);
                        break;

                }

                if (iCommand != null)
                {
                    iCommand.PutExtras(extras);
                    iCommand.SetData(Intent.Data);
                    StartActivity(iCommand);
                }

            } catch (Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
            Finish();
        }

        int iGotPermissionResult = 0;
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            iGotPermissionResult++;
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            CalendarWidget.updateWidgets(this);
            if (iGotPermissionResult >= requestCode)
                Finish();
        }
    }
}