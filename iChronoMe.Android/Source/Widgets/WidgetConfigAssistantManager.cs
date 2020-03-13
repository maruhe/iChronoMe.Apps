using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V7.App;

using iChronoMe.Core.Classes;
using iChronoMe.Core.DynamicCalendar;
using iChronoMe.Core.Interfaces;
using iChronoMe.Core.Types;
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Widgets;

using Net.ArcanaStudio.ColorPicker;

using Xamarin.Essentials;

namespace iChronoMe.Droid.Widgets
{
    public class WidgetConfigAssistantManager<T> : IUserIO
        where T : WidgetCfg
    {
        AppCompatActivity mContext;
        Point wSize = new Point(400, 300);
        DynamicCalendarModel CalendarModel = null;
        EventCollection myEventsMonth = null;
        EventCollection myEventsList = null;
        Drawable WallpaperDrawable = null;

        IWidgetConfigAssistant<T> currentAssi = null;
        public WidgetCfgSample<T> CurrentSample = null;

        public Task<bool> UserInputTaskTask { get { return tcsUI == null ? Task.FromResult(false) : tcsUI.Task; } }
        private TaskCompletionSource<bool> tcsUI = null;

        public WidgetConfigAssistantManager(AppCompatActivity context, Drawable wallpaperDrawable)
        {
            mContext = context;
            CalendarModel = CalendarModelCfgHolder.BaseGregorian;
            WallpaperDrawable = wallpaperDrawable;
            init();
        }

        public WidgetConfigAssistantManager(AppCompatActivity context, DynamicCalendarModel calendarModel, EventCollection eventsList, EventCollection eventsListMonth, Drawable wallpaperDrawable)
        {
            mContext = context;
            CalendarModel = calendarModel;
            myEventsList = eventsList;
            myEventsMonth = eventsListMonth;
            WallpaperDrawable = wallpaperDrawable;
            init();
        }

        private void init()
        {
            if (typeof(T) == typeof(WidgetCfg_ActionButton))
                wSize = new Point(100, 100);
        }

        public async Task<WidgetCfgSample<T>> StartAt(Type widgetConfigAssistantType, T baseConfig, List<Type> stopAt = null)
        {
            return await StartAt(widgetConfigAssistantType, new WidgetCfgSample<T>("_start_", baseConfig), stopAt);
        }

        public async Task<WidgetCfgSample<T>> StartAt(Type widgetConfigAssistantType, WidgetCfgSample<T> baseSample, List<Type> stopAt = null)
        {
            var assiType = widgetConfigAssistantType;
            var sample = baseSample;
            while (assiType != null && (stopAt == null || !stopAt.Contains(assiType)))
            {
                sample = await PerformOne(assiType, sample);
                if (sample == null)
                    return null;
                if (currentAssi == null)
                    break;
                assiType = currentAssi.NextStepAssistantType;
            }

            return CurrentSample;
        }

        public async Task<WidgetCfgSample<T>> PerformOne(Type widgetConfigAssistantType, T baseConfig)
        {
            return await PerformOne(widgetConfigAssistantType, new WidgetCfgSample<T>("_base_", baseConfig));
        }

        public async Task<WidgetCfgSample<T>> PerformOne(Type widgetConfigAssistantType, WidgetCfgSample<T> baseSample)
        {
            currentAssi = (IWidgetConfigAssistant<T>)Activator.CreateInstance(widgetConfigAssistantType, new object[] { baseSample });
            bool bConfirmed = false;

            await Task.Factory.StartNew(() =>
            {
                currentAssi.PerformPreperation(this);

                if (currentAssi.Samples.Count == 0)
                {
                    return;
                }

                var listAdapter = new WidgetPreviewListAdapter<T>(mContext, currentAssi, wSize, WallpaperDrawable, CalendarModel, myEventsMonth, myEventsList);

                tcsUI = new TaskCompletionSource<bool>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var dlg = new AlertDialog.Builder(mContext)
                        .SetTitle(currentAssi.Title)
                        .SetNegativeButton("abbrechen", new myNegativeButtonClickListener<T>(this))
                        .SetOnCancelListener(new myDialogCancelListener<T>(this))
                        .SetSingleChoiceItems(listAdapter, -1, new SingleChoiceClickListener<T>(this, listAdapter));

                    if (currentAssi.AllowCustom)
                        dlg.SetNeutralButton(currentAssi.CurstumButtonText, DlgCustomButtonClick);
                    dlg.Create().Show();
                });

                UserInputTaskTask.Wait();
                bConfirmed = UserInputTaskTask.Result;
            });

            if (pDlg != null)
                pDlg.SetProgressDone();

