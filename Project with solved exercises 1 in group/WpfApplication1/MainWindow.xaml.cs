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
using System.ComponentModel;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);

        public class CheckBoxBinding : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged(string propName)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }

            private bool val;
            public bool Val
            {
                get
                {
                    return val;
                }
                set
                {
                    val = value;
                    this.NotifyPropertyChanged("Val");
                }
            }
        }



        public CheckBoxBinding Calculating { set; get; }
        System.Windows.Point clickPointDown, clickPointUp, movedTo;
        double zoom = 1.0f;
        bool editPixel = false;
        public static WriteableBitmap wBmp;
        public static WriteableBitmap wBmp_original;
        public WriteableBitmap tmpWBmp = null; //Obraz sumy filtrow
        System.Windows.Media.Color pickedColor;
        TranslateTransform translateTransform = new TranslateTransform();
        ScaleTransform scaleTransform = new ScaleTransform();
        bool movingImage = false;
        bool readPixel = false;



        public MainWindow()
        {
            Calculating = new CheckBoxBinding();
            Calculating.Val = false;
            InitializeComponent();
            this.DataContext = this;
        }



        private void BT_Open(object sender, RoutedEventArgs e)
        {
            Calculating.Val = true;
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "All types|*.bmp;*.jpg;*.jpeg;*.gif;*.png;*.tif;*.tiff|"
                + "JPEG Files (*.jpeg;*.jpg)|*.jpeg;*.jpeg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif|BMP Files (*.bmp)|*.bmp|TIFF Files (*.tiff;*.tif)|*.tiff;*.tif";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string extension = System.IO.Path.GetExtension(dlg.FileName);
                try
                {
                    byte[] ir = File.ReadAllBytes(dlg.FileName);
                    System.Drawing.Image i = System.Drawing.Image.FromStream(new MemoryStream(ir));
                    Bitmap bmp = new Bitmap(new Bitmap(i));
                    var hBitmap = bmp.GetHbitmap();
                    var drawable = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(hBitmap);
                    imgPhoto.Source = drawable;
                    wBmp_original = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                    imgPhotoOriginal.Source = drawable;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            Calculating.Val = false;
        }

        public BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public BitmapImage ImageFromBuffer(Byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }

        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();

                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }

        private void BT_Save(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "All types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|"
                + "JPEG Files (*.jpeg;*.jpg)|*.jpeg;*.jpeg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif|BMP Files (*.bmp)|*.bmp|TIFF Files (*.tiff;*.tif)|*.tiff;*.tif";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                string extension = System.IO.Path.GetExtension(dlg.FileName);
                // Save document
                switch (extension)
                {
                    case ".bmp":
                        FileStream saveStream = new FileStream(dlg.FileName, FileMode.OpenOrCreate);
                        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgPhoto.Source));
                        encoder.Save(saveStream);
                        saveStream.Close();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        // Get a bitmap. The using statement ensures objects  
                        // are automatically disposed from memory after use.  
                        using (Bitmap bmp1 = new Bitmap(BitmapFromSource((BitmapSource)imgPhoto.Source)))
                        {
                            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                            // Create an Encoder object based on the GUID  
                            // for the Quality parameter category.  
                            System.Drawing.Imaging.Encoder myEncoder =
                                System.Drawing.Imaging.Encoder.Quality;

                            // Create an EncoderParameters object.  
                            // An EncoderParameters object has an array of EncoderParameter  
                            // objects. In this case, there is only one  
                            // EncoderParameter object in the array.  
                            EncoderParameters myEncoderParameters = new EncoderParameters(1);

                            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                            myEncoderParameters.Param[0] = myEncoderParameter;
                            bmp1.Save(dlg.FileName, jpgEncoder, myEncoderParameters);
                        }
                        break;
                    case ".png":
                        using (var fileStream = new FileStream(dlg.FileName, FileMode.Create))
                        {
                            BitmapEncoder encoder2 = new PngBitmapEncoder();
                            encoder2.Frames.Add(BitmapFrame.Create((BitmapSource)imgPhoto.Source));
                            encoder2.Save(fileStream);
                        }
                        break;
                    case ".tiff":
                    case ".tif":
                        FileStream stream = new FileStream(dlg.FileName, FileMode.Create);
                        TiffBitmapEncoder encoder1 = new TiffBitmapEncoder();
                        encoder1.Compression = TiffCompressOption.Zip;
                        encoder1.Frames.Add(BitmapFrame.Create(((BitmapSource)imgPhoto.Source)));
                        encoder1.Save(stream);
                        break;
                    case ".gif":
                        FileStream gifstream = new FileStream(dlg.FileName, FileMode.Create);
                        GifBitmapEncoder gifencoder = new GifBitmapEncoder();
                        gifencoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgPhoto.Source));
                        gifencoder.Save(gifstream);
                        break;

                }
            }
        }
        private void BT_Histogram(object sender, RoutedEventArgs e)
        {
            if (imgPhoto != null && imgPhoto.Source != null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                HistogramWindow window = new HistogramWindow();
                window.Show();
            }
        }
        private void BT_EqualHistogram(object sender, RoutedEventArgs e)
        {
            if (imgPhoto != null && imgPhoto.Source != null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                Histogram hs = new Histogram();
                hs.equalGreyHistogram();
                imgPhoto.Source = wBmp;
            }
        }
        private void BT_ResizeHistogram(object sender, RoutedEventArgs e)
        {

            if (imgPhoto != null && imgPhoto.Source != null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                Histogram hs = new Histogram();
                hs.resizeHistogram(10, 240);
                imgPhoto.Source = wBmp;
            }
        }
        private void BT_Scale_x2(object sender, RoutedEventArgs e)
        {
            zoom *= 2;
            scaleTransform.ScaleX = zoom;
            scaleTransform.ScaleY = zoom;
            imgPhoto.RenderTransform = scaleTransform;
            //TransformGroup tg = imgPhoto.RenderTransform;
            //((ScaleTransform)((TransformGroup)imgPhoto.RenderTransform).Children[0]).ScaleX = zoom;
            //((ScaleTransform)((TransformGroup)imgPhoto.RenderTransform).Children[0]).ScaleY = zoom;
            //var bitmap = new TransformedBitmap(imgPhoto.Source, new ScaleTransform(zoom, zoom));
        }
        private void BT_Binarization(object sender, RoutedEventArgs e)
        {
            Calculating.Val = true;
            int threshold = 0;

            //Kiedy parsowanie się nie powiodło lub wartość jest spoza zakresu
            if (int.TryParse(TB_Binarization_Threshold.Text, out threshold) == false || threshold > 255 || threshold < 0)
            {
                MessageBox.Show("Podano błędną wartość progu");
                return;
            }

            System.Windows.Media.Color pix;
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            for (int y = 0; y < wBmp.Height; y++)
            {
                for (int x = 0; x < wBmp.Width; x++)
                {
                    pix = wBmp.GetPixel(x, y);
                    if ((pix.R + pix.G + pix.B) / 3 > threshold)
                        wBmp.SetPixel(x, y, (byte)255, (byte)255, (byte)255);
                    else
                        wBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);
                }
            }
            imgPhoto.Source = wBmp;
            Calculating.Val = false;
        }
        private void BT_Otsu_Find_Threshold(object sender, RoutedEventArgs e)
        {
            Calculating.Val = true;
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            int[] greyHist = (new Histogram()).getColorHistogram(4);

            double sum = 0;
            for (int i = 0; i < 256; i++) sum += i * greyHist[i];

            double sumB = 0;
            int wB = 0;
            int wF = 0;

            double varMax = 0;
            int threshold = 0;

            for (int t = 0; t < 256; t++)
            {
                wB += greyHist[t];  //Waga tła
                if (wB == 0) continue;

                wF = ((int)(wBmp.Height * wBmp.Width)) - wB;    //Waga pierwszego planu
                if (wF == 0) break;

                sumB += (double)(t * greyHist[t]);

                double mB = sumB / wB;          //średnia tła
                double mF = (sum - sumB) / wF;  //średnia pierwszego planu

                //Wariancja międzyklasowa
                double varBetween = (double)wB * (double)wF * (mB - mF) * (mB - mF);

                if (varBetween > varMax)
                {
                    varMax = varBetween;
                    threshold = t;
                }
            }
            TB_Binarization_Threshold.Text = threshold.ToString();
            BT_Binarization(sender, e);
            Calculating.Val = false;
        }
        private void BT_Otsu_Find_Threshold_old(object sender, RoutedEventArgs e)
        {
            double[] variance = new double[256];
            for (int i = 0; i < 256; i++)
                variance[i] = 0;
            double[] omega = new double[2];
            double[] mikroi = new double[2];
            double mikro = 0;
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            int[] greyHist = (new Histogram()).getColorHistogram(4);
            for (int i = 0; i < 256; i++)
                mikro += greyHist[i] * i;
            int allPixels = (int)(wBmp.Height * wBmp.Width);
            mikro /= allPixels;

            for (int i = 0; i < 256; i++)   //Badanie każdego z progów(próg-i)
            {
                //omega[i] jest prawdopodobieństwem wystąpienia i-tej klasy, avg_brightness[i]
                //jest średnią jasnością i - tej klasy wyznaczoną dla
                //progu p, zaś mikro jest średnią jasnością całego obrazu.
                omega[0] = 0;
                omega[1] = 0;
                int j = 0;
                for (j = 0; j < i; j++)
                {
                    omega[0] += greyHist[j];
                }
                omega[0] /= allPixels;
                for (; j < 256; j++)
                    omega[1] += greyHist[j];
                omega[1] /= allPixels;

                j = 0;
                mikroi[0] = 0;
                int ilosc = 0;
                for (j = 0; j < i; j++)
                {
                    ilosc += greyHist[j];
                    mikroi[0] += greyHist[j] * j;
                }
                mikroi[0] /= ilosc;

                ilosc = 0;
                mikroi[1] = 0;
                for (; j < 256; j++)
                {
                    ilosc += greyHist[j];
                    mikroi[1] += greyHist[j] * j;
                }
                mikroi[1] /= ilosc;

                variance[i] = omega[0] * (mikroi[0] - mikro)
                    + omega[1] * (mikroi[1] - mikro);
            }

            double min_variance = Int32.MaxValue;
            int min_i = 0;
            for (int i = 0; i < 256; i++)
            {
                if (variance[i] != 0 && variance[i] < min_variance)
                {
                    min_variance = variance[i];
                    min_i = i;
                }
            }
            MessageBox.Show(min_variance + " " + min_i);
        }
        private void BT_Niblack_Threshold(object sender, RoutedEventArgs e)
        {
            Calculating.Val = true; 
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            WriteableBitmap tmpWBmp = wBmp.Clone();
            double k = 0.2;
            int wSize = 25;
            if (double.TryParse(TB_Niblack_K.Text, out k) == false)
            {
                MessageBox.Show("Wartość k jest błędna (np:0,1)");
                return;
            }
            if (int.TryParse(TB_Niblack_Window_Size.Text, out wSize) == false || wSize % 2 == 0)
            {
                MessageBox.Show("Podano błędną wartość rozmiaru okna (np:25)");
                return;
            }



            for (int y = 0; y < tmpWBmp.Height; y++)
            {
                for (int x = 0; x < tmpWBmp.Width; x++)
                {
                    //Średnia i suma
                    double avg = 0;
                    int sum = 0;
                    int tmp;
                    int tx, ty;
                    System.Windows.Media.Color pix;
                    for (int yy = -wSize / 2; yy <= wSize / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= tmpWBmp.Height; yi++) ;
                        for (int xx = -wSize / 2; xx <= wSize / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= tmpWBmp.Width; xi++) ;
                            pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                            tmp = (pix.R + pix.G + pix.B) / 3;
                            avg += tmp;
                            sum += tmp * tmp;
                        }
                    }
                    avg /= wSize * wSize;
                    //wartość progu = średnia + k*std_dev;
                    double threshold = avg + k * Math.Sqrt((sum / (int)(wSize * wSize)) - (avg * avg));
                    //Trace.WriteLine( "y" + y + " x" + x + " M.Sqrt" + Math.Sqrt((sum / (int)(wSize * wSize)) - (avg * avg)) +  " threshold" + threshold);
                    pix = wBmp.GetPixel(x, y);
                    tmp = (pix.R + pix.G + pix.B) / 3;
                    if (tmp > threshold)
                        tmpWBmp.SetPixel(x, y, (byte)255, (byte)255, (byte)255);
                    else
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);
                }
            }
            imgPhoto.Source = tmpWBmp;
            Calculating.Val = false;
        }
        public void MoveTo(System.Windows.Controls.Image target, double newX, double newY)
        {
            //Vector offset = VisualTreeHelper.GetOffset(target);
            //var top = offset.Y;
            //var left = offset.X;
            double oldCenterX = scaleTransform.CenterX;
            double oldCenterY = scaleTransform.CenterY;

            if (zoom > 1)
            {
                scaleTransform.CenterX = newX * -1;
                scaleTransform.CenterY = newY * -1;
            }
            else if (zoom < 1)
            {
                scaleTransform.CenterX = newX;
                scaleTransform.CenterY = newY;
            }
            else
            {
                TranslateTransform translateTransform = new TranslateTransform();
                target.RenderTransform = translateTransform;
                System.Windows.Media.Animation.DoubleAnimation translateanim1 = new System.Windows.Media.Animation.DoubleAnimation(0, newY, TimeSpan.FromSeconds(0));
                System.Windows.Media.Animation.DoubleAnimation translateanim2 = new System.Windows.Media.Animation.DoubleAnimation(0, newX, TimeSpan.FromSeconds(0));
                translateTransform.BeginAnimation(TranslateTransform.YProperty, translateanim1);
                translateTransform.BeginAnimation(TranslateTransform.XProperty, translateanim2);
                return;  // RETURN!!!
            }

            System.Windows.Media.Animation.DoubleAnimation anim1 = new System.Windows.Media.Animation.DoubleAnimation(0, newY, TimeSpan.FromSeconds(0));
            System.Windows.Media.Animation.DoubleAnimation anim2 = new System.Windows.Media.Animation.DoubleAnimation(0, newX, TimeSpan.FromSeconds(0));
            scaleTransform.BeginAnimation(TranslateTransform.YProperty, anim1);
            scaleTransform.BeginAnimation(TranslateTransform.XProperty, anim2);
        }
        private void BT_Scale_div2(object sender, RoutedEventArgs e)
        {
            zoom /= 2;
            scaleTransform.ScaleX = zoom;
            scaleTransform.ScaleY = zoom;
            imgPhoto.RenderTransform = scaleTransform;
        }

        private void BT_EditPixel(object sender, RoutedEventArgs e)
        {
            editPixel = !editPixel;
        }

        private void BT_ReadPixel(object sender, RoutedEventArgs e)
        {
            readPixel = !readPixel;
        }
        private void BT_ShowOriginal(object sender, RoutedEventArgs e)
        {
            if (imgPhoto != null && imgPhoto.Source != null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                OriginalImageWindow window = new OriginalImageWindow();
                window.Show();
            }
        }
        private void ClrPckerChanged(object sender, RoutedEventArgs e)
        {
            pickedColor.R = ClrPcker.SelectedColor.Value.R;
            pickedColor.G = ClrPcker.SelectedColor.Value.G;
            pickedColor.B = ClrPcker.SelectedColor.Value.B;
            pickedColor.A = ClrPcker.SelectedColor.Value.A;
        }
        private void BT_UserFilter(object sender, RoutedEventArgs e)
        {
            //Wczytywanie maski filtra
            int size;
            if (int.TryParse(TB_FilterSize.Text, out size) && size % 2 != 0 && size > 0)
            {
                FilterMask window = new FilterMask(size);

                if ((bool)window.ShowDialog() == true)
                {
                    var table = window.Table;



                    if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
                    {
                        MessageBox.Show("Najpierw wczytaj obraz");
                        return;
                    }



                    //Filtrowanie daną maską
                    Calculating.Val = true;
                    //Obliczanie podzielnika dla maski, jeśli wartość jest mniejsza od zera to bierzemy moduł wartości
                    double podzielnik = 0;
                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            podzielnik += Math.Abs(table.ElementAt(i).ElementAt(j).TI);
                        }
                    }
                    if (podzielnik == 0)
                    {
                        podzielnik = 1;
                    }


                    if (tmpWBmp == null)
                    {
                        wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                        tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                        for (int y = 0; y < tmpWBmp.Height; y++)
                        {
                            for (int x = 0; x < tmpWBmp.Width; x++)
                            {
                                tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                            }
                        }
                    }

                    System.Windows.Media.Color pix;
                    System.Windows.Media.Color pixToAdd;

                    int tx, ty;
                    for (int y = 0; y < tmpWBmp.Height; y++)
                    {
                        for (int x = 0; x < tmpWBmp.Width; x++)
                        {
                            int[] tmp = { 0, 0, 0 };
                            for (int yy = -size / 2; yy <= size / 2; yy++)
                            {
                                ty = y + yy;
                                if (ty < 0)
                                    ty = y + Math.Abs(yy);
                                int yi = 0;
                                for (; ty - yi >= tmpWBmp.Height; yi++) ;
                                for (int xx = -size / 2; xx <= size / 2; xx++)
                                {
                                    tx = x + xx;
                                    if (tx < 0)
                                        tx = x + Math.Abs(xx);
                                    int xi = 0;
                                    for (; tx - xi >= tmpWBmp.Width; xi++) ;
                                    pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                                    tmp[0] += pix.R * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2).TI;
                                    tmp[1] += pix.G * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2).TI;
                                    tmp[2] += pix.B * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2).TI;
                                }
                            }

                            //Sumowanie wcześniejszej filtracji z aktualną przed zatwierdzeniem
                            pixToAdd = tmpWBmp.GetPixel(x, y);
                            byte[] pixToAddByte = { pixToAdd.R, pixToAdd.G, pixToAdd.B };
                            for (int i = 0; i < 3; i++)
                            {
                                tmp[i] = (int)(tmp[i] / podzielnik);
                                if (tmp[i] + pixToAddByte[i] > 255)
                                {
                                    tmp[i] = 255;
                                }
                                else if (tmp[i] + pixToAddByte[i] < 0)
                                {
                                    tmp[i] = 0;
                                }
                                else
                                {
                                    tmp[i] = tmp[i] + pixToAddByte[i];
                                    if (tmp[i] > 255)
                                    {
                                        tmp[i] = 255;
                                    }
                                    else if (tmp[i] < 0)
                                    {
                                        tmp[i] = 0;
                                    }
                                }
                            }


                            tmpWBmp.SetPixel(x, y, (byte)tmp[0], (byte)tmp[1], (byte)tmp[2]);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Wartość \"" + TB_FilterSize.Text + "\" jest błędna");
            }
            Calculating.Val = false;
        }

        private void BT_PrewittFilter(object sender, RoutedEventArgs e)
        { 
            if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }


            Calculating.Val = true;
            int size = 3;
            int[,,] mask = { //Maska filtru 0 45 90 i 135 stopni
                { { -1, 0 ,1 }, { -1, 0, 1 }, { -1, 0, 1 } },
                { { 0, 1 ,1 }, { -1, 0, 1 }, { -1, -1, 0 } },
                { { 1, 1, 1 }, { 0, 0, 0 }, { -1, -1, -1 } },
                { { 1, 1, 0 }, { 1, 0, -1 }, { 0, -1, -1 } }
            };

            int[][][] jaggedMask = new int[4][][];
            for (int z = 0; z < 4; z++)
            {
                jaggedMask[z] = new int[3][];
                for (int y = 0; y < 3; y++)
                {
                    jaggedMask[z][y] = new int[3];
                    for (int x = 0; x < 3; x++)
                    {
                        jaggedMask[z][y][x] = mask[z, y, x];
                    }
                }
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            //Filtrowanie daną maską
            for (int i = 0; i < 4; i++)
            {
                LinearFilter(wBmp, tmpWBmp, size, jaggedMask[i]);
            }
            Calculating.Val = false;
        }

        private void BT_SobelFilter(object sender, RoutedEventArgs e)
        {
            if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }
            Calculating.Val = true;

            int size = 3;
            int[,,] mask = { //Maska filtru 0 45 90 i 135 stopni
                { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } },
                { { 0, 1, 2 }, { -1, 0, 1 }, { -2, -1, 0 } },
                { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } },
                { { 2, 1, 0 }, { 1, 0, -1 }, { 0, -1, -2 } }
            };

            int[][][] jaggedMask = new int[4][][];
            for (int z = 0; z < 4; z++)
            {
                jaggedMask[z] = new int[3][];
                for (int y = 0; y < size; y++)
                {
                    jaggedMask[z][y] = new int[3];
                    for (int x = 0; x < size; x++)
                    {
                        jaggedMask[z][y][x] = mask[z, y, x];
                    }
                }
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            //Filtrowanie danymi maskami
            for (int i = 0; i < 4; i++)
            {
                LinearFilter(wBmp, tmpWBmp, size, jaggedMask[i]);
            }
            Calculating.Val = false;
        }

        private void BT_LaplaceFilter(object sender, RoutedEventArgs e)
        {
            if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }


            Calculating.Val = true;
            Trace.WriteLine(Calculating.Val + " : " + CB_Processing.IsChecked );
            int size = 3;
            int[,,] mask = { //Maska filtru 0 45 90 i 135 stopni
                { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } },
                { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } },
                { { 1, -2, 1 }, { -2, 4, -2 }, { 1, -2, 1 } },
            };

            int[][][] jaggedMask = new int[3][][];
            for (int z = 0; z < 3; z++)
            {
                jaggedMask[z] = new int[3][];
                for (int y = 0; y < size; y++)
                {
                    jaggedMask[z][y] = new int[3];
                    for (int x = 0; x < size; x++)
                    {
                        jaggedMask[z][y][x] = mask[z, y, x];
                    }
                }
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            //Filtrowanie danymi maskami
            for (int i = 1; i < 2; i++)
            {
                LinearFilter(wBmp, tmpWBmp, size, jaggedMask[i]);
            }
            Calculating.Val = false;
        }

        private void BT_CornerFilter(object sender, RoutedEventArgs e)
        {
            if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }


            Calculating.Val = true;
            int size = 3;
            int[,,] mask = { //Maska filtru 0 45 90 i 135 stopni
                { { 1, 1, 1 }, { 1, -2, -1 }, { 1, -1, -1 } },
                { { 1, 1, 1 }, { -1, -2, 1 }, { -1, -1, 1 } },
                { { 1, -1, -1 }, { 1, -2, -1 }, { 1, 1, 1 } },
                { { -1, -1, 1 }, { -1, -2, 1 }, { 1, 1, 1 } },
            };

            int[][][] jaggedMask = new int[4][][];
            for (int z = 0; z < 4; z++)
            {
                jaggedMask[z] = new int[3][];
                for (int y = 0; y < size; y++)
                {
                    jaggedMask[z][y] = new int[3];
                    for (int x = 0; x < size; x++)
                    {
                        jaggedMask[z][y][x] = mask[z, y, x];
                    }
                }
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            //Filtrowanie danymi maskami
            for (int i = 0; i < 4; i++)
            {
                LinearFilter(wBmp, tmpWBmp, size, jaggedMask[i]);
            }
            Calculating.Val = false;
        }

        private void LinearFilter(WriteableBitmap fromWB, WriteableBitmap toWB, int size, int[][] mask)
        {
            //Obliczanie podzielnika dla maski

            double podzielnik = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    podzielnik += mask[i][j];
                }
            }
            if (podzielnik == 0)
            {
                podzielnik = 1;
            }

            System.Windows.Media.Color pix;
            System.Windows.Media.Color pixToAdd;

            int tx, ty;
            for (int y = 0; y < fromWB.Height; y++)
            {
                for (int x = 0; x < fromWB.Width; x++)
                {
                    //Przejście po masce i po obrazie
                    int tmp = 0;
                    for (int yy = -size / 2; yy <= size / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= fromWB.Height; yi++) ;
                        for (int xx = -size / 2; xx <= size / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= fromWB.Width; xi++) ;
                            pix = fromWB.GetPixel(tx - (xi * 2), ty - (yi * 2));
                            tmp += (pix.R + pix.G + pix.B)/3 * mask[yy + size / 2][xx + size / 2];
                        }
                    }

                    //Sumowanie wcześniejszej filtracji z aktualną przed zatwierdzeniem
                    pixToAdd = toWB.GetPixel(x, y);
                    byte pixToAddByte = (byte)((pixToAdd.R + pixToAdd.G + pixToAdd.B) / 3);
                    
                    tmp = (int)(tmp / podzielnik);
                    if (tmp + pixToAddByte > 255)
                    {
                        tmp = 255;
                    }
                    else if (tmp + pixToAddByte < 0)
                    {
                        tmp = 0;
                    }
                    else
                    {
                        tmp = tmp + pixToAddByte;
                    }



                    toWB.SetPixel(x, y, (byte)tmp, (byte)tmp, (byte)tmp);
                }
            }
        }

        private void BT_KuwaharaFilter(object sender, RoutedEventArgs e)
        {
            //Wczytywanie maski filtra
            int size=5;
            //if (int.TryParse(TB_FilterSize.Text, out size) && size % 2 != 0 && size > 0)
            //{
            //FilterMask window = new FilterMask(size);

            //if ((bool)window.ShowDialog() == true)
            //{
            //var table = window.Table;
            List<List<int>> table = new List<List<int>>();
            for (int i = 0; i < 5; i++)
            {
                List<int> tmpList = new List<int>();
                for (int j = 0; j < 5; j++)
                {
                    tmpList.Add(1);
                }
                table.Add(tmpList);
            }


            if (imgPhoto == null || imgPhoto.Source == null) //Brak obrazu, więc wyjście
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }



            //Filtrowanie daną maską
            Calculating.Val = true;
            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            System.Windows.Media.Color pix;
            System.Windows.Media.Color pixToAdd;

            int tx, ty;
            for (int y = 0; y < tmpWBmp.Height; y++)
            {
                for (int x = 0; x < tmpWBmp.Width; x++)
                {
                    //Obliczanie średniej każdego z czterech regionów
                    double[,] avg = { 
                        { 0, 0, 0 },
                        { 0, 0, 0 },
                        { 0, 0, 0 },
                        { 0, 0, 0 }
                    };
                    int[] valuesCount = { 0, 0, 0, 0 };
                    for (int yy = -size / 2; yy <= size / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= tmpWBmp.Height; yi++) ;
                        for (int xx = -size / 2; xx <= size / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= tmpWBmp.Width; xi++) ;
                            pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                            if (xx < 1 && yy < 1)
                            {
                                avg[0, 0] += pix.R;
                                avg[0, 1] += pix.G;
                                avg[0, 2] += pix.B;
                                valuesCount[0]++;
                            }
                            else if (xx > -1 && yy < 1)
                            {
                                avg[1, 0] += pix.R;
                                avg[1, 1] += pix.G;
                                avg[1, 2] += pix.B;
                                valuesCount[1]++;
                            }
                            else if (xx < 1 && yy > -1)
                            {
                                avg[2, 0] += pix.R;
                                avg[2, 1] += pix.G;
                                avg[2, 2] += pix.B;
                                valuesCount[2]++;
                            }
                            else if (xx > -1 && yy > -1)
                            {
                                avg[3, 0] += pix.R;
                                avg[3, 1] += pix.G;
                                avg[3, 2] += pix.B;
                                valuesCount[3]++;
                            }

                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        avg[i, 0] /= valuesCount[i];
                        avg[i, 1] /= valuesCount[i];
                        avg[i, 2] /= valuesCount[i];
                    }
                    


                    //Obliczanie wariancji dla każdego z czterech regionów
                    double[] variance = { 0, 0, 0, 0 };
                    for (int yy = -size / 2; yy <= size / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= tmpWBmp.Height; yi++) ;
                        for (int xx = -size / 2; xx <= size / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= tmpWBmp.Width; xi++) ;
                            pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));

                            if (xx < 1 && yy < 1)
                            {
                                variance[0] += Math.Pow(((pix.R + pix.G + pix.B) / 3 * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2) - (avg[0, 0] + avg[0, 1] + avg[0, 2]  ) / 3) , 2);
                            }
                            if (xx > -1 && yy < 1)
                            {
                                variance[1] += Math.Pow(((pix.R + pix.G + pix.B) / 3 * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2) - (avg[1, 0] + avg[1, 1] + avg[1, 2]) / 3), 2);
                            }
                            if (xx < 1 && yy > -1)
                            {
                                variance[2] += Math.Pow(((pix.R + pix.G + pix.B) / 3 * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2) - (avg[2, 0] + avg[2, 1] + avg[2, 2]) / 3), 2);
                            }
                            if (xx > -1 && yy > -1)
                            {
                                variance[3] += Math.Pow(((pix.R + pix.G + pix.B) / 3 * table.ElementAt(yy + size / 2).ElementAt(xx + size / 2) - (avg[3, 0] + avg[3, 1] + avg[3, 2]) / 3), 2);
                            }

                            
                        }
                    }
                    double smallestVariance = Int32.MaxValue;
                    int smallestVarianceIndex = -1;
                    for (int i = 0; i < 4; i++)
                    {
                        variance[i] /= valuesCount[i];
                        if (variance[i] < smallestVariance)
                        {
                            smallestVariance = variance[i];
                            smallestVarianceIndex = i;
                        }
                    }

                    pix = new System.Windows.Media.Color();
                    pix.R = (byte)avg[smallestVarianceIndex, 0];
                    pix.G = (byte)avg[smallestVarianceIndex, 1];
                    pix.B = (byte)avg[smallestVarianceIndex, 2];


                    //Sumowanie wcześniejszej filtracji z aktualną przed zatwierdzeniem
                    pixToAdd = tmpWBmp.GetPixel(x, y);
                    byte[] pixToAddByte = { pixToAdd.R, pixToAdd.G, pixToAdd.B };

                    int[] tmp = { 0, 0, 0 }; //Sumowanie dla każdego z kolorów (RGB) oddzielnie
                    for (int i = 0; i < 3; i++)
                    {
                        tmp[i] = (int)avg[smallestVarianceIndex, i];
                        if (tmp[i] + pixToAddByte[i] > 255)
                        {
                            tmp[i] = 255;
                        }
                        else if (tmp[i] + pixToAddByte[i] < 0)
                        {
                            tmp[i] = 0;
                        }
                        else
                        {
                            tmp[i] = tmp[i] + pixToAddByte[i];
                        }
                    }

                    tmpWBmp.SetPixel(x, y, (byte)tmp[0], (byte)tmp[1], (byte)tmp[2]);

                }
            }
                //}
            //}
            //else
            //{
            //    MessageBox.Show("Wartość \"" + TB_FilterSize.Text + "\" jest błędna");
            //}
            Calculating.Val = false;
        }

        private void BT_Median3by3Filter(object sender, RoutedEventArgs e)
        {
            if(imgPhoto==null || imgPhoto.Source == null)
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }


            Calculating.Val = true;
            


            int[,] mask = {
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            };

            MedianFilter(wBmp, tmpWBmp, 3, mask);

            Calculating.Val = false;
        }

        private void BT_Median5by5Filter(object sender, RoutedEventArgs e)
        {
            if (imgPhoto == null || imgPhoto.Source == null)
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }

            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }


            Calculating.Val = true;



            int[,] mask = {
                { 1, 1, 1, 1, 1},
                { 1, 1, 1, 1, 1},
                { 1, 1, 1, 1, 1},
                { 1, 1, 1, 1, 1},
                { 1, 1, 1, 1, 1}
            };

            MedianFilter(wBmp, tmpWBmp, 5, mask);

            Calculating.Val = false;
        }

        private void MedianFilter(WriteableBitmap fromWB, WriteableBitmap toWB, int size, int[,] mask)
        {
            if (tmpWBmp == null)
            {
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                for (int y = 0; y < tmpWBmp.Height; y++)
                {
                    for (int x = 0; x < tmpWBmp.Width; x++)
                    {
                        tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);  //Zerowanie pobranego obrazu na początku wszystkich filtracji, aby później dodawać filtry
                    }
                }
            }

            System.Windows.Media.Color pix;
            System.Windows.Media.Color pixToAdd;

            int tx, ty, median;
            int[] medianTable = new int[size * size];
            for (int y = 0; y < fromWB.Height; y++)
            {
                for (int x = 0; x < fromWB.Width; x++)
                {
                    //Przejście po masce i po obrazie
                    int tmp = 0;
                    for (int yy = -size / 2; yy <= size / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= fromWB.Height; yi++) ;
                        for (int xx = -size / 2; xx <= size / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= fromWB.Width; xi++) ;
                            pix = fromWB.GetPixel(tx - (xi * 2), ty - (yi * 2));
                            medianTable[(yy+size/2 * size) + xx+size/2] = (pix.R + pix.G + pix.B) / 3 * mask[yy + size / 2, xx + size / 2];
                        }
                    }

                    //Wybór mediany
                    Array.Sort(medianTable);
                    median = medianTable[((medianTable.Count() - 1) / 2)];



                    //Sumowanie wcześniejszej filtracji z aktualną przed zatwierdzeniem
                    pixToAdd = toWB.GetPixel(x, y);
                    byte pixToAddByte = (byte)median;

                    if (tmp + pixToAddByte > 255)
                    {
                        tmp = 255;
                    }
                    else if (tmp + pixToAddByte < 0)
                    {
                        tmp = 0;
                    }
                    else
                    {
                        tmp = tmp + pixToAddByte;
                    }

                    toWB.SetPixel(x, y, (byte)tmp, (byte)tmp, (byte)tmp);
                }
            }
        }

        private void BT_ApplyFilters(object sender, RoutedEventArgs e)
        {
            imgPhoto.Source = wBmp = tmpWBmp;
            tmpWBmp = null;
        }
        private void BT_K3M(object sender, RoutedEventArgs e)
        {//https://github.com/kotertom/aipob-thinning/blob/master/scripts/k3m.m
            if (imgPhoto == null || imgPhoto.Source == null)
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            Calculating.Val = true;
            int[][] A = new int[6][];
            A[0] = new int[] { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 };
            A[1] = new int[] { 7, 14, 28, 56, 112, 131, 193, 224 };
            A[2] = new int[] { 7, 14, 15, 28, 30, 56, 60, 112, 120, 131, 135, 193, 195, 224, 225, 240 };
            A[3] = new int[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120, 124, 131, 135, 143, 193, 195, 199, 224, 225, 227, 240, 241, 248 };
            A[4] = new int[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 193, 195, 199, 207, 224, 225, 227, 231, 240, 241, 243, 248, 249, 252 };
            A[5] = new int[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 191, 193, 195, 199, 207, 224, 225, 227, 231, 239, 240, 241, 243, 248, 249, 251, 252, 254 };
            int[] A1pix = { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 };
            int[,] mask3by3 = { { 128, 1, 2 }, { 64, 0, 4 }, { 32, 16, 8 } };
            int size = 3; //Rozmiar maski
            bool checkChange = false;



            System.Windows.Media.Color pix;
            int tx, ty;
            int it = 0;
            do {
                checkChange = false;
                for (int step = 0; step < 6; step++)
                {
                    for (int y = 0; y < wBmp.Height; y++)
                    {
                        for (int x = 0; x < wBmp.Width; x++)
                        {
                            //Przejście po masce i po obrazie
                            int weight = 0;
                            if(step==0)
                                pix = wBmp.GetPixel(x, y);
                            else
                                pix = tmpWBmp.GetPixel(x, y);
                            //Jeśli biały to nie liczymy wagi, jeśli krok różny od zera to liczymy wagę tylko dla czerwonych
                            if ((pix.R==255 && pix.G==255 && pix.B==255) || (!(pix.R == 255 && pix.G == 0 && pix.B == 0) && step != 0))
                            {
                                continue;
                            }
                            for (int yy = -size / 2; yy <= size / 2; yy++)
                            {
                                ty = y + yy;
                                if (ty < 0)
                                    ty = y + Math.Abs(yy);
                                int yi = 0;
                                for (; ty - yi >= wBmp.Height; yi++) ;
                                for (int xx = -size / 2; xx <= size / 2; xx++)
                                {
                                    tx = x + xx;
                                    if (tx < 0)
                                        tx = x + Math.Abs(xx);
                                    int xi = 0;
                                    for (; tx - xi >= wBmp.Width; xi++) ;
                                    if(step==0)
                                        pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                                    else
                                        pix = tmpWBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                                    if (step == 0 && pix.R == 0 && pix.G==0 && pix.B==0)//Jeśli czarny to dodaj wartość do wagi
                                        weight += mask3by3[yy + size / 2, xx + size / 2];
                                    //Jeśli środek to piksel graniczny to dodaj czerwone  i czarne
                                    else if (step != 0 && ((pix.R == 255 && pix.G==0 && pix.B==0) || (pix.R==0 && pix.G==0 && pix.B==0)) )
                                        weight += mask3by3[yy + size / 2, xx + size / 2];
                                }
                            }


                            //Sprawdzanie czy jest spełniona maska A[i]
                            bool checkInMask = false;
                            for (int i = 0; i < A[step].Count(); i++)
                            {
                                if (weight == A[step][i])
                                {
                                    checkInMask = true;
                                    break;
                                }
                            }
                            if (step == 0)//Na początku oznaczanie pikseli granicznych na czerwono
                            {
                                if (checkInMask == true)
                                {
                                    tmpWBmp.SetPixel(x, y, (byte)255, (byte)0, (byte)0);
                                }
                            }
                            else//Sprawdzanie pikseli granicznych
                            {
                                if (checkInMask == true)//Jeśli waga jest w tablicy
                                {
                                    checkChange = true;
                                    tmpWBmp.SetPixel(x, y, (byte)255, (byte)255, (byte)255);//to ustaw piksel na biały
                                }
                            }
                        }
                    }
                }

                //Zamiana reszty pikseli granicznych na czarne po pięciu maskach
                for (int y = 0; y < wBmp.Height; y++)
                {
                    for (int x = 0; x < wBmp.Width; x++)
                    {
                        pix = tmpWBmp.GetPixel(x, y);
                        if (pix.R == 0 || (pix.R == 255 && pix.G == 0 && pix.B == 0)) //Jeśli to czarny lub pozostał nieusunięty czerwony
                        {
                            tmpWBmp.SetPixel(x, y, (byte)0, (byte)0, (byte)0);
                        }
                    }
                }
                imgPhoto.Source = tmpWBmp;
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                Trace.WriteLine("Iteration" + it++ + " changed"+checkChange);

            } while(checkChange == true);



            //Przejście tablicą A1pix
            for (int y = 0; y < wBmp.Height; y++)
            {
                for (int x = 0; x < wBmp.Width; x++)
                {
                    //Przejście po masce i po obrazie
                    int weight = 0;
                    pix = wBmp.GetPixel(x, y);
                    for (int yy = -size / 2; yy <= size / 2; yy++)
                    {
                        ty = y + yy;
                        if (ty < 0)
                            ty = y + Math.Abs(yy);
                        int yi = 0;
                        for (; ty - yi >= wBmp.Height; yi++) ;
                        for (int xx = -size / 2; xx <= size / 2; xx++)
                        {
                            tx = x + xx;
                            if (tx < 0)
                                tx = x + Math.Abs(xx);
                            int xi = 0;
                            for (; tx - xi >= wBmp.Width; xi++) ;
                            pix = wBmp.GetPixel(tx - (xi * 2), ty - (yi * 2));
                            if(pix.R==0)//Jeśli czarny to dodaj wartość do wagi
                                weight += mask3by3[yy + size / 2, xx + size / 2];
                        }
                    }


                    //Sprawdzanie czy jest spełniona maska A1pix
                    bool checkInMask = false;
                    for (int i = 0; i < A1pix.Count(); i++)
                    {
                        if (weight == A1pix[i])
                        {
                            checkInMask = true;
                            break;
                        }
                    }
                    
                    if (checkInMask)//Ostatnie przejscie, wiec jesli waga jest w tablicy A1pix to jest usuwany piksel
                    {
                        tmpWBmp.SetPixel(x, y, (byte)255, (byte)255, (byte)255);//to ustaw piksel na biały
                    }
                }
            }
            //imgPhoto.Source = tmpWBmp;
            Calculating.Val = false;
            //tmpWBmp = null;
        }
        
        private void BT_FindMinutiae(object sender, RoutedEventArgs e)
        {
            if (imgPhoto == null || imgPhoto.Source == null)
            {
                MessageBox.Show("Najpierw wczytaj obraz");
                return;
            }
            wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            tmpWBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
            if(wBmp.Height<3 || wBmp.Width<3)
            {
                MessageBox.Show("Zbyt mały obraz (minimalnie 3x3)");
                return;
            }



            Calculating.Val = true;
            bool[,] tab_minutiae = new bool[(int)wBmp.Height, (int)wBmp.Width];
            for(int y=0;y<wBmp.Height; y++)
            {
                for(int x=0; x<wBmp.Width; x++)
                {
                    tab_minutiae[y,x] = false;
                }
            }


            System.Windows.Media.Color pix;
            int size = 3;
            bool found;
            int sum = 0;
            int lastVal = 0;
            int my = 0, mx = 0;
            int itmp;
            int[,] mask3by3 = { { 4, 3, 2 }, { 5, 0, 1 }, { 6, 7, 8 } };
            for (int y = 1; y < wBmp.Height - 1; y++)
            {
                for (int x = 1; x < wBmp.Width - 1; x++)
                {
                    //Szukanie miejsca pierwszej wartości P1
                    sum = 0;
                    found = false;
                    pix = wBmp.GetPixel(x, y);
                    if (pix.R == 0 && pix.G == 0 && pix.B == 0)//Jeśli czarny
                    {
                        for (my = -size/2; my <= size/2 && found == false;)
                        {
                            my++;
                            for (mx = -size/2; mx <= size/2 && found == false;)
                            {
                                mx++;
                                if (1 == mask3by3[my, mx])
                                {
                                    found = true;
                                }
                            }
                        }

                        pix = wBmp.GetPixel(x - 1 + mx, y - 1 + my);
                        if (pix.R == 0 && pix.G == 0 && pix.B == 0)//Jeśli czarny
                        {
                            lastVal = 1;
                        }
                        else
                        {
                            lastVal = 0;
                        }

                        for (int i = 1; i < size*size; i++)
                        {
                            found = false;
                            itmp = (i + 1) % 9;
                            if (itmp == 0)
                                itmp = 1;
                            for (my = -1; my < 2 && found == false;)
                            {
                                my++;
                                for (mx = -1; mx < 2 && found == false;)
                                {
                                    mx++;
                                    if (itmp == mask3by3[my, mx])
                                    {
                                        found = true;
                                    }
                                }
                            }

                            pix = wBmp.GetPixel(x - 1 + mx, y - 1 + my);
                            if (pix.R == 0 && pix.G == 0 && pix.B == 0)//Jeśli czarny
                            {
                                sum += Math.Abs(lastVal - 1);
                                lastVal = 1;
                            }
                            else
                            {
                                sum += Math.Abs(lastVal - 0);
                                lastVal = 0;
                            }
                        }
                        sum /= 2;
                        

                        //0pojedynczy punkt 1zakończenie krawędzi 2kontynuacja krawędzi 3 rozwidlenie 4skrzyżowanie
                        switch (sum)
                        {
                            case 0:
                            case 1:
                            case 3:
                            case 4:
                                {
                                    tab_minutiae[y, x] = true;
                                    break;
                                }
                        }
                    }
                }
            }



            //Usuwanie minucji podpunkty 1 i 2
            //1.Usuwanie poprzez odległość
            int deleteDistance;
            if (!int.TryParse(TB_MinutiaLengthBetween.Text, out deleteDistance) || deleteDistance < 0) 
            {
                MessageBox.Show("Błędna wartość odległości do usunięcia minucji, wybrana domyślna - 5");
                deleteDistance = 5;
            }
            bool toDelete = false;
            for (int y = 0; y < wBmp.Height; y++) 
            {
                for (int x = 0; x < wBmp.Width; x++)
                {
                    if (tab_minutiae[y, x] == true) //Jeśli to minucja to sprawdź od niej odległości
                    {
                        toDelete = false;
                        int dy=0, dx=0;
                        for (dy = 0; dy < deleteDistance && toDelete == false;)
                        {
                            dy++;
                            for (dx = 0; dx < deleteDistance && toDelete == false;)
                            {
                                dx++;
                                if (y + dy >= wBmp.Height || x + dx >= wBmp.Width)
                                    continue;
                                if (tab_minutiae[y + dy, x + dx] == true)
                                {
                                    toDelete = true;
                                    break;
                                }
                            }
                        }
                        if (toDelete == true)//Jeśli znaleziono dwie minucje w odleglosci mniejszej niz deleteDistance
                        {
                            tab_minutiae[y, x] = false;             //to usun glowna
                            tab_minutiae[y + dy, x + dx] = false;   //i poboczna
                        }
                    }
                }
            }



            //2.Usuwanie maską
            int maskSize = 5;
            if (!int.TryParse(TB_MinutiaMaskSize.Text, out maskSize) || maskSize < 0)
            {
                MessageBox.Show("Błędna wielkość maski do usunięcia minucji, wybrana domyślna - 5");
                maskSize = 5;
            }
            
            for(int y=maskSize/2; y<wBmp.Height-maskSize/2; y++)
            {
                for(int x=maskSize/2; x<wBmp.Width-maskSize/2; x++)
                {
                    sum = 0;
                    for (my = -maskSize / 2; my <= maskSize / 2; my++)
                    {
                        for (mx = -maskSize / 2; mx <= maskSize / 2; mx++)
                        {
                            if(tab_minutiae[y+my, x+mx] == true)
                            {
                                ++sum;
                            }
                        }
                    }
                    //Jeśli więcej niż jedna minucja to usuwamy tą minucje dla której było sprawdzenie
                    if(sum>1)
                    {
                        tab_minutiae[y, x] = false;
                    }
                }
            }



            //Stawianie czerwonych kropek tam gdzie minucje
            int dotSize;
            if ( !int.TryParse(TB_MinutiaDotSize.Text, out dotSize) || dotSize<0)
            {
                MessageBox.Show("Błędna wartość rozmiaru kropki minucji, wybrana domyślna - 3");
                dotSize = 3;
            }


            for(int y=1; y<wBmp.Height; y++)
            {
                for(int x=1; x<wBmp.Width; x++)
                {
                    if(tab_minutiae[y,x] == true)
                    {
                        for (int dy = -dotSize / 2; dy < dotSize / 2 || dy<1; dy++)
                        {
                            for (int dx = -dotSize / 2; dx < dotSize / 2 || dx<1; dx++)
                            {
                                wBmp.SetPixel(x+dx, y+dy, (byte)255, (byte)0, (byte)0);
                            }
                        }
                        
                    }
                }
            }



            imgPhoto.Source = wBmp;
            Calculating.Val = false;
        }



        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            clickPointUp = e.GetPosition(imgPhoto);
            if (imgPhoto != null && imgPhoto.Source != null && clickPointUp.Y > 0 && clickPointDown.Y > 0 && editPixel == false
                && movingImage == true)
            {
                movedTo.X += clickPointUp.X - clickPointDown.X;
                movedTo.Y += clickPointUp.Y - clickPointDown.Y;
                MoveTo(imgPhoto, movedTo.X, movedTo.Y);
                MoveTo(imgPhotoOriginal, movedTo.X, movedTo.Y);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            clickPointDown = e.GetPosition(imgPhoto);
            if (imgPhoto != null && imgPhoto.Source != null && editPixel == true)
            {

                //edycja Piksela i przypisanie mu mapy
                wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);

                if (imgPhoto.IsMouseDirectlyOver)
                {
                    wBmp.SetPixel((int)clickPointDown.X, (int)clickPointDown.Y, pickedColor);
                }
                imgPhoto.Source = wBmp;
            }
            if (imgPhoto != null && imgPhoto.Source != null && imgPhoto.IsMouseDirectlyOver)
            {
                if (editPixel == false && readPixel == false)
                {
                    movingImage = true;
                }
                else if (editPixel == false && readPixel == true)
                {
                    wBmp = new WriteableBitmap((BitmapSource)imgPhoto.Source);
                    System.Windows.Media.Color color = wBmp.GetPixel((int)clickPointDown.X, (int)clickPointDown.Y);
                    MessageBox.Show("Piksel (" + (int)clickPointDown.X + "," + (int)clickPointDown.Y + ") R"
                        + color.R + " G" + color.G + " B" + color.B + " A" + color.A);
                }
            }
        }
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            movingImage = false;
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        //Zamykanie strumienia
        //https://msdn.microsoft.com/en-us/library/aa328800(v=vs.71).aspx

        //https://stackoverflow.com/questions/15779564/resize-image-in-wpf
        //https://stackoverflow.com/questions/4214155/wpf-easiest-way-to-move-image-to-x-y-programmatically

        //Scale Transform
        //http://www.c-sharpcorner.com/uploadfile/mahesh/scaletransform-in-wpf/

        //WriteableBitmapEx
        //nuget PM> Install-Package WriteableBitmapEx -Version 1.5.1
        //https://archive.codeplex.com/?p=writeablebitmapex

        //Extended.Wpf.Toolkit (ColorPicker)
        //https://stackoverflow.com/questions/17089382/wpf-color-picker-implementation/17090082
        //https://wpftoolkit.codeplex.com/documentation
        //nuget PM> Install-Package Extended.Wpf.Toolkit -Version 3.3.0

        //Wczytywanie gifa do tablicy bajtów
        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/40e8fcdc-94b9-4e5a-a517-62514a2f8874/how-to-read-a-gif-file?forum=csharpgeneral
        //bajty do imagesource
        //https://stackoverflow.com/questions/30727343/fast-converting-bitmap-to-bitmapsource-wpf


        //Zadanie 3
        //Otsu's method explained
        //http://www.labbookpages.co.uk/software/imgProc/otsuThreshold.html
        //Niblack local thresholding MATLAB
        //https://www.researchgate.net/publication/221253803_Comparison_of_Niblack_inspired_Binarization_Methods_for_Ancient_Documents


        //Zadanie 4
        //Filtr konwolucyjny
        //https://4programmers.net/Forum/C_i_C++/60425-Filtr_konwolucyjny

        //Poprawnie działające zadanie 4.1 http://wklej.org/id/3432022/

        //Zadanie grupowe 1
        //Usuwanie minucji
        //https://www.researchgate.net/publication/256470785_Removal_of_False_Minutiae_with_Modified_Fuzzy_Rules
    }
}