using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class GeneralDepartment : BaseEntity
    {
        public GeneralDepartment()
        {
        }

        public GeneralDepartment(GeneralDepartment department)
        {
            Id = department.Id;
            Name = department.Name;

            if (department.Departments != null)
            {
                Departments = new List<Department>(department.Departments);
            }
            else
            {
                Departments = new List<Department>();
            }
        }

        // One to many relationship with Department
        [JsonIgnore]
        public List<Department>? Departments { get; set; }
    }
}
