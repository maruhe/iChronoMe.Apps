using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using Net.ArcanaStudio.ColorPicker;
using Xamarin.Essentials;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [Activity(Label = "ActionButtonWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.ActionButton.ActionButtonWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class ActionButtonWidgetConfigActivity : BaseWidgetActivity
    {
        public int appWidgetId = -1;
        DynamicCalendarModel CalendarModel;
        Drawable wallpaperDrawable;
        AlertDialog pDlg;

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
                Toast.MakeText(this, "Fehlerhafte Parameter!", ToastLength.Long).Show();
                FinishAndRemoveTask();
                return;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (NeedsStartAssistant())
                ShowStartAssistant();
            else
            {
                var progressBar = new ProgressBar(this);
                progressBar.Indeterminate = true;
                pDlg = new AlertDialog.Builder(this)
                    .SetCancelable(false)
                    .SetTitle("Daten werden aufbereitet...")
                    .SetView(progressBar)
                    .Create();
                pDlg.Show();

                StartWidgetSelection();
            }
        }

        System.Drawing.Point wSize = new System.Drawing.Point(100, 100);

        void StartWidgetSelection()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Task.Delay(100).Wait();


                    if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                    {
                        RunOnUiThread(() => ShowExitMessage("Die Widget's funktionieren (aktuell) nur mit Standort-Zugriff!"));
                        return;
                    }

                    try
                    {
                        WallpaperManager wpMgr = WallpaperManager.GetInstance(this);
                        wallpaperDrawable = wpMgr.PeekDrawable();
                    }
                    catch (System.Exception ex)
                    {
                        ex.ToString();
                    }

                    if (wallpaperDrawable == null)
                        wallpaperDrawable = Resources.GetDrawable(Resource.Drawable.dummy_wallpaper, Theme);

                    CalendarModel = new CalendarModelCfgHolder().GetDefaultModelCfg();

                    RunOnUiThread(() =>
                    {
                        ShowWidgetTypeSelector();
                        pDlg.Dismiss();
                    });
                }
                catch (System.Exception ex)
                {
                    ShowExitMessage(ex.Message);
                }
            });
        }

        private void ShowExitMessage(string cMessage)
        {
            var alert = new AlertDialog.Builder(this)
               .SetMessage(cMessage)
               .SetCancelable(false);
            alert.SetPositiveButton("OK", (senderAlert, args) =>
            {
                (senderAlert as Dialog).Dismiss();
                FinishAndRemoveTask();
            });

            alert.Show();
        }

        private void ShowWidgetTypeSelector()
        {
            var list = new List<ActionButton_ClickAction>();
            foreach (var o in Enum.GetValues(typeof(ActionButton_ClickAction)))
                list.Add((ActionButton_ClickAction)o);
            var strings = new List<string>();
            foreach (var o in list)
                strings.Add(o.ToString());

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Aktion")
                .SetSingleChoiceItems(strings.ToArray(), -1, new AktionTypeOnClickListener(this, appWidgetId, list))
                .SetNegativeButton("abbrechen", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowWidgetStyleSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);

            var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.Style = ActionButton_Style.iChronoEye;
            cfg.AnimateOnFirstClick = false;
            listAdapter.Items.Add("theEye starr", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.Style = ActionButton_Style.iChronoEye;
            cfg.AnimateOnFirstClick = true;
            listAdapter.Items.Add("theEye animiert (Doppelklick = Aktion)", cfg as WidgetCfg_ActionButton);

            string cIconPrefix = "";
            switch (cfgTemplate.ClickAction)
            {
                case ActionButton_ClickAction.OpenApp:
                    //cIconPrefix = "icons8_calendar_plus";
                    break;
                case ActionButton_ClickAction.OpenCalendar:
                    cIconPrefix = "icons8_calendar";
                    break;
                case ActionButton_ClickAction.CreateEvent:
                    cIconPrefix = "icons8_calendar_plus_";
                    break;
                case ActionButton_ClickAction.CreateAlarm:
                    cIconPrefix = "icons8_alarm";
                    break;
                case ActionButton_ClickAction.TimeToTimeDialog:
                    cIconPrefix = "icons8_map_marker";
                    break;
            }
            if (!string.IsNullOrEmpty(cIconPrefix))
            {
                foreach (var prop in typeof(Resource.Drawable).GetFields())
                {
                    if (prop.Name.ToLower().StartsWith(cIconPrefix) && prop.Name.Length - 3 < cIconPrefix.Length)
                    {
                        cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                        cfg.Style = ActionButton_Style.Icon;
                        cfg.IconName = prop.Name;
                        listAdapter.Items.Add(prop.Name, cfg as WidgetCfg_ActionButton);
                    }
                }
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Design")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetStyleOnClickListener(this, appWidgetId, listAdapter))
                .SetNegativeButton("genung", new myCancelClickListener(this))
                .SetOnCancelListener(new myDialogCancelListener(this))
                .Create();
            dlg.Show();
        }

        public void ShowWidgeAfterStyleSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            if (cfgTemplate.Style == ActionButton_Style.iChronoEye && cfgTemplate.AnimateOnFirstClick)
            {
                var strings = new List<string>();
                for (int i = 1; i <= 5; i++)
                    strings.Add(i + " Sekunden");

                var dlg = new AlertDialog.Builder(this)
                    .SetTitle("Animationsdauer")
                    .SetSingleChoiceItems(strings.ToArray(), -1, new WidgetAnimationDuriationOnClickListener(this, cfgTemplate))
                    .SetNegativeButton("abbrechen", new myCancelClickListener(this))
                    .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                    .Create();
                dlg.Show();
            }
            else
                ShowTitleEditDialog(cfgTemplate);
        }

        public void ShowWidgeRoundCountSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            var strings = new List<string>();
            for (int i = 1; i <= (int)(cfgTemplate.AnimationDuriation*3); i++)
                strings.Add(i + " Umdrehungen");

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Animationsgeschwindigkeit")
                .SetSingleChoiceItems(strings.ToArray(), -1, new WidgetAnimationRoundCountOnClickListener(this, cfgTemplate))
                .SetNegativeButton("abbrechen", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        bool bIsIconColored = false;
        public void ShowTitleEditDialog(WidgetCfg_ActionButton cfgTemplate)
        {
            if (cfgTemplate.ClickAction == ActionButton_ClickAction.Animate)
            {
                FinishAndRemoveTask();
                return;
            }
            var titleDialog = new AlertDialog.Builder(this);
            EditText titleInput = new EditText(this) { };

            string selectedInput = string.Empty;
            titleInput.Text = cfgTemplate.WidgetTitle;
            //SetEditTextStylings(userInput);
            titleInput.InputType = Android.Text.InputTypes.TextFlagNoSuggestions;
            titleDialog.SetTitle("Der Text:");
            titleDialog.SetView(titleInput);
            titleDialog.SetPositiveButton(
                "Weiter",
                (see, ess) =>
                {
                    HideKeyboard(titleInput);
                    Task.Factory.StartNew(() =>
                    {
                        var holder = new WidgetConfigHolder();

                        var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();

                        cfg.WidgetId = appWidgetId;
                        holder.SetWidgetCfg(cfg);

                        Task.Delay(100).Wait();

                        UpdateWidget();

                        bIsIconColored = cfg.Style == ActionButton_Style.Icon && svg.IsIconColored(cfg.IconName); //needs Thread
                        RunOnUiThread(() =>
                        {
                            titleDialog.Dispose();
                            if (cfg.Style == ActionButton_Style.Icon)
                                ShowBackgroundStyleSelector(cfg);
                            else
                                ShowTextColorSelector(cfg);
                        });
                    });
                });
            titleDialog.SetOnCancelListener(new myDialogCancelListener(this));
            titleDialog.Show();
            ShowKeyboard(titleInput);
        }

        public void ShowBackgroundStyleSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);

            var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcTransparent;
            listAdapter.Items.Add("Transparent", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcBlack;
            cfg.ColorTitleText = cfg.IconColor = WidgetCfg.tcWhite;
            listAdapter.Items.Add("Black", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcWhite;
            cfg.ColorTitleText = cfg.IconColor = WidgetCfg.tcBlack;
            listAdapter.Items.Add("White", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcDark;
            cfg.ColorTitleText = cfg.IconColor = WidgetCfg.tcLight;
            listAdapter.Items.Add("Dark", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcLight;
            cfg.ColorTitleText = cfg.IconColor = WidgetCfg.tcDark;
            listAdapter.Items.Add("Light", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcLightGlass1;
            listAdapter.Items.Add("Halb-Transparent 1", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcLightGlass2;
            listAdapter.Items.Add("Halb-Transparent 2", cfg as WidgetCfg_ActionButton);

            cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            cfg.ColorBackground = WidgetCfg.tcLightGlass3;
            listAdapter.Items.Add("Halb-Transparent 3", cfg as WidgetCfg_ActionButton);

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Hintergrundtyp")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetBackgroundTypeOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", async (d, w) => {
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(true)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(true)
                        .SetDialogTitle("Hintergrundfarbe");

                    var clr = await clrDlg.ShowAsyncNullable(this);

                    cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                    if (clr.HasValue)
                    {
                        cfg.ColorBackground = clr.Value.ToColor();
                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();
                    }
                    ShowIconColorSelector(cfg);
                })
                .SetNegativeButton("genung", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        public void ShowBackgroundColorSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            if (cfgTemplate.ColorBackground.A == 0 || cfgTemplate.ColorBackground.A == 1)
            {
                ShowIconColorSelector(cfgTemplate);
            }
            else
            {
                var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);
                var nA = cfgTemplate.ColorBackground.A;

                var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorBackground = WidgetCfg.tcBlack.WithAlpha(nA);
                listAdapter.Items.Add("Black", cfg as WidgetCfg_ActionButton);

                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorBackground = WidgetCfg.tcWhite.WithAlpha(nA);
                listAdapter.Items.Add("White", cfg as WidgetCfg_ActionButton);

                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorBackground = WidgetCfg.tcDark.WithAlpha(nA);
                listAdapter.Items.Add("Dark", cfg as WidgetCfg_ActionButton);

                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorBackground = WidgetCfg.tcLight.WithAlpha(nA);
                listAdapter.Items.Add("Light", cfg as WidgetCfg_ActionButton);

                int i = 0;
                foreach (var clrs in DynamicColors.SampleColorSetS)
                {
                    i++;
                    var clr = xColor.FromHex(clrs[4]);
                    if (!listAdapter.Items.ContainsKey(clr.HexString))
                    {
                        cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                        cfg.ColorBackground = clr.WithAlpha(nA);
                        listAdapter.Items.Add(clr.HexString, cfg as WidgetCfg_ActionButton);
                    }
                }

                var dlg = new AlertDialog.Builder(this)
                    .SetTitle("Hintergrundfarbe")
                    .SetSingleChoiceItems(listAdapter, -1, new WidgetBackgroundColorOnClickListener(this, appWidgetId, listAdapter))
                    .SetPositiveButton("custom", async (d, w) => {
                        var clrDlg = ColorPickerDialog.NewBuilder()
                            .SetDialogType(ColorPickerDialog.DialogType.Preset)
                            .SetAllowCustom(true)
                            .SetShowColorShades(true)
                            .SetColorShape(ColorShape.Circle)
                            .SetShowAlphaSlider(true)
                            .SetDialogTitle("Hintergrundfarbe");

                        var clr = await clrDlg.ShowAsyncNullable(this);

                        cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                        if (clr.HasValue)
                        {
                            cfg.ColorBackground = clr.Value.ToColor();
                            cfg.WidgetId = appWidgetId;
                            new WidgetConfigHolder().SetWidgetCfg(cfg);
                            Task.Delay(100).Wait();
                            UpdateWidget();
                        }
                        ShowIconColorSelector(cfg);
                    })
                    .SetNegativeButton("genung", new myCancelClickListener(this))
                    .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                    .Create();
                dlg.Show();
            }
        }

        public void ShowIconColorSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            if (cfgTemplate.Style != ActionButton_Style.Icon || bIsIconColored)
            {
                ShowTextColorSelector(cfgTemplate);
                return;
            }

            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);

            var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            if (cfg.ColorBackground != WidgetCfg.tcBlack)
            {
                cfg.IconColor = WidgetCfg.tcBlack;
                listAdapter.Items.Add("Black", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcWhite)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.IconColor = WidgetCfg.tcWhite;
                listAdapter.Items.Add("White", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcDark)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.IconColor = WidgetCfg.tcDark;
                listAdapter.Items.Add("Dark", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcLight)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.IconColor = WidgetCfg.tcLight;
                listAdapter.Items.Add("Light", cfg as WidgetCfg_ActionButton);
            }

            int i = 0;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                i++;
                var clr = xColor.FromHex(clrs[2]);
                if (!listAdapter.Items.ContainsKey(clr.HexString) && cfg.ColorBackground != clr)
                {
                    cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                    cfg.IconColor = clr;
                    listAdapter.Items.Add(clr.HexString, cfg as WidgetCfg_ActionButton);
                }
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Symbol-Farbe")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetIconColorOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", async (d, w) => {
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(true)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(false)
                        .SetDialogTitle("Symbol-Farbe");

                    var clr = await clrDlg.ShowAsyncNullable(this);

                    cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                    if (clr.HasValue)
                    {
                        cfg.IconColor = clr.Value.ToColor();
                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();
                    }
                    ShowBackgroundStyleSelector(cfg);
                })
                .SetNegativeButton("genung", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        public void ShowTextColorSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);

            var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            if (cfg.Style == ActionButton_Style.Icon && !bIsIconColored)
            {
                cfg.ColorTitleText = cfg.IconColor;
                listAdapter.Items.Add("wie Symbol", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcBlack)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorTitleText = WidgetCfg.tcBlack;
                listAdapter.Items.Add("Black", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcWhite)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorTitleText = WidgetCfg.tcWhite;
                listAdapter.Items.Add("White", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcDark)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorTitleText = WidgetCfg.tcDark;
                listAdapter.Items.Add("Dark", cfg as WidgetCfg_ActionButton);
            }

            if (cfg.ColorBackground != WidgetCfg.tcLight)
            {
                cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                cfg.ColorTitleText = WidgetCfg.tcLight;
                listAdapter.Items.Add("Light", cfg as WidgetCfg_ActionButton);
            }

            int i = 0;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                i++;
                var clr = xColor.FromHex(clrs[2]);
                if (!listAdapter.Items.ContainsKey(clr.HexString) && cfg.ColorBackground != clr)
                {
                    cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                    cfg.ColorTitleText = clr;
                    listAdapter.Items.Add(clr.HexString, cfg as WidgetCfg_ActionButton);
                }
            }

            var dlg = new AlertDialog.Builder(this)
                .SetTitle("Text-Farbe")
                .SetSingleChoiceItems(listAdapter, -1, new WidgetTextColorOnClickListener(this, appWidgetId, listAdapter))
                .SetPositiveButton("custom", async (d, w) => {
                    var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(true)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(false)
                        .SetDialogTitle("Text-Farbe");

                    var clr = await clrDlg.ShowAsyncNullable(this);

                    cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
                    if (clr.HasValue)
                    {
                        cfg.IconColor = clr.Value.ToColor();
                        cfg.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(cfg);
                        Task.Delay(100).Wait();
                        UpdateWidget();
                    }
                    FinishAndRemoveTask();
                })
                .SetNegativeButton("genung", new myCancelClickListener(this))
                .SetOnCancelListener(new CancelToWidgetStyleSelectorSelectorListener(this, cfgTemplate))
                .Create();
            dlg.Show();
        }

        private void ShowKeyboard(EditText userInput)
        {
            try
            {
                userInput.RequestFocus();
                InputMethodManager imm = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                imm.ToggleSoftInput(ShowFlags.Forced, 0);
            }
            catch { }
        }

        private void HideKeyboard(EditText userInput)
        {
            try
            {
                InputMethodManager imm = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(userInput.WindowToken, 0);
            }
            catch { }
        }

        public void UpdateWidget()
        {
            Intent updateIntent = new Intent(this, typeof(ActionButtonWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            AppWidgetManager widgetManager = AppWidgetManager.GetInstance(this);
            int[] ids = new int[] { appWidgetId };
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            SendBroadcast(updateIntent);
        }
    }

    public class AktionTypeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        List<ActionButton_ClickAction> ListItems;

        public AktionTypeOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, List<ActionButton_ClickAction> items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            Intent resultValue = new Intent();
            resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, myActivity.appWidgetId);
            myActivity.SetResult(Result.Ok, resultValue);

            var holder = new WidgetConfigHolder();

            var cfg = new WidgetCfg_ActionButton();
            cfg.ClickAction = ListItems[which];
            var ca = ListItems[which];
            if (ca == ActionButton_ClickAction.Animate)
            {
                cfg.WidgetTitle = "";
                cfg.Style = ActionButton_Style.iChronoEye;
                cfg.AnimateOnFirstClick = true;
            }
            else if (ca != ActionButton_ClickAction.OpenApp)
                cfg.WidgetTitle = ca.ToString();

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            if (ca == ActionButton_ClickAction.Animate)
                myActivity.ShowWidgeAfterStyleSelector(cfg);
            else
                myActivity.ShowWidgetStyleSelector(cfg);
        }
    }

    public class WidgetStyleOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetStyleOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowWidgeAfterStyleSelector(cfg as WidgetCfg_ActionButton);
        }
    }

    public class WidgetAnimationDuriationOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        WidgetCfg_ActionButton Cfg;

        public WidgetAnimationDuriationOnClickListener(ActionButtonWidgetConfigActivity activity, WidgetCfg_ActionButton cfg)
        {
            myActivity = activity;
            Cfg = cfg;
        }

        public new void Dispose()
        {
            myActivity = null;
            Cfg = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = Cfg;

            cfg.WidgetId = myActivity.appWidgetId;
            cfg.AnimationDuriation = which + 1;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowWidgeRoundCountSelector(cfg);
        }
    }

    public class WidgetAnimationRoundCountOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        WidgetCfg_ActionButton Cfg;

        public WidgetAnimationRoundCountOnClickListener(ActionButtonWidgetConfigActivity activity, WidgetCfg_ActionButton cfg)
        {
            myActivity = activity;
            Cfg = cfg;
        }

        public new void Dispose()
        {
            myActivity = null;
            Cfg = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = Cfg;

            cfg.WidgetId = myActivity.appWidgetId;
            cfg.AnimationRounds = which + 1;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowTitleEditDialog(cfg);
        }
    }

    public class WidgetIconColorOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetIconColorOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowTextColorSelector((WidgetCfg_ActionButton)cfg);
        }
    }

    public class WidgetTextColorOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetTextColorOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.FinishAndRemoveTask();
        }
    }

    public class WidgetBackgroundTypeOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetBackgroundTypeOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowBackgroundColorSelector((WidgetCfg_ActionButton)cfg);
        }
    }

    public class WidgetBackgroundColorOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        int myWidgetId;
        WidgetPreviewListAdapter ListItems;

        public WidgetBackgroundColorOnClickListener(ActionButtonWidgetConfigActivity activity, int iWidgetId, WidgetPreviewListAdapter items)
        {
            myActivity = activity;
            myWidgetId = iWidgetId;
            ListItems = items;
        }

        public new void Dispose()
        {
            myActivity = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            var holder = new WidgetConfigHolder();

            var cfg = new List<WidgetCfg>(ListItems.Items.Values)[which];

            cfg.WidgetId = myActivity.appWidgetId;
            holder.SetWidgetCfg(cfg);

            Task.Delay(100).Wait();

            myActivity.UpdateWidget();
            if (dialog != null)
                dialog.Dismiss();
            myActivity.ShowIconColorSelector((WidgetCfg_ActionButton)cfg);
        }
    }

    public class myCancelClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        Activity myActivity;

        public myCancelClickListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            dialog?.Dismiss();
            myActivity.FinishAndRemoveTask();
        }

        protected override void Dispose(bool disposing)
        {
            myActivity = null;
            base.Dispose(disposing);
        }
    }

    public class myDialogCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        Activity myActivity;

        public myDialogCancelListener(Activity activity)
        {
            myActivity = activity;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            myActivity.FinishAndRemoveTask();
        }
    }

    public class CancelToWidgetStyleSelectorSelectorListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        ActionButtonWidgetConfigActivity myActivity;
        WidgetCfg_ActionButton Cfg;

        public CancelToWidgetStyleSelectorSelectorListener(ActionButtonWidgetConfigActivity activity, WidgetCfg_ActionButton cfg)
        {
            myActivity = activity;
            Cfg = cfg;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            myActivity.ShowWidgetStyleSelector(Cfg);
        }
    }
}