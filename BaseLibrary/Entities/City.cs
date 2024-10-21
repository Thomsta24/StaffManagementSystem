using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class City : BaseEntity
    {
        public City() { }

        public City(City city)
        {
            Id = city.Id;
            Name = city.Name;
            Country = city.Country;
            CountryId = city.CountryId;

            if (city.Towns != null)
            {
                Towns = new List<Town>(city.Towns);
            }
            else
            {
                Towns = new List<Town>();
            }
        }

        //Many to one relationship with Country
        public Country? Country { get; set; }
        public int CountryId { get; set; }

        //One to many relationship with Town
        [JsonIgnore]
        public List<Town>? Towns { get; set; }
    }
}
