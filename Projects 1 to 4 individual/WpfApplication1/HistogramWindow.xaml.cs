using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for HistogramWindow.xaml
    /// </summary>
    public partial class HistogramWindow : Window
    {
        private MainWindow _mw;
        private Polygon lastPolygon = null;
        private WriteableBitmap wBmp_temp;
        public class ComboData
        {
            public int Id { get; set; }
            public string value { get; set; }
        }
        public HistogramWindow()
        {
            InitializeComponent();
            wBmp_temp = MainWindow.wBmp.Clone();
            foreach (Window w in Application.Current.Windows)
                if (w is MainWindow)
                    _mw = (MainWindow)w;

            SD_Brightness.Value = 25;
            
            List<ComboData> ListHistogram = new List<ComboData>();
            ListHistogram.Add(new ComboData { Id = 0, value = "Red" });
            ListHistogram.Add(new ComboData { Id = 1, value = "Green" });
            ListHistogram.Add(new ComboData { Id = 2, value = "Blue" });
            ListHistogram.Add(new ComboData { Id = 3, value = "(R+G+B)/3" });
            CB_SelectHistogram.ItemsSource = ListHistogram;
            CB_SelectHistogram.DisplayMemberPath = "value";
            CB_SelectHistogram.SelectedValuePath = "Id";
            CB_SelectHistogram.SelectedValue = 3;

            List<ComboData> ListEqual = new List<ComboData>();
            ListEqual.Add(new ComboData { Id = 0, value = "RGB" });
            ListEqual.Add(new ComboData { Id = 1, value = "GrayScale" });
            CB_SelectToEqual.ItemsSource = ListEqual;
            CB_SelectToEqual.DisplayMemberPath = "value";
            CB_SelectToEqual.SelectedValuePath = "Id";
            CB_SelectToEqual.SelectedValue = 1;

            
            Histogram hs = new Histogram();
            int[] list = hs.getColorHistogram(4);
            double max = list.Max();
            PointCollection points = new PointCollection();
            int winWidth = (int)this.Width;
            int winHeight = (int)this.Height - 117;

            max = System.Math.Log10(max);            
            
            double scaley = max / winHeight;
            points.Add(new Point(0, winHeight+100));
            float scalex = winWidth / 256;

            for (int i = 0; i < 256; i++)
            {
                int h = (int)(winHeight + 80 - (System.Math.Log10(list[i]) / scaley));
                if (h < 0) h = winHeight+80;
                points.Add(new Point(i*scalex, h)); //Wykres jest rysowany od góry
                points.Add(new Point(i*scalex+scalex, h ));

            }

            points.Add(new Point((256 - 1) * scalex, winHeight+100));
            Polygon p = new Polygon();
            p.Stroke = Brushes.Black;
            p.Fill = Brushes.LightBlue;
            p.StrokeThickness = 1;
            p.HorizontalAlignment = HorizontalAlignment.Left;
            p.VerticalAlignment = VerticalAlignment.Center;
            p.Points = points;
            Histogram.Children.Add(p);
            lastPolygon = p;
        }
        private void BT_EqualHistogram(object sender, RoutedEventArgs e)
        {
            if (_mw.imgPhoto != null && _mw.imgPhoto.Source != null)
            {
                MainWindow.wBmp = new WriteableBitmap((BitmapSource)_mw.imgPhoto.Source);
                Histogram hs = new Histogram();
                if(CB_SelectToEqual.SelectedValue.ToString() == "0")
                {
                    hs.equalRGBHistogram();
                }
                if (CB_SelectToEqual.SelectedValue.ToString() == "1")
                {
                    hs.equalGreyHistogram();
                }
                    _mw.imgPhoto.Source = MainWindow.wBmp;
            }
        }
        private void CB_SelectHistogram_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Histogram hs = new Histogram();
            int[] list;
            if (CB_SelectHistogram.SelectedValue == null)
            {
                return;
            }
            switch (CB_SelectHistogram.SelectedValue.ToString())
            {
                case "0": list = hs.getColorHistogram(0); break;
                case "1": list = hs.getColorHistogram(1); break;
                case "2": list = hs.getColorHistogram(2); break;
                case "3": list = hs.getColorHistogram(4); break;
                default: list = null; break;
            }
            if (list == null)
                return;
            double max = list.Max();
            PointCollection points = new PointCollection();
            int winWidth = (int)this.Width;
            int winHeight = (int)this.Height - 117;

            max = System.Math.Log10(max);

            double scaley = max / winHeight;
            points.Add(new Point(0, winHeight + 100));
            float scalex = winWidth / 256;

            for (int i = 0; i < 256; i++)
            {
                int h = (int)(winHeight + 80 - (System.Math.Log10(list[i]) / scaley));
                if (h < 0) h = winHeight + 80;
                points.Add(new Point(i * scalex, h)); //Wykres jest rysowany od góry
                points.Add(new Point(i * scalex + scalex, h));

            }

            points.Add(new Point((256 - 1) * scalex, winHeight + 100));
            Polygon p = new Polygon();
            p.Stroke = Brushes.Black;
            p.Fill = Brushes.LightBlue;
            p.StrokeThickness = 1;
            p.HorizontalAlignment = HorizontalAlignment.Left;
            p.VerticalAlignment = VerticalAlignment.Center;
            p.Points = points;
            Histogram.Children.Remove(lastPolygon);
            Histogram.Children.Add(p);
            lastPolygon = p;
        }
        private void BT_ResizeHistogram(object sender, RoutedEventArgs e)
        {
            if (_mw.imgPhoto != null && _mw.imgPhoto.Source != null)
            {
                MainWindow.wBmp = new WriteableBitmap((BitmapSource)_mw.imgPhoto.Source);
                Histogram hs = new Histogram();
                int from, to;
                bool from_exit, to_exit;
                from_exit = int.TryParse(resize_from.Text, out from);
                to_exit = int.TryParse(resize_to.Text, out to);
                if(from>to)
                {
                    int tmp = from;
                    from = to;
                    to = tmp;
                }
                if(from_exit == true && to_exit == true)
                {
                    hs.resizeHistogram(from, to);
                    _mw.imgPhoto.Source = MainWindow.wBmp;
                }else if(from_exit == false && to_exit==false)
                {
                    MessageBox.Show("Podane obie wartosci(" + resize_from.Text + " i " + resize_to.Text + ") do rozciagniecia sa bledne");
                }else if(from_exit == false)
                {
                    MessageBox.Show("Podana wartosc poczatkowa(" + resize_from.Text + ") jest bledna");
                }else
                {
                    MessageBox.Show("Podana wartosc koncowa(" + resize_to.Text + ") jest bledna");
                }

                
            }
        }
        public void ChkB_SLD_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkB_SLD.IsChecked == (bool?)false)
                _mw.imgPhoto.Source = wBmp_temp;
            if (ChkB_SLD.IsChecked == (bool?)true)
                _mw.imgPhoto.Source = MainWindow.wBmp;
        }
        //Funkcja od rozjaśniania i przyciemniania, działająca po zaznaczeniu Checkboxa
        public void SliderLighterDarker(object sender, RoutedEventArgs e)
        {
            if (ChkB_SLD.IsChecked == (bool?)false)//Checkbox czy ma być zastosowana zmiana jasności
            {
                return;
            }
            Color pixel;
            int countedRed;
            int countedGreen;
            int countedBlue;
            double sd_val = SD_Brightness.Value;
            sd_val -= 25;
            sd_val = System.Math.Abs(sd_val) * sd_val;
            for (int y = 0; y < MainWindow.wBmp.Height; y++)
                for (int x = 0; x < MainWindow.wBmp.Width; x++)
                {
                    pixel = wBmp_temp.GetPixel(x, y);
                    countedRed = (int)(pixel.R + sd_val);
                    countedGreen = (int)(pixel.G + sd_val);
                    countedBlue = (int)(pixel.B + sd_val);
                    if (countedRed > 255) countedRed = 255;
                    else if (countedRed < 0) countedRed = 0;

                    if (countedGreen > 255) countedGreen = 255;
                    else if (countedGreen < 0) countedGreen = 0;

                    if (countedBlue > 255) countedBlue = 255;
                    else if (countedBlue < 0) countedBlue = 0;

                    MainWindow.wBmp.SetPixel(x, y, (byte)countedRed, (byte)countedGreen, (byte)countedBlue);

                    _mw.imgPhoto.Source = MainWindow.wBmp;
                }
        }
        private void BT_ConfirmBrightness(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
