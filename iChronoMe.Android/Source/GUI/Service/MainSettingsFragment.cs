using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iChronoMe.Core.Classes;
using iChronoMe.Core.DataBinding;
using iChronoMe.Droid.GUI;

namespace iChronoMe.Droid.GUI.Service
{
    public class MainSettingsFragment : ActivityFragment
    {
        DataBinder binder;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            RootView = (ViewGroup)inflater.Inflate(Resource.Layout.fragment_setting_main, container, false);

            var cfg = AppConfigHolder.MainConfig;

            binder = new DataBinder(Activity, RootView);
            binder.BindViewProperty<bool>(Resource.Id.cb_showalways, nameof(CheckBox.Checked), cfg, nameof(MainConfig.AlwaysShowForegroundNotification), BindMode.TwoWay);
            binder.BindViewProperty<string>(Resource.Id.testedit1, nameof(TextView.Text), cfg, nameof(MainConfig.cTest1), BindMode.TwoWay);
            binder.BindViewProperty<string>(Resource.Id.testedit2, nameof(TextView.Text), cfg, nameof(MainConfig.cTest2), BindMode.TwoWay);
            binder.BindViewProperty<string>(Resource.Id.testedit3, nameof(TextView.Text), cfg, nameof(MainConfig.cTest3), BindMode.TwoWay);
            binder.BindViewProperty<string>(Resource.Id.testedit4, nameof(TextView.Text), cfg, nameof(MainConfig.cTest4), BindMode.TwoWay);

            return RootView;
        }

        public override void OnResume()
        {
            base.OnResume();
            
            binder.Start();

            Task.Factory.StartNew(() =>
            {
                var cfg = AppConfigHolder.MainConfig;

                Task.Delay(1500).Wait();
                cfg.cTest1 = "Change 1 adjsakjds";
                Task.Delay(500).Wait();
                cfg.cTest2 = "Change 2 adjsakjds";
                Task.Delay(1500).Wait();
                cfg.cTest3 = "Change 3 adjsakjds";

                Task.Delay(1500).Wait();
                cfg.cTest1 = "all";
                cfg.cTest2 = "all";
                cfg.cTest3 = "all";

                Task.Delay(500).Wait();
                cfg.cTest1 = "same";
                cfg.cTest2 = "same";
                cfg.cTest3 = "same";

                Task.Delay(500).Wait();
                cfg.cTest1 = "time";
                cfg.cTest2 = "time";
                cfg.cTest3 = "time";

                Task.Delay(1500).Wait();

                for (int i = 1; i < 100; i++)
                {
                    if (i % 2 == 0)
                        cfg.cTest2 = i.ToString();
                    else if (i % 3 == 0)
                        cfg.cTest3 = i.ToString();
                    else
                        cfg.cTest1 = i.ToString();

                    Task.Delay(75).Wait();
                }
            });
        }

        public override void OnPause()
        {
            base.OnPause();

            binder.Stop();
        }
    }
}