using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Collections.Specialized;
using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace VKCrawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private string accessToken = "";  //токен для авторизации
        AuthWindow aWnd;
        CancellationTokenSource cts;
        PauseTokenSource pts;
        //Getting authorize token from auth window via event listener
        private void AuthHandler(object sender, EventArgs e)
        {
            accessToken = aWnd.accessToken;
        }
        //Code for queryng users via "execute" method
        private string makeExecuteCode(int age, int bday, int country_id, int city_id)
        {
            return "var users;"                                                                             //API.users.search returns first 1000 results no matter the offset
                        + "var sex = 1;"                                                                    //in order to get maximum amount of entries we create separate queries for each possible birth day and month
                        + "while(sex %3C= 2){"                                                              //for given age and sex
                        + "var bmonth = 1;"
                        + "while(bmonth %3C= 12) {"
                        + "var tmp = API.users.search({ \"v\":\"5.34\", \"count\":\"1000\",\"sort\":\"0\","
                        + "\"sex\":sex,"
                        + "\"fields\":\"screen_name,sex,bdate,contacts\","
                        + "\"country\":" + country_id.ToString() + ","
                        + "\"city\":" + city_id.ToString() + ","
                        + "\"age_from\":" + age.ToString() + ","
                        + "\"age_to\":" + age.ToString() + ","
                        + "\"birth_day\":" + bday.ToString() + ","
                        + "\"birth_month\":bmonth}).items;"
                        + "if(bmonth == 1 ){"
                        + "if(sex == 1 ){users = tmp;} else{ users = users %2B tmp;}}"
                        + "else{users = users %2B tmp;}"
                        + "bmonth=bmonth %2B 1;}"
                        + "sex = sex %2B 1;}"
                        + "return users;";
        }
        //Progressbar update
        void ReportProgress(ProgressContext progress)
        {
            if (pbProgress.Maximum != progress.max)
                pbProgress.Maximum = progress.max;
            pbProgress.Value = progress.value;
            tbProgress.Text = "Возраст: "+progress.age+"; число: "+progress.bDay+"; "+progress.max+" записей.";
        }
        //Parsing XML with user data
        private async Task<int> parseUsersXml(int age, int bDay, int country_id, int city_id, XmlNodeList usersXml, SQLiteConnection conn, IProgress<ProgressContext> progress, CancellationToken ct, PauseToken pt)
        {
            int count = 0;
            foreach (XmlNode userXml in usersXml)
            {
                await pt.WaitWhilePausedAsync();
                int id;
                if (userXml["uid"] != null)
                {
                    id = Int32.Parse(userXml["uid"].InnerText);
                }
                else
                {
                    id = Int32.Parse(userXml["id"].InnerText);
                }
                string firstName = userXml["first_name"].InnerText;
                string lastName = userXml["last_name"].InnerText;
                int sexId = Int32.Parse(userXml["sex"].InnerText);
                DateTime bDate;
                if (userXml["bdate"] != null)
                    bDate = dateParse(userXml["bdate"].InnerText, age);
                else
                    bDate = dateParse(null, age);
                string mobilePhone = "";
                if (userXml["mobile_phone"] != null)
                    mobilePhone = userXml["mobile_phone"].InnerText;
                await Task.Run(() =>
                {
                    try
                    {
                        conn.Insert(new User()
                        {
                            id = id,
                            countryId = country_id,
                            cityId = city_id,
                            firstName = firstName,
                            lastName = lastName,
                            sexId = sexId,
                            bDate = bDate,
                            mobilePhone = mobilePhone
                        });
                        ct.ThrowIfCancellationRequested();
                    }
                    catch (SQLiteException e) { }
                    catch (OperationCanceledException e) { throw e; }
                }, ct);
                progress.Report(new ProgressContext() { max = usersXml.Count, value = count, bDay = bDay, age = age});
                count++;
            }
            return count;
        }

        //Querying users and populating database
        private async Task<int> fillUserTable(int country_id, int city_id, int ageMin, int ageMax, SQLiteConnection conn, IProgress<ProgressContext> progress, CancellationToken ct, PauseToken pt)
        {
            int count = 0;
            for (int age = ageMin; age <= ageMax; age++)
            {
                for (int bDay = 1; bDay <= 31; bDay++)
                {
                    await Task.Delay(300);
                    await pt.WaitWhilePausedAsync();
                    NameValueCollection qs = new NameValueCollection();
                    qs["count"] = "1000";
                    qs["birth_day"] = bDay.ToString();
                    qs["age_from"] = age.ToString();
                    qs["age_to"] = age.ToString();
                    qs["country"] = country_id.ToString();
                    qs["city"] = city_id.ToString();
                    qs["fields"] = "screen_name,sex,bdate,contacts";
                    XmlDocument doc = ExecuteCommand("users.search", qs); 
                    XmlNode response;
                    int responseCount;
                    try
                    {
                        response = doc.DocumentElement.SelectSingleNode("/response");
                        responseCount = Int32.Parse(response.SelectSingleNode("count").InnerText);
                    }
                    catch (Exception e)
                    {
                        progress.Report(new ProgressContext() { max = 0, value = 0, bDay = bDay, age = age });
                        continue; 
                    }
                    if (responseCount > 1000)
                    {
                        qs = new NameValueCollection();
                        qs["code"] = makeExecuteCode(age, bDay, country_id, city_id);
                        doc = ExecuteCommand("execute", qs);
                        try
                        {
                            response = doc.DocumentElement.SelectSingleNode("/response");
                        }
                        catch (Exception e)
                        {
                            progress.Report(new ProgressContext() { max = responseCount, value = 0, bDay = bDay, age = age });
                            continue;
                        }
                        XmlNodeList usersXml = response.ChildNodes;
                        if (usersXml.Count != 0)
                        {
                            try
                            {
                                count += await parseUsersXml(age, bDay, country_id, city_id, usersXml, conn, progress, ct, pt);
                            }
                            catch (SQLiteException e) { }
                            catch (OperationCanceledException e)
                            {
                                throw e;
                            }
                        }
                    }
                    else if (responseCount > 0 && responseCount <= 1000)
                    {
                        response.RemoveChild(response.FirstChild);
                        XmlNodeList usersXml = response.ChildNodes;
                        if (usersXml.Count != 0)
                        {
                            try
                            {
                                count += await parseUsersXml(age, bDay, country_id, city_id, usersXml, conn, progress, ct, pt);
                            }
                            catch (SQLiteException e) { }
                            catch (OperationCanceledException e)
                            {
                                throw e;
                            }
                        }
                    }
                    else
                    {
                        progress.Report(new ProgressContext() { max = responseCount, value = 0, bDay = bDay, age = age });
                        continue;
                    }
                }
            }
            return count;
        }

        //Parsing date string
        private DateTime dateParse(string dateString, int targetAge)
        {
            DateTime result = new DateTime();
            if (dateString == null)
            {
                result = DateTime.Parse("1.1." + (DateTime.Now.Year - targetAge).ToString());
            }
            else
            {
                Regex myReg = new Regex(@"(\d+)");
                int bDay, bMonth, bYear;
                MatchCollection m = myReg.Matches(dateString);
                bDay = Int32.Parse(m[0].Value);
                bMonth = Int32.Parse(m[1].Value);
                if(m[1].Value != null)
                {
                    if (bMonth > DateTime.Now.Month)
                        bYear = DateTime.Now.Year - targetAge - 1;
                    else if (bMonth == DateTime.Now.Month && bDay >= DateTime.Now.Day)
                        bYear = DateTime.Now.Year - targetAge - 1;
                    else
                        bYear = DateTime.Now.Year - targetAge;
                }
                else
                {
                    bYear = Int32.Parse(m[2].Value);
                }
                //implement check for day going out of bounds
                try
                {
                    result = DateTime.Parse(bDay.ToString() + "." + bMonth.ToString() + "." + bYear.ToString());
                }
                catch (Exception e)
                {
                    switch (bMonth)
                    {
                        case 2:
                            if (DateTime.IsLeapYear(bYear))
                                bDay = 29;
                            else
                                bDay = 28;
                            break;
                        case 1:
                        case 3:
                        case 5:
                        case 6:
                        case 7:
                        case 10:
                        case 12:
                            bDay = 31;
                            break;
                        default:
                            bDay = 30;
                            break;
                    }
                    result = DateTime.Parse(bDay.ToString() + "." + bMonth.ToString() + "." + bYear.ToString());
                }
            }
            return result;
        }

        public SearchWindow()
        {
            InitializeComponent();
        }

        //checks if table exists in database
        public static bool tableExists<T>(SQLiteConnection conn)
        {
            const string cmdText = "SELECT name FROM sqlite_master WHERE type='table' AND name=?";
            var cmd = conn.CreateCommand(cmdText, typeof(T).Name);
            return cmd.ExecuteScalar<string>() != null;
        }

        //generic VK API call method
        private XmlDocument ExecuteCommand(string name, NameValueCollection qs)
        {
            XmlDocument result = new XmlDocument();
            result.Load(String.Format("https://api.vk.com/method/{0}.xml?{1}&lang=ru&access_token={2}", name, String.Join("&", from item in qs.AllKeys select item + "=" + qs[item]), accessToken));
            return result;
        }

        //input check (numeric only)
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //searches for symbols not matching 0-9
            return !regex.IsMatch(text);
        }
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        //populating sex table
        private async Task fillSexTable(SQLiteConnection conn)
        {
            await Task.Run(() =>
            {
                try
                {
                    conn.Insert(new Sex()
                    {
                        id = 1,
                        title = "женский"
                    });
                    conn.Insert(new Sex()
                    {
                        id = 2,
                        title = "мужской"
                    });
                }
                catch (SQLiteException e) { }
            });
        }
        //querying countries and populating database
        private async Task fillCountryTable(SQLiteConnection conn)
        {
            NameValueCollection qs = new NameValueCollection();
            qs["need_all"] = "1";
            qs["count"] = "1000";
            qs["offset"] = "0";
            while (true)
            {
                XmlDocument doc = ExecuteCommand("database.getCountries", qs);
                XmlNode response = doc.DocumentElement.SelectSingleNode("/response");
                XmlNodeList countriesXml = response.ChildNodes;
                if (countriesXml.Count != 0)
                {
                    foreach (XmlNode countryXml in countriesXml)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                conn.Insert(new Country()
                                {
                                    id = Int32.Parse(countryXml["cid"].InnerText),
                                    title = countryXml["title"].InnerText
                                });
                            }
                            catch (SQLiteException e) { }
                        });
                    }
                }
                else
                    break;
                qs["offset"] = (int.Parse(qs["offset"].ToString()) + 1000).ToString();
            }
        }

        //querying regions and populating database
        private async Task fillRegionTable(int countryId, SQLiteConnection conn)
        {
            NameValueCollection qs = new NameValueCollection();
            qs["country_id"] = countryId.ToString();
            qs["count"] = "1000";
            qs["offset"] = "0";
            while (true)
            {
                XmlDocument doc = ExecuteCommand("database.getRegions", qs);
                XmlNode response = doc.DocumentElement.SelectSingleNode("/response");
                XmlNodeList regionsXml = response.ChildNodes;
                if (regionsXml.Count != 0)
                {
                    foreach (XmlNode regionXml in regionsXml)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                conn.Insert(new Region()
                                {
                                    id = Int32.Parse(regionXml["region_id"].InnerText),
                                    countryId = countryId,
                                    title = regionXml["title"].InnerText
                                });
                            }
                            catch (SQLiteException e) { }
                        });
                    }
                }
                else
                    break;
                qs["offset"] = (int.Parse(qs["offset"].ToString()) + 1000).ToString();
            }
        }

        //querying cities and populating database
        private async Task fillCityTable(int countryId, int regionId, SQLiteConnection conn)
        {
            NameValueCollection qs = new NameValueCollection();
            qs["country_id"] = countryId.ToString();
            qs["region_id"] = regionId.ToString();
            qs["count"] = "1000";
            if (qs["region_id"] != "0")
            {
                qs["need_all"] = "1";
            }
            qs["offset"] = "0";
            while (true)
            {
                XmlDocument doc = ExecuteCommand("database.getCities", qs);
                XmlNode response = doc.DocumentElement.SelectSingleNode("/response");
                XmlNodeList citiesXml = response.ChildNodes;
                if (citiesXml.Count != 0)
                {
                    foreach (XmlNode cityXml in citiesXml)
                    {
                        await Task.Run(() => 
                        {   
                            try
                            {
                                if (qs["region_id"] == "0")
                                {
                                    if (cityXml["important"].InnerText.Equals("1"))     //querying cities with no "regionid" parameter returns special status cities ("important"=1)
                                    {                                                   //in internal VK database they are not assigned to any regionid and simply don't have the parameter
                                        conn.Insert(new City()                          //locally we assign them regionid of 0
                                        {
                                            id = Int32.Parse(cityXml["cid"].InnerText),
                                            countryId = countryId,
                                            regionId = regionId,
                                            title = cityXml["title"].InnerText
                                        });
                                    }
                                }
                                else
                                    conn.Insert(new City()
                                    {
                                        id = Int32.Parse(cityXml["cid"].InnerText),
                                        countryId = countryId,
                                        regionId = regionId,
                                        title = cityXml["title"].InnerText
                                    });
                            }
                            catch (Exception e) { }
                        });
                    }
                }
                else
                    break;
                qs["offset"] = (int.Parse(qs["offset"].ToString()) + 1000).ToString();
            }
        }

        private async void wndSearch_Loaded(object sender, RoutedEventArgs e)
        {
            //new database connection
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            if (!tableExists<Country>(conn))
            {
                //table doesn't exist
                conn.CreateTable<Country>();
            }
            await fillCountryTable(conn);
            if (!tableExists<Sex>(conn))
            {
                conn.CreateTable<Sex>();
            }
            await fillSexTable(conn);
            cbCountry.ItemsSource = conn.Table<Country>().OrderBy(v => v.title);
            cbCountry.IsEnabled = true;
        }

        private async void cbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbCountry.IsEnabled = false;
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            if (!tableExists<Region>(conn))
            {
                //table doesn't exist
                conn.CreateTable<Region>();
            }
            await fillRegionTable(((Country)cbCountry.SelectedItem).id, conn);
            cbRegion.ItemsSource = conn.Table<Region>()
                .Where(v => v.countryId.Equals(((Country)cbCountry.SelectedItem).id))
                .Concat(new[] { new Region { id = 0, countryId = ((Country)cbCountry.SelectedItem).id, title = "Города особого значения"} })
                .OrderBy(v => v.title);
            cbCountry.IsEnabled = true;
            cbRegion.IsEnabled = true;
        }
        
        private async void cbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbRegion.IsEnabled = false;
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            if (!tableExists<City>(conn))
            {
                //table doesn't exist
                conn.CreateTable<City>();
            }
            await fillCityTable(((Country)cbCountry.SelectedItem).id, ((Region)cbRegion.SelectedItem).id, conn);
            cbCity.ItemsSource = conn.Table<City>()
                .Where(v => v.countryId.Equals(((Country)cbCountry.SelectedItem).id))
                .Where(v => v.regionId.Equals(((Region)cbRegion.SelectedItem).id))
                .OrderBy(v => v.title);
            cbRegion.IsEnabled = true;
            cbCity.IsEnabled = true;
        }
        
        private void cbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnUsersSearch.IsEnabled = true;
        }

        //Run user search
        private async void btnUsersSearch_Click(object sender, RoutedEventArgs e)
        {
            btnUsersSearch.IsEnabled = false;
            var progressIndicator = new Progress<ProgressContext>(ReportProgress);
            var conn = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "VKCrawler.db");
            if (!tableExists<User>(conn))
            {
                //table doesn't exist
                conn.CreateTable<User>();
            }
            if (tbAgeMin.Text == "" || tbAgeMax.Text == "" || Int32.Parse(tbAgeMax.Text) < Int32.Parse(tbAgeMin.Text))
            {
                MessageBox.Show("Неправильное ограничение по возрасту!");
            }
            else if (accessToken == "")
            {
                MessageBox.Show("Авторизация не пройдена!");
            }
            else
            {
                cts = new CancellationTokenSource();
                pts = new PauseTokenSource();
                int resultCount = 0;
                btnCancel.IsEnabled = true;
                btnPause.IsEnabled = true;
                try
                {
                    resultCount = await fillUserTable(((Country)cbCountry.SelectedItem).id, ((City)cbCity.SelectedItem).id, Int32.Parse(tbAgeMin.Text), Int32.Parse(tbAgeMax.Text), conn, progressIndicator, cts.Token, pts.Token);
                }
                catch (OperationCanceledException ex)
                {
                    MessageBox.Show("Операция прервана");
                }
                MessageBox.Show("Обработано " + resultCount.ToString() + " записей");
            }
            btnUsersSearch.IsEnabled = true;
            btnCancel.IsEnabled = false;
            btnPause.IsEnabled = false;
        }

        private void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            aWnd = new AuthWindow();
            aWnd.SendToken += new EventHandler(AuthHandler);
            aWnd.Show();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            pts.IsPaused = !pts.IsPaused;
            if (pts.IsPaused)
            {
                pbProgress.Foreground = Brushes.Yellow;
                btnPause.Content = "Возобновить";
            }
            else
            {
                pbProgress.Foreground = Brushes.Green;
                btnPause.Content = "Пауза";
            }
        }
    }
}
