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
using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform;

namespace VKCrawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataWindow dWnd;
        SearchWindow sWnd;
        public MainWindow()
        {
            InitializeComponent();
        }
        public static bool tableExists<T>(SQLiteConnection conn)
        {
            const string cmdText = "SELECT name FROM sqlite_master WHERE type='table' AND name=?";
            var cmd = conn.CreateCommand(cmdText, typeof(T).Name);
            return cmd.ExecuteScalar<string>() != null;
        }
        private void btnDataWindow_Click(object sender, RoutedEventArgs e)
        {
            dWnd = new DataWindow();
            dWnd.Show();
        }

        private void btnSearchWindow_Click(object sender, RoutedEventArgs e)
        {
            sWnd = new SearchWindow();
            sWnd.Show();
        }

        private void wndMain_Loaded(object sender, RoutedEventArgs e)
        {
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            if(tableExists<User>(conn))
                tbDBInfo.Text = conn.Table<User>().Count().ToString() + " записей в базе";
        }
    }
}
