using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class Country : BaseEntity
    {
        public Country() { }

        public Country(Country country)
        {
            Id = country.Id;
            Name = country.Name;

            if (country.Cities != null)
            {
                Cities = new List<City>(country.Cities);
            }
            else
            {
                Cities = new List<City>();
            }
        }

        //One to many relationship with City
        [JsonIgnore]
        public List<City>? Cities { get; set; }
    }
}
