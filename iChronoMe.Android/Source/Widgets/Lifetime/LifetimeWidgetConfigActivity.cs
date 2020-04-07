using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DataModels;
using iChronoMe.Widgets;

using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace iChronoMe.Droid.Widgets.Lifetime
{
    [Activity(Label = "WidgetConfig", Name = "me.ichrono.droid.Widgets.Lifetime.LifetimeWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]

    public class LifetimeWidgetConfigActivity : BaseWidgetActivity<WidgetCfg_Lifetime>
    {
        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
            {
                ShowStartAssistant();
                pDlg?.Dismiss();
            }
            else
            {
                var holder = new WidgetConfigHolder();
                var cfg = holder.GetWidgetCfg<WidgetCfg_Lifetime>(appWidgetId);

                //bearbeiten, wenn Widget bereits existiert
                if (!string.IsNullOrEmpty(cfg.WidgetTitle))
                {
                    /*SetTheme(Resource.Style.MainTheme);
                    SetContentView(Resource.Layout.formsmapper_layout);
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(100);

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            LoadConfigPage();
                        });
                    });*/
                    return;
                }

                SetTheme(Resource.Style.TransparentTheme);

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(100);

                    RunOnUiThread(() =>
                    {

                        pDlg?.Dismiss();
                        //gespeicherte Wesen vorschlagen
                        var cache = db.dbConfig.Query<Creature>("select * from Creature", new object[0]);
                        if (cache.Count > 0)
                        {
                            var list = new List<string>();
                            foreach (var o in cache)
                                list.Add(o.Name);
                            new AlertDialog.Builder(this)
                                .SetTitle("Wesen wählen")
                                .SetSingleChoiceItems(list.ToArray(), -1, new MyDialogInterfaceOnClickListener(this, appWidgetId, cache))
                                .SetPositiveButton("neu", (la, le) => { AskDataManual(); })
                                .SetNegativeButton("abbrechen", (senderAlert, args) =>
                                {
                                    Intent cancelResultValue = new Intent();
                                    cancelResultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                                    SetResult(Result.Canceled, cancelResultValue);
                                    Finish();
                                })
                                .SetOnCancelListener(new myCancelListener(this))
                                .Create().Show();
                            return;
                        }

                        AskDataManual();
                    });
                });
            }
        }

        private void AskDataManual()
        {
            //order Daten abfragen
            var holder = new WidgetConfigHolder();
            bool bIsNewWidget = !holder.WidgetExists(appWidgetId);
            var cfg = holder.GetWidgetCfg<WidgetCfg_Lifetime>(appWidgetId);

            var titleDialog = new AlertDialog.Builder(this);
            EditText titleInput = new EditText(this);

            string selectedInput = string.Empty;
            titleInput.Text = cfg.WidgetTitle;
            titleInput.Hint = "I";
            //SetEditTextStylings(userInput);
            titleInput.InputType = Android.Text.InputTypes.TextFlagNoSuggestions;
            titleDialog.SetTitle("Einen Namen oder Titel bitte:");
            titleDialog.SetView(titleInput);
            titleDialog.SetPositiveButton(
                "Weiter",
                (see, ess) =>
                {
                    string cTitle = titleInput.Text;
                    HideKeyboard(titleInput);

                    //und noch das Datum abfreage..
                    var dateDialog = new AlertDialog.Builder(this);
                    var dateInput = new Android.Widget.DatePicker(this);
                    dateInput.DateTime = cfg.LifeStartTime != DateTime.MinValue ? cfg.LifeStartTime : new DateTime(1980, 12, 25);

                    dateDialog.SetTitle("Beginn des Lebens war:");
                    dateDialog.SetView(dateInput);
                    dateDialog.SetPositiveButton(
                        "Weiter",
                        (dee, deee) =>
                        {
                            DateTime tDate = dateInput.DateTime;

                            //und die Uhrzeit
                            var timeDialog = new AlertDialog.Builder(this);
                            var timeInput = new Android.Widget.TimePicker(this);
                            timeInput.SetIs24HourView(Java.Lang.Boolean.True);
                            timeInput.Hour = cfg.LifeStartTime != DateTime.MinValue ? cfg.LifeStartTime.Hour : 12;
                            timeInput.Minute = cfg.LifeStartTime != DateTime.MinValue ? cfg.LifeStartTime.Minute : 0;

                            timeDialog.SetTitle("und die Uhrzeit:");
                            timeDialog.SetView(timeInput);
                            timeDialog.SetPositiveButton(
                                "Weiter",
                                (tee, teee) =>
                                {

                                    cfg.WidgetTitle = cTitle;
                                    cfg.LifeStartTime = tDate.Date + new TimeSpan(timeInput.Hour, timeInput.Minute, 0);
                                    holder.SetWidgetCfg(cfg);

                                    Task.Delay(100).Wait();

                                    Intent resultValue = new Intent();
                                    resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                                    resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                                    SetResult(Result.Ok, resultValue);

                                    Intent updateIntent = new Intent(this, typeof(LifetimeWidget));
                                    updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                                    AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
                                    int[] ids = widgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));
                                    updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
                                    SendBroadcast(updateIntent);

                                    if (bIsNewWidget)
                                        db.dbConfig.Insert(new Creature() { Name = cTitle, LifeStartTime = cfg.LifeStartTime });

                                    Finish();

                                });
                            timeDialog.SetNegativeButton("Abbrechen", (afk, kfa) => { Finish(); });
                            timeDialog.SetOnCancelListener(new myCancelListener(this));
                            timeDialog.Show();

                        });
                    dateDialog.SetNegativeButton("Abbrechen", (afk, kfa) => { Finish(); });
                    dateDialog.SetOnCancelListener(new myCancelListener(this));
                    dateDialog.Show();
                });
            titleDialog.SetNegativeButton("Abbrechen", (afk, kfa) => { HideKeyboard(titleInput); Finish(); });
            titleDialog.SetOnCancelListener(new myCancelListener(this));
            titleDialog.Show();
            ShowKeyboard(titleInput);
        }

        private void LoadConfigPage()
        {
            /*
            var mainPage = new AndroidWidgetConfig_LifetimePage(appWidgetId).CreateSupportFragment(this);
            SupportFragmentManager
            .BeginTransaction()
            .Replace(Resource.Id.fragment_frame_layout, mainPage)
            .Commit();

            FindViewById(Resource.Id.loading_panel).Visibility = Android.Views.ViewStates.Gone;

            AndroidWidgetConfig_LifetimePage.WidgetConfigIsDone = false;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1500);
                while (!AndroidWidgetConfig_LifetimePage.WidgetConfigIsDone)
                    await Task.Delay(100);

                Device.BeginInvokeOnMainThread(() =>
                {
                    Intent resultValue = new Intent();
                    resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                    resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                    SetResult(Result.Ok, resultValue);

                    Intent updateIntent = new Intent(this, typeof(LifetimeWidget));
                    updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                    AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
                    int[] ids = widgetManager.GetAppWidgetIds(new ComponentName(this, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));
                    updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
                    SendBroadcast(updateIntent);

                    Finish();
                });
            });
            */
        }

        protected override void OnStop()
        {
            base.OnStop();
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    public class MyDialogInterfaceOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        Activity myActivity;
        int myWidgetId;
        List<Creature> myCreatures;

        public MyDialogInterfaceOnClickListener(Activity activity, int iWidgetId, List<Creature> creatures)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            myCreatures = creatures;
        }

        public new void Dispose()
        {
            base.Dispose();
            myCreatures.Clear();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var beeing = myCreatures[which];

            var holder = new WidgetConfigHolder();
            var cfg = holder.GetWidgetCfg<WidgetCfg_Lifetime>(myWidgetId);

            cfg.WidgetTitle = beeing.Name;
            cfg.LifeStartTime = beeing.LifeStartTime;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            Intent resultValue = new Intent();
            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, myWidgetId);
            myActivity.SetResult(Result.Ok, resultValue);

            Intent updateIntent = new Intent(myActivity, typeof(LifetimeWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(myActivity);
            int[] ids = widgetManager.GetAppWidgetIds(new ComponentName(myActivity, Java.Lang.Class.FromType(typeof(LifetimeWidget)).Name));
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            myActivity.SendBroadcast(updateIntent);

            if (dialog != null)
                dialog.Dismiss();

            myActivity.Finish();

        }
    }

    public class myCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        Activity myActivity;

        public myCancelListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            myActivity.FinishAndRemoveTask();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            myActivity = null;
        }
    }
}