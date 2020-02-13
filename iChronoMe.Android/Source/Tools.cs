using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;

using iChronoMe.Core.Types;

using Java.Lang;

using Xamarin.Essentials;

using static Android.Content.Res.Resources;

namespace iChronoMe.Droid
{
    public static class Tools
    {
        public static void ShowToast(Context context, string text, bool bShowLong = false)
            => MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(context, text, bShowLong ? ToastLength.Long : ToastLength.Short).Show());
        public static void ShowToast(Context context, ICharSequence text, bool bShowLong = false)
            => MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(context, text, bShowLong ? ToastLength.Long : ToastLength.Short).Show());

        public static void ShowToast(Context context, int resId, bool bShowLong = false)
            => MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(context, resId, bShowLong ? ToastLength.Long : ToastLength.Short).Show());

        public static void ShowMessage(Context context, string title, string text)
            => new AlertDialog.Builder(context).
            SetTitle(title)
            .SetMessage(text)
            .SetPositiveButton(context.Resources.GetString(Resource.String.action_ok), (s, e) => { })
            .Create().Show();

        public static void ShowMessage(Context context, int title, int text)
            => new AlertDialog.Builder(context).
            SetTitle(title)
            .SetMessage(text)
            .SetPositiveButton(context.Resources.GetString(Resource.String.action_ok), (s, e) => { })
            .Create().Show();

        private static Task<bool> tskYnMsg { get { return tcsYnMsg == null ? Task.FromResult(false) : tcsYnMsg.Task; } }
        private static TaskCompletionSource<bool> tcsYnMsg = null;

        public static async Task<bool> ShowYesNoMessage(Context context, string title, string text)
        {
            tcsYnMsg = new TaskCompletionSource<bool>();

            var dlg = new AlertDialog.Builder(context).
            SetTitle(title)
            .SetMessage(text)
            .SetPositiveButton(context.Resources.GetString(Resource.String.action_yes), (s, e) => { tcsYnMsg.TrySetResult(true); })
            .SetNegativeButton(context.Resources.GetString(Resource.String.action_no), (s, e) => { tcsYnMsg.TrySetResult(false); })
            .SetOnCancelListener(new myDialogCancelListener<bool>(tcsYnMsg))
            .Create();

            dlg.Show();
            await tskYnMsg;
            return tskYnMsg.Result;
        }

        private static Task<int> tskScDlg { get { return tcsScDlg == null ? Task.FromResult(-1) : tcsScDlg.Task; } }
        private static TaskCompletionSource<int> tcsScDlg = null;

        public static async Task<int> ShowSingleChoiseDlg(Context context, string title, string[] items, bool bAllowAbort = true)
        {
            tcsScDlg = new TaskCompletionSource<int>();

            var builder = new AlertDialog.Builder(context).SetTitle(title);
            if (bAllowAbort)
                builder = builder.SetNegativeButton(context.Resources.GetString(Resource.String.action_yes), (s, e) => { tcsYnMsg.TrySetResult(false); });
            builder = builder.SetSingleChoiceItems(items, -1, new SingleChoiceClickListener(tcsScDlg));
            builder = builder.SetOnCancelListener(new myDialogCancelListener<int>(tcsScDlg));
            var dlg = builder.Create();

            dlg.Show();
            await tskScDlg;
            return tskScDlg.Result;
        }

        public static async Task<object> ShowSingleChoiseDlg(Context context, string title, object[] items, string textProperty, bool bAllowAbort = true)
        {
            tcsScDlg = new TaskCompletionSource<int>();

            List<string> textS = new List<string>();
            foreach (object x in items)
            {
                try
                {
                    if (x == null)
                        textS.Add("_empty_");
                    else
                        x.GetType().GetProperty(textProperty).GetValue(x).ToString();
                }
                catch
                {
                    textS.Add(string.Concat("_error_", x));
                }
            }

            var builder = new AlertDialog.Builder(context).SetTitle(title);
            if (bAllowAbort)
                builder = builder.SetNegativeButton(context.Resources.GetString(Resource.String.action_yes), (s, e) => { tcsYnMsg.TrySetResult(false); });
            builder = builder.SetSingleChoiceItems(textS.ToArray(), -1, new SingleChoiceClickListener(tcsScDlg));
            builder = builder.SetOnCancelListener(new myDialogCancelListener<int>(tcsScDlg));
            var dlg = builder.Create();

            dlg.Show();
            await tskScDlg;
            int iRes = tskScDlg.Result;
            if (iRes < 0)
                return null;
            else
                return items[iRes];
        }

        public static string GetAllThemeColors(Theme theme)
        {
            string cRes = "";

            foreach (var prop in typeof(Android.Resource.Attribute).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.IsLiteral && !prop.IsInitOnly)
                {
                    if (prop.FieldType == typeof(int))
                    {
                        var clr = GetThemeColor(theme, (int)prop.GetValue(null));
                        if (clr != null)
                        {
                            cRes += prop.Name + "\t" + clr.Value.ToColor().HexString + "\n";
                        }
                    }
                }
            }

            return cRes;
        }

        public static Color? GetThemeColor(Theme theme, int attrId)
        {
            try
            {
                TypedValue val = new TypedValue();
                theme.ResolveAttribute(attrId, val, true);


                if (val.Type >= DataType.FirstColorInt && val.Type <= DataType.LastColorInt)
                {
                    //simple colors
                    return new Color((int)val.Data);
                }
                else
                {
                    //android colorSet
                    TypedArray themeArray = theme.ObtainStyledAttributes(new int[] { (int)val.Data });
                    try
                    {
                        int index = 0;
                        int defaultColourValue = 0;
                        int aColour = themeArray.GetColor(index, defaultColourValue);
                        return new Color((int)aColour);
                    }
                    finally
                    {
                        // Calling recycle() is important. Especially if you use alot of TypedArrays
                        // http://stackoverflow.com/a/13805641/8524
                        themeArray.Recycle();
                    }
                }


            }
            catch { }
            return null;
        }

        private class myDialogCancelListener<T> : Java.Lang.Object, IDialogInterfaceOnCancelListener
        {
            TaskCompletionSource<T> Handler;

            public myDialogCancelListener(TaskCompletionSource<T> tcs)
            {
                Handler = tcs;
            }

            public void OnCancel(IDialogInterface dialog)
            {
                if (typeof(T) == typeof(bool))
                    (Handler as TaskCompletionSource<bool>).TrySetResult(false);
                else if (typeof(T) == typeof(int))
                    (Handler as TaskCompletionSource<int>).TrySetResult(-1);
                else if (typeof(string) == typeof(string))
                    (Handler as TaskCompletionSource<string>).TrySetResult(null);
                else if (typeof(object) == typeof(object))
                    (Handler as TaskCompletionSource<object>).TrySetResult(null);
                else
                    Handler.TrySetResult(default(T));

                dialog?.Dismiss();
            }

            protected override void Dispose(bool disposing)
            {
                Handler = null;
                base.Dispose(disposing);
            }
        }

        public class SingleChoiceClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
        {
            TaskCompletionSource<int> Handler;

            public SingleChoiceClickListener(TaskCompletionSource<int> tcs)
            {
                Handler = tcs;
            }

            public new void Dispose()
            {
                Handler = null;
                base.Dispose();
            }

            public void OnClick(IDialogInterface dialog, int which)
            {
                if (dialog != null)
                    dialog.Dismiss();
                Handler.TrySetResult(which);
            }
        }
    }
}