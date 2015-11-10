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
using System.Collections.Specialized;
using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform;
using System.Text.RegularExpressions;

namespace VKCrawler
{
    /// <summary>
    /// Interaction logic for DataWindow.xaml
    /// </summary>
    public partial class DataWindow : Window
    {
        SearchQueryWindow sqWnd;
        public DataWindow()
        {
            InitializeComponent();
        }
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //поиск символов не принадлежащих [0-9]
            return !regex.IsMatch(text);
        }
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        public class UserResult
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string sex { get; set; }
            public DateTime bDate { get; set; }
            public string country { get; set; }
            public string city { get; set; }
            public string mobilePhone { get; set; }
            public string pageAddress { get; set; }
        }
        private int fillDataGrid(int country_id, int city_id, SQLiteConnection conn)
        {
            dgResult.ItemsSource = conn.Query<UserResult>("SELECT 'User'.'firstName', 'User'.'lastName', 'Sex'.'title' as 'sex', 'User'.'bDate', 'Country'.'title' as 'country', 'City'.'title' as 'city', 'User'.'mobilePhone', printf('http://vk.com/id%d','User'.'id') AS 'pageAddress' FROM ((('User' JOIN 'Country' ON 'User'.'countryId' = 'Country'.'id') JOIN 'City' ON 'User'.'cityId' = 'City'.'id') JOIN 'Sex' ON 'User'.'sexId' = 'Sex'.'id') WHERE 'User'.'countryId' = ?  AND 'User'.'cityId' = ?", country_id, city_id)
                .Where(v => (tbAge.Text == "" ? true : Math.Floor(((DateTime.Now - v.bDate).TotalDays / 365.25)) == Int32.Parse(tbAge.Text)));
            return dgResult.Items.Count;
        }
        private void wndData_Loaded(object sender, RoutedEventArgs e)
        {
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            cbCountry.ItemsSource = conn.Table<Country>().OrderBy(v => v.title);
        }
        private void cbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCountry.SelectedIndex == -1) return;
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            cbRegion.ItemsSource = conn.Table<Region>()
                .Where(v => v.countryId.Equals(((Country)cbCountry.SelectedItem).id))
                .Concat(new[] { new Region { id = 0, countryId = ((Country)cbCountry.SelectedItem).id, title = "Города особого значения" } })
                .OrderBy(v => v.title);
            cbRegion.IsEnabled = true;
        }

        private void cbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbRegion.SelectedIndex == -1) return;
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            cbCity.ItemsSource = conn.Table<City>()
                .Where(v => v.countryId.Equals(((Country)cbCountry.SelectedItem).id))
                .Where(v => v.regionId.Equals(((Region)cbRegion.SelectedItem).id))
                .OrderBy(v => v.title);
            cbCity.IsEnabled = true;
        }

        private void cbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnApply.IsEnabled = true;
        }

        private async void btnApply_Click(object sender, RoutedEventArgs e)
        {
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            int resultCount = fillDataGrid(((Country)cbCountry.SelectedItem).id, ((City)cbCity.SelectedItem).id, conn);
            MessageBox.Show(resultCount.ToString() + " записей");
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            btnApply.IsEnabled = cbRegion.IsEnabled = cbCity.IsEnabled = false;
            cbCountry.SelectedIndex = cbRegion.SelectedIndex = cbCity.SelectedIndex = -1;
            tbAge.Text = "";
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            sqWnd = new SearchQueryWindow();
            sqWnd.SendQuery += new EventHandler(SearchQueryHandler);
            sqWnd.Show();
        }

        public void SearchQueryHandler(object sender, EventArgs e)
        {
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            dgResult.ItemsSource = conn.Query<UserResult>("SELECT 'User'.'firstName', 'User'.'lastName', 'Sex'.'title' as 'sex', 'User'.'bDate', 'Country'.'title' as 'country', 'City'.'title' as 'city', 'User'.'mobilePhone', printf('http://vk.com/id%d','User'.'id') AS 'pageAddress' FROM ((('User' JOIN 'Country' ON 'User'.'countryId' = 'Country'.'id') JOIN 'City' ON 'User'.'cityId' = 'City'.'id') JOIN 'Sex' ON 'User'.'sexId' = 'Sex'.'id')")
                .Where(v => (sqWnd.firstName == "" ? true : v.firstName.Contains(sqWnd.firstName)) &&
                            (sqWnd.lastName == "" ? true : v.lastName.Contains(sqWnd.lastName)) &&
                            (sqWnd.mobilePhone == "" ? true : v.mobilePhone.Contains(sqWnd.mobilePhone)));
            MessageBox.Show(dgResult.Items.Count.ToString() + " записей");
        }
    }
}