            if (bConfirmed)
                return CurrentSample;
            else
                return null;
        }

        public void DlgCustomButtonClick(object sender, DialogClickEventArgs e)
        {
            (sender as IDialogInterface).Dismiss();
            Task.Factory.StartNew(() =>
            {
                currentAssi.ExecCustom(this);
            });
        }

        public void TriggerSingleChoiceClicked(int which)
        {
            CurrentSample = currentAssi.Samples[which];
            Task.Factory.StartNew(() =>
            {
                currentAssi.AfterSelect(this, CurrentSample);
                tcsUI?.TrySetResult(true);
            });
        }

        public void TriggerPositiveButtonClicked()
        {
            CurrentSample = currentAssi.BaseSample;
            tcsUI?.TrySetResult(true);
        }

        public void TriggerNegativeButtonClicked()
        {
            tcsUI?.TrySetResult(false);
        }

        public void TriggerDialogCanceled()
        {
            tcsUI?.TrySetResult(false);
        }

        public void TriggerAbortProzess()
        {
            CurrentSample = null;
            currentAssi = null;
            tcsUI?.TrySetResult(false);
        }

        public Task<SelectPositionResult> UserSelectMapsLocation(Location center = null, Location marker = null)
        {
            return LocationPickerDialog.SelectLocation(mContext, center, marker);
        }

        ProgressDlg pDlg = null;

        public void StartProgress(string cTitle)
        {
            pDlg = ProgressDlg.NewInstance(cTitle);
            pDlg.Show(mContext.SupportFragmentManager, "progress_widget_cfg_assi_mgr");
        }

        public void SetProgress(int progress, int max, string cMessage)
        {
            if (pDlg == null)
                StartProgress("just a moment...");
            pDlg.SetProgress(progress, max, cMessage);
        }

        public void SetProgressDone()
        {
            if (pDlg != null)
                pDlg.SetProgressDone();
        }

        public void ShowToast(string cMessage)
        {
            Tools.ShowToast(mContext, cMessage, true);
        }

        public void ShowError(string cMessage)
        {
            if (pDlg != null)
                pDlg.SetProgressDone();
            Task.Factory.StartNew(() =>
            {
                Task.Delay(250).Wait();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    new AlertDialog.Builder(mContext).SetTitle("Error").SetMessage(cMessage).Create().Show();
                });
            });
        }

        public Task<bool> UserShowYesNoMessage(string title, string message, string yes = null, string no = null)
        {
            return Tools.ShowYesNoMessage(mContext, title, message, yes, no);
        }

        public Task<bool> UserShowYesNoMessage(int title, int message, int? yes = null, int? no = null)
        {
            return Tools.ShowYesNoMessage(mContext, title, message, yes, no);
        }

        public Task<xColor?> UserSelectColor(int title, xColor? current = null, xColor[] colors = null, bool allowCustom = true, bool allowAlpha = true)
        {
            return UserSelectColor(mContext.Resources.GetString(title), current, colors, allowCustom, allowAlpha);
        }

        public async Task<xColor?> UserSelectColor(string title, xColor? current = null, xColor[] colors = null, bool allowCustom = true, bool allowAlpha = true)
        {
            var clrDlg = ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Preset)
                        .SetAllowCustom(allowCustom)
                        .SetShowColorShades(true)
                        .SetColorShape(ColorShape.Circle)
                        .SetShowAlphaSlider(allowAlpha)
                        .SetDialogTitle(title);
            if (current.HasValue)
                clrDlg.SetColor(current.Value.ToAndroid());

            var clr = await clrDlg.ShowAsyncNullable(mContext);
            if (clr.HasValue)
                return clr.Value.ToColor();
            return null;
        }

        public Task<string> UserInputText(string title, string message, string placeholder)
        {
            return Tools.UserInputText(mContext, title, message, placeholder);
        }
    }

    public class SingleChoiceClickListener<T> : Java.Lang.Object, IDialogInterfaceOnClickListener
        where T : WidgetCfg
    {
        WidgetConfigAssistantManager<T> mManager;
        WidgetPreviewListAdapter<T> ListItems;

        public SingleChoiceClickListener(WidgetConfigAssistantManager<T> manager, WidgetPreviewListAdapter<T> items)
        {
            mManager = manager;
            ListItems = items;
        }

        public new void Dispose()
        {
            mManager = null;
            ListItems = null;
            base.Dispose();
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            if (dialog != null)
                dialog.Dismiss();
            mManager.TriggerSingleChoiceClicked(which);
        }
    }

    public class myNegativeButtonClickListener<T> : Java.Lang.Object, IDialogInterfaceOnClickListener
        where T : WidgetCfg
    {
        WidgetConfigAssistantManager<T> mManager;

        public myNegativeButtonClickListener(WidgetConfigAssistantManager<T> manager)
        {
            mManager = manager;
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            dialog?.Dismiss();
            mManager.TriggerNegativeButtonClicked();
        }

        protected override void Dispose(bool disposing)
        {
            mManager = null;
            base.Dispose(disposing);
        }
    }

    public class myDialogCancelListener<T> : Java.Lang.Object, IDialogInterfaceOnCancelListener
        where T : WidgetCfg
    {
        WidgetConfigAssistantManager<T> mManager;

        public myDialogCancelListener(WidgetConfigAssistantManager<T> manager)
        {
            mManager = manager;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            dialog?.Dismiss();
            mManager.TriggerDialogCanceled();
        }

        protected override void Dispose(bool disposing)
        {
            mManager = null;
            base.Dispose(disposing);
        }
    }
}