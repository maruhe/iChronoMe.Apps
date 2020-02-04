using Android.App;
using Android.OS;

namespace iChronoMe.Droid
{
    [Activity(Label = "TestActivity")]
    public class TestActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
    }
}
/*
            var llColorList = new LinearLayout(this) { Orientation = Orientation.Vertical };
            int size = 20 * sys.DisplayDensity;
            foreach (var clrs in DynamicColors.SampleColorSetS)
            {
                var llColors = new LinearLayout(this) { Orientation = Orientation.Horizontal };
                var llText1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
                var llText2 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
                var llText3 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
                var llText4 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
                List<xColor> xclrs = new List<xColor>();
                int i = 0;
                foreach (string hex in clrs)
                {
                    llText1.AddView(new TextView(this) { Text = "  " + hex + "  " });

                    var clr = new xColor(hex);
                    xclrs.Add(clr);

                    if (i > 0)
                        llColors.AddView(new LinearLayout(this) { LayoutParameters = new LinearLayout.LayoutParams(size / 2, size) });
                    LinearLayout llClr = new LinearLayout(this);
                    llClr.LayoutParameters = new LinearLayout.LayoutParams(size, size);
                    llClr.SetBackgroundColor(clr.ToAndroid());


                    GradientDrawable shape = new GradientDrawable();
                    shape.SetShape(ShapeType.Rectangle);
                    shape.SetCornerRadii(new float[] { 2, 2, 2, 2, 2, 2, 2, 2 });
                    shape.SetColor(clr.ToAndroid());
                    shape.SetStroke(sys.DisplayDensity, Color.Black);
                    llClr.Background = shape;

                    llColors.AddView(llClr);
                    i++;
                }
                for (int iClr = 0; iClr < xclrs.Count-1; iClr++)
                {
                    Color clr1 = xclrs[iClr].ToAndroid();
                    Color clr2 = xclrs[iClr + 1].ToAndroid();
                    var nR = clr1.R - clr2.R;
                    if (nR < 0)
                        nR *= -1;
                    var nG = clr1.G - clr2.G;
                    if (nG < 0)
                        nG *= -1;
                    var nB = clr1.B - clr2.B;
                    if (nB < 0)
                        nB *= -1;
                    var d = Math.Sqrt(nR ^ 2 + nG ^ 2 + nB ^ 2);
                    var p = d / Math.Sqrt((255) ^ 2 + (255) ^ 2 + (255) ^ 2);
                    llText2.AddView(new TextView(this) { Text = "  " + d + "  " });
                    llText3.AddView(new TextView(this) { Text = "  " + p + "  " });

                    var nH1 = clr1.GetHue();
                    var nH2 = clr2.GetHue();

                    var nAvgHue = (nH1+nH2) / 2;
                    var nDist = Math.Abs(nH1 - nAvgHue);

                    var nH = nH1 - nH2;
                    if (nH < 0)
                        nH *= -1;
                    if (nH > 180)
                        nH = 360 - nH;
                    if (nH > 255)
                        nH.ToString();

                    llText4.AddView(new TextView(this) { Text = "  " + (int)nDist + "  " });
                }
                llColorList.AddView(llColors);
                //llColorList.AddView(llText1);
                //llColorList.AddView(llText2);
                //llColorList.AddView(llText3);
                llColorList.AddView(llText4);
            }

            var sv = new ScrollView(this);
            sv.AddView(llColorList);
            SetContentView(sv);

            return;
            new Thread(() =>
            {
                Looper.Prepare();
                for (int i = 0; i < 100; i++)
                {
                    Toast.MakeText(this, "icon " + i, ToastLength.Short).Show();
                    Bitmap bmp = ActionButtonService.GetIChronoEye(48, 48, 48, 24, 24, 15, -1, 366);
                    var sdCardPath = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).AbsolutePath, "iChronoMe");
                    Directory.CreateDirectory(sdCardPath);
                    var filePath = System.IO.Path.Combine(sdCardPath, "icon_" + DateTime.Now.TimeOfDay.TotalMilliseconds + ".png");
                    var stream = new FileStream(filePath, FileMode.OpenOrCreate);
                    bmp.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    stream.Close();
                }
            }).Start();
        }
    }
}
*/
