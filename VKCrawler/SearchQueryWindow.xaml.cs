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

namespace VKCrawler
{
    /// <summary>
    /// Interaction logic for SearchQueryWindow.xaml
    /// </summary>
    public partial class SearchQueryWindow : Window
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string mobilePhone { get; set; }
        public event EventHandler SendQuery;
        public SearchQueryWindow()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            firstName = tbFirstName.Text;
            lastName = tbLastName.Text;
            mobilePhone = tbMobilePhone.Text;
            SendQuery(this, EventArgs.Empty);
            this.Close();
        }
    }
}
