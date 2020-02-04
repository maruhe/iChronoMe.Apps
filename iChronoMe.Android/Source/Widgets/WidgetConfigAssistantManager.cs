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
using iChronoMe.Droid.GUI;
using iChronoMe.Droid.GUI.Dialogs;
using iChronoMe.Widgets;

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
        Drawable wallpaperDrawable = null;

        IWidgetConfigAssistant<T> currentAssi = null;
        public WidgetCfgSample<T> CurrentSample = null;

        public Task<bool> UserInputTaskTask { get { return tcsUI == null ? Task.FromResult(false) : tcsUI.Task; } }
        private TaskCompletionSource<bool> tcsUI = null;

        public WidgetConfigAssistantManager(AppCompatActivity context)
        {
            mContext = context;
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
                    break;
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

                var listAdapter = new WidgetPreviewListAdapter(mContext, wSize, CalendarModel, myEventsMonth, myEventsList, null);
                foreach (var sample in currentAssi.Samples)
                {
                    listAdapter.Items.Add(sample.Title, sample.WidgetConfig);
                }

                tcsUI = new TaskCompletionSource<bool>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var dlg = new AlertDialog.Builder(mContext)
                        .SetTitle(currentAssi.Title)
                        .SetSingleChoiceItems(listAdapter, -1, new SingleChoiceClickListener<T>(this, listAdapter))
                        .SetNegativeButton("abbrechen", new myNegativeButtonClickListener<T>(this))
                        .SetOnCancelListener(new myDialogCancelListener<T>(this))
                        .Create();
                    dlg.Show();
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

        public void TriggerSingleChoiceClicked(int which)
        {
            CurrentSample = currentAssi.Samples[which];
            Task.Factory.StartNew(() =>
            {
                currentAssi.AfterSelect(this, CurrentSample);
                tcsUI?.TrySetResult(true);
            });
        }

        public void TriggerNegativeButtonClicked()
        {
            CurrentSample = null;
            tcsUI?.TrySetResult(false);
        }
        public void TriggerDialogCanceled()
        {
            tcsUI?.TrySetResult(false);
        }

        public Task<SelectPositionResult> TriggerSelectMapsLocation()
        {
            return LocationPickerDialog.SelectLocation(mContext);
        }

        ProgressDialog pDlg = null;

        public void StartProgress(string cTitle)
        {
            pDlg = ProgressDialog.NewInstance(cTitle);
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
    }

    public class SingleChoiceClickListener<T> : Java.Lang.Object, IDialogInterfaceOnClickListener
        where T : WidgetCfg
    {
        WidgetConfigAssistantManager<T> mManager;
        WidgetPreviewListAdapter ListItems;

        public SingleChoiceClickListener(WidgetConfigAssistantManager<T> manager, WidgetPreviewListAdapter items)
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