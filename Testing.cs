using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Math;
using AForge;

namespace ColorTest
{
    class Testing
    {
        private static IntRange filterHue;
        private static Range filterSat;
        private static Range filterLig;

        private static HSL fillColor;

        private static Bitmap img;
        
        public static void inkFilter()
        {
            HSLFiltering filter = new HSLFiltering();
            filter.FillOutsideRange = false;
            filter.FillColor = fillColor;
            filter.Hue = filterHue;
            filter.Saturation = filterSat;
            filter.Luminance = filterLig;
            filter.ApplyInPlace(img);
        }
        public static void inkFinder()
        {
            Image<Hls, Byte> wrk = new Image<Hls, Byte>(img);
            Image<Gray, Byte>[] channels = wrk.Split();
            Image<Gray, Byte> imgHue = channels[0];
            Image<Gray, Byte> imgLig = channels[1];
            Image<Gray, Byte> imgSat = channels[2];
            int bl = 0;
            int wi = 0;
            int col = 0;
            ArrayList white = new ArrayList(); // HSL for white ones
            //ArrayList black = new ArrayList(); // HSL for black ones
            ArrayList other = new ArrayList();
            //
            // collect
            for (int i = 0; i < wrk.Height; i++)
            {
                for (int j = 0; j < wrk.Width; j++)
                {
                    int hue = (int)(imgHue[i, j].Intensity * 2);
                    float sat = (float)(imgSat[i, j].Intensity/255);
                    float lig = (float)(imgLig[i, j].Intensity/255);
                    //double border = -Math.Sqrt((1 - (lig - 0.5f) * (lig - 0.5f) / 0.34 / 0.34) * 0.84 * 0.84) + 1;
                    if (lig < 0.275 - 0.125 * sat || lig < 0.5 - 0.5 * sat / 0.6 || (sat < 0.18 && lig < 0.5))
                    {
                        //black.Add(new HSL(hue, sat, lig));
                        bl++;
                        continue;
                    }
                    if (lig > 0.725 + 0.125 * sat || lig > 0.5 + 0.5 * sat / 0.6 || (sat < 0.18 && lig >= 0.5))
                    {
                        white.Add(new HSL(hue, sat, lig));
                        wi++;
                        continue;
                    }
                    col++;
                    addToOther(other, 5, hue, sat, lig);
                }
                Console.WriteLine("Row - " + i + " : clusters - " + other.Count);
            }
            Console.WriteLine(bl + " " + wi + " " + col);
            Console.WriteLine(other.Count);
            ArrayList majCol = (ArrayList)other[0];
            //
            // find major color
            for (int t = 0; t < other.Count; t++)
            {
                if (majCol.Count < ((ArrayList)other[t]).Count)
                {
                    majCol = (ArrayList)other[t];
                }
            }
            //
            // find ranges
            HSL firstCol = (HSL)majCol[0];
            filterHue = new IntRange(firstCol.Hue, firstCol.Hue);
            filterLig = new Range(firstCol.Luminance, firstCol.Luminance);
            filterSat = new Range(firstCol.Saturation, firstCol.Saturation);
            for (int q = 0; q < majCol.Count; q++)
            {
                HSL nextCol = (HSL)majCol[q];
                if (nextCol.Hue < filterHue.Min) filterHue.Min = nextCol.Hue;
                if (nextCol.Hue > filterHue.Max) filterHue.Max = nextCol.Hue;
                if (nextCol.Luminance < filterLig.Min) filterLig.Min = nextCol.Luminance;
                if (nextCol.Luminance > filterLig.Max) filterLig.Max = nextCol.Luminance;
                if (nextCol.Saturation < filterSat.Min) filterSat.Min = nextCol.Saturation;
                if (nextCol.Saturation > filterSat.Max) filterSat.Max = nextCol.Saturation;
            }
            //
            // find fill color
            int huesum = 0;
            float satsum = 0;
            float ligsum = 0;
            for (int p = 0; p < white.Count; p++)
            {
                HSL wh = (HSL)white[p];
                huesum += wh.Hue;
                satsum += wh.Saturation;
                ligsum += wh.Luminance;
            }
            fillColor = new HSL(huesum / white.Count, satsum / white.Count, ligsum / white.Count);
        }
        private static void addToOther(ArrayList other, int r, int hue, float sat, float lig)
        {
            for (int l = 0; l < other.Count; l++)
            {
                for (int k = 0; k < ((ArrayList)other[l]).Count; k++)
                {
                    if (((HSL)((ArrayList)other[l])[k]).Hue - r < hue &&
                        ((HSL)((ArrayList)other[l])[k]).Hue + r > hue)
                    {
                        ((ArrayList)other[l]).Add(new HSL(hue, sat, lig));
                        return;
                    }
                }
            }
            ArrayList nextOne = new ArrayList();
            nextOne.Add(new HSL(hue, sat, lig));
            other.Add(nextOne);
        }
        static void Main(string[] args)
        {
            DateTime start = DateTime.Now;
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine((i + 1) + " pic");
                string name = args[i] + ".jpg";
                img = new Bitmap(name);
                inkFinder();
                //img.Save("P" + args[i] + ".jpg", ImageFormat.Jpeg);
                inkFilter();
                img.Save("F" + args[i] + ".jpg", ImageFormat.Jpeg);
            }
            Console.WriteLine(DateTime.Now.Subtract(start).TotalMilliseconds.ToString() + "ms");
            CvInvoke.cvShowImage("new", new Image<Bgr, Byte>(img));
            CvInvoke.cvWaitKey(0);
            Console.ReadLine();
        }
    }
}
