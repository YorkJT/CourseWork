using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace CourseWorkDouble2Try2
{
    public partial class Form1 : Form
    {
        public static double GetGray(Bitmap someImg, int i, int j)
        {
            return (double)someImg.GetPixel(i, j).R * 0.2126 + (double)someImg.GetPixel(i, j).G * 0.7152 + (double)someImg.GetPixel(i, j).B * 0.0722;
        }

        public static double tresholdOtsu = 0;
        public static void CalcOtsu(Bitmap someImg)
        {
            int up = (1 << 8), total = someImg.Height * someImg.Width;
            double varMax = 0;
            double[] hist = new double[up + 1];
            for (int i = 0; i < someImg.Width; ++i)
            {
                for (int j = 0; j < someImg.Height; ++j)
                {
                    if (someImg.GetPixel(i, j).A == 0)
                    {
                        someImg.SetPixel(i, j, Color.White);
                    }
                    double gray = GetGray(someImg, i, j);
                    ++hist[(int)Math.Round(gray, 0)];
                }
            }
            double t1 = 0, t2 = 0;
            double sum = 0, sumB = 0, mB = 0, mF = 0, wB = 0, wF = 0, varBetween = 0, eps = 1e-10;
            for (int t = 0; t < up; t++)
            {
                sum += (double) t * hist[t];
            }

            for (int t = 0; t < up; ++t)
            {
                wB += hist[t];
                if (wB == 0) continue;

                wF = total - wB;
                if (wF == 0) break;

                sumB += (double) t * hist[t];
                mB = sumB / wB;
                mF = (sum - sumB) / wF;

                varBetween = wB * wF * (mB - mF) * (mB - mF);

                if (varBetween >= varMax + eps)
                {
                    t1 = t;
                    if (varBetween > varMax + eps)
                    {
                        t2 = t;
                    }
                    varMax = varBetween;
                }
            }
            tresholdOtsu = (t1 + t2) / 2.0;
            return;
        }

        public static void CreateBW(Bitmap src, string path)
        {
            Bitmap bwSrc = new Bitmap(src);
            for (int i = 0; i < src.Width; ++i)
            {
                for (int j = 0; j < src.Height; ++j)
                {
                    if (GetGray(src, i, j) < tresholdOtsu)
                    {
                        bwSrc.SetPixel(i, j, Color.Black);
                    }
                    else
                    {
                        bwSrc.SetPixel(i, j, Color.White);
                    }
                }
            }
            bwSrc.Save(path + @"bw.png");
            return;
        }

        public static void CreateG(Bitmap src, string path)
        {
            Bitmap bwSrc = new Bitmap(src);
            for (int i = 0; i < src.Width; ++i)
            {
                for (int j = 0; j < src.Height; ++j)
                {
                    int g = (int)Math.Round(GetGray(src, i, j));
                    bwSrc.SetPixel(i, j, Color.FromArgb(g, g, g));
                }
            }
            bwSrc.Save(path + @"g.png");
            return;
        }

        public static double[] NormalEquations2d(double[] y, double[] x)
        {
            //x^t * x
            double[,] xtx = new double[2, 2];
            for (int i = 0; i < x.Length; i++)
            {
                xtx[0, 1] += x[i];
                xtx[0, 0] += x[i] * x[i];
            }
            xtx[1, 0] = xtx[0, 1];
            xtx[1, 1] = x.Length;

            //inverse
            double[,] xtxInv = new double[2, 2];
            double d = 1 / (xtx[0, 0] * xtx[1, 1] - xtx[1, 0] * xtx[0, 1]);
            xtxInv[0, 0] = xtx[1, 1] * d;
            xtxInv[0, 1] = -xtx[0, 1] * d;
            xtxInv[1, 0] = -xtx[1, 0] * d;
            xtxInv[1, 1] = xtx[0, 0] * d;

            //times x^t
            double[,] xtxInvxt = new double[2, x.Length];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < x.Length; j++)
                {
                    xtxInvxt[i, j] = xtxInv[i, 0] * x[j] + xtxInv[i, 1];
                }
            }

            //times y
            double[] theta = new double[2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < x.Length; j++)
                {
                    theta[i] += xtxInvxt[i, j] * y[j];
                }
            }

            return theta;
        }

        public static IDictionary<double, double> BowCountingDimension(Bitmap bw, int startSize, int finishSize, int step = 1, string dataPath = "")
        {
            IDictionary<double, double> baList = new Dictionary<double, double>();

            for (int b = startSize; b <= finishSize; b += step)
            {
                int hCount = bw.Height / b;
                int wCount = bw.Width / b;
                bool[,] filledBoxes =
                    new bool[wCount + (bw.Width > wCount * b ? 1 : 0), hCount + (bw.Height > hCount * b ? 1 : 0)];

                for (int x = 0; x < bw.Width; ++x)
                {
                    for (int y = 0; y < bw.Height; ++y)
                    {
                        if (GetGray(bw, x, y) < tresholdOtsu)
                        {
                            int xBox = x / b;
                            int yBox = y / b;
                            filledBoxes[xBox, yBox] = true;
                        }
                    }
                }

                int a = 0;
                for (int i = 0; i < filledBoxes.GetLength(0); i++)
                {
                    for (int j = 0; j < filledBoxes.GetLength(1); j++)
                    {
                        if (filledBoxes[i, j])
                        {
                            a++;
                        }
                    }
                }

                baList.Add(Math.Log10(1d / b), Math.Log10(a));
            }
            return baList;
        }    


        public Form1()
        {
            InitializeComponent(); 
            Bitmap bwContour = null;
            using (var image = new Bitmap(@"C:\Users\Yura\Desktop\Work\Code\BoxDimPrxs4.png"))
            {
                bwContour = new Bitmap(image);
            }
            CalcOtsu(bwContour);
            CreateBW(bwContour, @"C:\Users\Yura\Desktop\Work\Code\");
            //CreateG(bwContour, @"C:\Users\Yura\Desktop\Work\Code\");
            IDictionary<double, double> baList = BowCountingDimension(bwContour, 4, 120, 2, @"C:\Users\Yura\Desktop\Work\Code\" /*+ "boxing\\"*/);
            double[] y = new double[baList.Count];
            double[] x = new double[baList.Count];
            int c = 0;
            foreach (double key in baList.Keys)
            {
                y[c] = baList[key];
                x[c] = key;
                c++;
            }
            double[] theta = NormalEquations2d(y, x);
            double[] temp = new double[100];
        }
    }
}
