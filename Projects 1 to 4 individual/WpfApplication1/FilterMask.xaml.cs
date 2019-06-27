using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for FilterMask.xaml
    /// </summary>
    public partial class FilterMask : Window
    {
        //https://stackoverflow.com/questions/665299/are-2-dimensional-lists-possible-in-c
        //https://stackoverflow.com/questions/276808/how-to-populate-a-wpf-grid-based-on-a-2-dimensional-array
        //http://www.thinkbottomup.com.au/site/blog/Game_of_Life_in_XAML_WPF_using_embedded_Python
        //https://docs.microsoft.com/pl-pl/dotnet/framework/wpf/advanced/how-to-build-a-table-programmatically#example-5
        //https://stackoverflow.com/questions/8575189/equivalent-of-getters-setters-for-collections-within-a-class
        public class TableItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged(string propName)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }

            private int _tableItem;

            public int TI
            {

                get { return _tableItem; }
                set
                {
                    _tableItem = value;
                    this.NotifyPropertyChanged("TI");
                }
            }

            public TableItem(int TI)
            {
                this.TI = TI;
            }
        }

        public List<List<TableItem>> Table { get; set; }

        public FilterMask(int size)
        {

            Table = new List<List<TableItem>>();
            List<TableItem> tmpTable = new List<TableItem>();
            for (int y = 0; y < size; y++)
            {
                tmpTable = new List<TableItem>();
                for (int x = 0; x < size; x++)
                {
                    tmpTable.Add(new TableItem(0));
                }
                Table.Add(tmpTable);
            }

            this.DataContext = this;

            InitializeComponent();


        }

        private void BT_Click1(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).DialogResult = true;
            Window.GetWindow(this).Close();
        }
        private void BT_Close(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        //https://stackoverflow.com/questions/3468433/wpf-window-return-value
    }
}
