using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class Town : BaseEntity
    {
        public Town() { }

        public Town(Town town)
        {
            Id = town.Id;
            Name = town.Name;
            City = town.City;
            CityId = town.CityId;

            if (town.Employees != null)
            {
                Employees = new List<Employee>(town.Employees);
            }
            else
            {
                Employees = new List<Employee>();
            }
        }

        //Many to one relationship with City
        public City? City { get; set; }
        public int CityId { get; set; }

        //Relationship: One to Many with Employee
        [JsonIgnore]
        public List<Employee>? Employees { get; set; }
    }
}
