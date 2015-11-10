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
using System.Text.RegularExpressions;

namespace VKCrawler
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        private int scope = (int)(0);
        private int appId = 4979798;
        public string accessToken { get; set; }
        public event EventHandler SendToken;
        public AuthWindow()
        {
            InitializeComponent();
            wbAuth.Navigate(String.Format("https://oauth.vk.com/authorize?client_id={0}&scope={1}&display=popup&response_type=token", appId, scope));
        }

        private void wbAuth_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Uri.ToString().IndexOf("access_token") != -1)
            {
                int userId = 0;
                Regex myReg = new Regex(@"(?<name>[\w\d\x5f]+)=(?<value>[^\x26\s]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in myReg.Matches(e.Uri.ToString()))
                {
                    if (m.Groups["name"].Value == "access_token")
                    {
                        accessToken = m.Groups["value"].Value;
                    }
                    else if (m.Groups["name"].Value == "user_id")
                    {
                        userId = Convert.ToInt32(m.Groups["value"].Value);
                    }
                }
                if (SendToken != null)
                    SendToken(this, EventArgs.Empty);
                this.Close();
            }
        }

    }
}
