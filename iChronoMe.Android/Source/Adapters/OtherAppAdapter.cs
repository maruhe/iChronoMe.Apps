using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Widget;

namespace iChronoMe.Droid.Adapters
{
    public class OtherAppAdapter : BaseAdapter<ApplicationInfo>
    {
        List<ApplicationInfo> items { get; } = new List<ApplicationInfo>();
        Activity mContext;

        public OtherAppAdapter(Activity context)
        {
            mContext = context;
            List<string> packages = new List<string>();

            /* */
            PackageManager pm = context.PackageManager;

            Intent mainIntent = new Intent(Intent.ActionMain, null);
            mainIntent.AddCategory(Intent.CategoryLauncher);

            IList<ResolveInfo> appList = pm.QueryIntentActivities(mainIntent, PackageInfoFlags.Activities);
            //Collections.sort(appList, new ResolveInfo.DisplayNameComparator(pm));

            int count = 0;
            List<string> xx = new List<string>();
            foreach (ResolveInfo ri in appList)
            {
                try
                {
                    string cLabel = ri.ActivityInfo.LoadLabel(Application.Context.PackageManager);
                    if (string.IsNullOrEmpty(cLabel))
                        continue;
                    if (!packages.Contains(ri.ActivityInfo.PackageName))
                        packages.Add(ri.ActivityInfo.PackageName);
                }
                catch { }
            }
            /**/

            var apps = Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
            foreach (var app in apps)
            {
                try
                {
                    if (string.IsNullOrEmpty(app.Name))
                        continue;
                    string cLabel = app.LoadLabel(Application.Context.PackageManager);
                    if (string.IsNullOrEmpty(cLabel))
                        continue;
                    if (!packages.Contains(app.PackageName))
                        packages.Add(app.PackageName);
                }
                catch { }
            }

            SortedDictionary<string, ApplicationInfo> temp = new SortedDictionary<string, ApplicationInfo>();
            foreach (string package in packages)
            {
                try
                {
                    var info = pm.GetApplicationInfo(package, PackageInfoFlags.Activities);
                    string cLabel = info.LoadLabel(Application.Context.PackageManager);
                    while (temp.ContainsKey(cLabel))
                        cLabel += "Z";
                    temp.Add(cLabel, info);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }

            items = new List<ApplicationInfo>(temp.Values);
        }

        public override ApplicationInfo this[int position] => items[position];

        public override int Count => items.Count;

        public override long GetItemId(int position)
            => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            if (convertView == null)
            {
                convertView = mContext.LayoutInflater.Inflate(Resource.Layout.listitem_title, null);
            }

            string cLabel = item.LoadLabel(Application.Context.PackageManager);
            var icon = item.LoadIcon(Application.Context.PackageManager);

            convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(icon);
            convertView.FindViewById<TextView>(Resource.Id.title).Text = cLabel;

            return convertView;
        }
    }
}
