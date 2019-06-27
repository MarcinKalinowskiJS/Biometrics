using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WpfApplication1
{
    
    class Histogram
    {
        public Histogram()
        {
        }
        public int[] getColorHistogram(int color)
        {
            if (MainWindow.wBmp == null)
            {
                return null;
            }

            int[] Histogram = new int[256];
            for(int i = 0; i < 256; i++)
                Histogram[i] = 0;
            System.Windows.Media.Color pixel;

            for (int y = 0; y < MainWindow.wBmp.Height; y++)
                for (int x = 0; x < MainWindow.wBmp.Width; x++)
                {
                    pixel = MainWindow.wBmp.GetPixel(x, y);
                    switch(color)
                    {
                        case 0:
                            Histogram[pixel.R]++;
                            break;
                        case 1:
                            Histogram[pixel.G]++;
                            break;
                        case 2:
                            Histogram[pixel.B]++;
                            break;
                        case 3:
                            Histogram[pixel.A]++;
                            break;
                        case 4:
                            Histogram[(pixel.R + pixel.G + pixel.B) / 3]++;
                            break;
                    }
                }
            return Histogram;
        }

        public void resizeHistogram(int MIN, int MAX)
        {
            int[] LUTR = new int[256];
            int[] LUTG = new int[256];
            int[] LUTB = new int[256];

            for (int i = 0; i < 256; i++)
            {
                LUTR[i] = (int)((255.0 / (MAX - MIN)) * (i - MIN));
                LUTG[i] = (int)((255.0 / (MAX - MIN)) * (i - MIN));
                LUTB[i] = (int)((255.0 / (MAX - MIN)) * (i - MIN));
            }

            
            for (int y = 0; y < MainWindow.wBmp.Height; y++)
                for (int x = 0; x < MainWindow.wBmp.Width; x++)
                {
                    System.Windows.Media.Color pix = MainWindow.wBmp.GetPixel(x, y);
                    MainWindow.wBmp.SetPixel(x,y,  (byte)LUTR[pix.R], (byte)LUTG[pix.G], (byte)LUTB[pix.B]);
                }
            
        }
        public void equalGreyHistogram()
        {
            int[] greyHist = getColorHistogram(4);
            double sumgray=0;
            List<double> Dgray = new List<double>();

            int pixels = (int)(MainWindow.wBmp.Height * MainWindow.wBmp.Width);
            for (int i = 0; i < 256; i++)
            {
                sumgray += ((double)(greyHist[i])) / pixels;
                Dgray.Add(sumgray);
            }
            double min = 0;
            for (int i = 0; i < 256; i++)
            {
                if (Dgray[i] != 0)
                {
                    min = Dgray[i];
                    break;
                }
            }
            int[] LUTGray = new int[256];

            for (int i = 0; i < 256; i++)
            {
                LUTGray[i] = (int)((Dgray[i] - min)/(1.0 - min) * 255);
            }

            for (int i = 0; i < MainWindow.wBmp.Height; i++)
                for (int j = 0; j < MainWindow.wBmp.Width; j++)
                {
                    System.Windows.Media.Color pix = MainWindow.wBmp.GetPixel(j, i);
                    MainWindow.wBmp.SetPixel(j, i, (byte)LUTGray[pix.R], (byte)LUTGray[pix.G], (byte)LUTGray[pix.B]);
                }
        }
        public void equalRGBHistogram()
        {
            int[] redHist = getColorHistogram(0);
            int[] greenHist = getColorHistogram(1);
            int[] blueHist = getColorHistogram(2);

            double sumRed = 0;
            double sumGreen = 0;
            double sumBlue = 0;

            List<double> DRed = new List<double>();
            List<double> DGreen = new List<double>();
            List<double> DBlue = new List<double>();

            int pixels = (int)(MainWindow.wBmp.Height * MainWindow.wBmp.Width);
            for (int i = 0; i < 256; i++)
            {
                sumRed += ((double)(redHist[i])) / pixels;
                sumGreen += ((double)(greenHist[i])) / pixels;
                sumBlue += ((double)(blueHist[i])) / pixels;
                DRed.Add(sumRed);
                DGreen.Add(sumGreen);
                DBlue.Add(sumBlue);
            }
            double minRed = 0;
            double minGreen = 0;
            double minBlue = 0;
            for (int i = 0; i < 256; i++)
            {
                if (DRed[i] != 0)
                {
                    minRed = DRed[i];
                    break;
                }
            }
            for (int i = 0; i < 256; i++)
            {
                if (DGreen[i] != 0)
                {
                    minGreen = DGreen[i];
                    break;
                }
            }
            for (int i = 0; i < 256; i++)
            {
                if (DBlue[i] != 0)
                {
                    minBlue = DBlue[i];
                    break;
                }
            }
            int[] LUTRed = new int[256];
            int[] LUTGreen = new int[256];
            int[] LUTBlue = new int[256];

            for (int i = 0; i < 256; i++)
            {
                LUTRed[i] = (int)((DRed[i] - minRed) / (1.0 - minRed) * 255);
                LUTGreen[i] = (int)((DGreen[i] - minGreen) / (1.0 - minGreen) * 255);
                LUTBlue[i] = (int)((DBlue[i] - minBlue) / (1.0 - minBlue) * 255);
            }

            for (int y = 0; y < MainWindow.wBmp.Height; y++)
                for (int x = 0; x < MainWindow.wBmp.Width; x++)
                {
                    System.Windows.Media.Color pix = MainWindow.wBmp.GetPixel(x, y);
                    MainWindow.wBmp.SetPixel(x, y, (byte)LUTRed[pix.R], (byte)LUTGreen[pix.G], (byte)LUTBlue[pix.B]);
                }
        }

    }
    //Histogram from image
    //https://stackoverflow.com/questions/27136360/using-c-sharp-to-produce-a-color-histogram-from-an-image
}
