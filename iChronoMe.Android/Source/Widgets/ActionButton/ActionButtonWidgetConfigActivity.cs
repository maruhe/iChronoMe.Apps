using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Views.InputMethods;
using Android.Widget;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Types;
using iChronoMe.Widgets;
using Net.ArcanaStudio.ColorPicker;

namespace iChronoMe.Droid.Widgets.ActionButton
{
    [Activity(Label = "ActionButtonWidgetConfigActivity", Name = "me.ichrono.droid.Widgets.ActionButton.ActionButtonWidgetConfigActivity", Theme = "@style/TransparentTheme", LaunchMode = LaunchMode.SingleTask, TaskAffinity = "", NoHistory = true)]
    public class ActionButtonWidgetConfigActivity : BaseWidgetActivity
    {
        DynamicCalendarModel CalendarModel;
        AlertDialog pDlg;

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
                    .SetTitle(Resource.String.progress_preparing_data)
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

                    TryGetWallpaper();

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
        
        private void ShowWidgetTypeSelector()
        {
            if (sys.AllDrawables.Count == 0)
            {
                foreach (var prop in typeof(Resource.Drawable).GetFields())
                    sys.AllDrawables.Add(prop.Name);
            }

            var tStartAssistant = typeof(WidgetCfgAssistant_ActionButton_ClickAction);
            //if (holder.WidgetExists<WidgetCfg_ActionButton>(appWidgetId))
            //  tStartAssistant = typeof(WidgetCfgAssistant_ActionButton_OptionsBase);
            //var cfg = holder.GetWidgetCfg<WidgetCfg_ActionButton>(appWidgetId);
            var cfg = new WidgetCfg_ActionButton();
            var manager = new WidgetConfigAssistantManager<WidgetCfg_ActionButton>(this, wallpaperDrawable);
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var result = await manager.StartAt(tStartAssistant, cfg);
                    if (result != null)
                    {
                        result.WidgetConfig.WidgetId = appWidgetId;
                        new WidgetConfigHolder().SetWidgetCfg(result.WidgetConfig);

                        Intent resultValue = new Intent();
                        resultValue.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
                        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                        SetResult(Result.Ok, resultValue);

                        UpdateWidget();
                    }
                }
                catch (Exception ex)
                {
                    sys.LogException(ex);
                    RunOnUiThread(() => Toast.MakeText(this, ex.Message, ToastLength.Long).Show());
                }
                finally
                {
                    FinishAndRemoveTask();
                }
            });
        }
            
        public void löklöklöShowTextColorSelector(WidgetCfg_ActionButton cfgTemplate)
        {
            var listAdapter = new WidgetPreviewListAdapter(this, wSize, CalendarModel, null, null, wallpaperDrawable);

            var cfg = (WidgetCfg_ActionButton)cfgTemplate.Clone();
            if (cfg.Style == ActionButton_Style.Icon)
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
                .SetPositiveButton("custom", async (d, w) =>
                {
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

                .Create();
            dlg.Show();
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
}