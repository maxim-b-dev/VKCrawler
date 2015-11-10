using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace VKCrawler
{
    public class Country
    {
        [PrimaryKey]
        public int id { get; set; }

        [MaxLength(40)]
        public string title { get;set; }

        [OneToMany]
        public List<Region> regions { get; set; }

        [OneToMany]
        public List<City> cities { get; set; }
    }
    public class Region
    {
        [PrimaryKey]
        public int id { get; set; }

        [ForeignKey(typeof(Country))]
        public int countryId { get; set; }

        [MaxLength(40)]
        public string title { get; set; }

        [ManyToOne]
        public Country country { get; set; }

        [OneToMany]
        public List<City> cities { get; set; }
    }
    public class City
    {
        [PrimaryKey]
        public int id { get; set; }

        [ForeignKey(typeof(Country))]
        public int countryId { get; set; }

        [ForeignKey(typeof(Region))]
        public int regionId { get; set; }

        [MaxLength(40)]
        public string title { get; set; }

        [ManyToOne]
        public Country country { get; set; }

        [ManyToOne]
        public Region region { get; set; }
        [OneToMany]
        public List<User> users { get; set; }
    }
    public class Sex
    {
        [PrimaryKey]
        public int id { get; set; }

        [MaxLength(15)]
        public string title { get; set; }
        [OneToMany]
        public List<User> users { get; set; }
    }
    public class User
    {
        [PrimaryKey]
        public int id { get; set; }

        [ForeignKey(typeof(Country))]
        public int countryId { get; set; }

        [ForeignKey(typeof(City))]
        public int cityId { get; set; }

        [MaxLength(20)]
        public string firstName { get; set; }

        [MaxLength(30)]
        public string lastName { get; set; }

        [ForeignKey(typeof(Sex))]
        public int sexId { get; set; }
        public DateTime bDate { get; set; }

        [MaxLength(30)]
        public string mobilePhone { get; set; }
    }
}
