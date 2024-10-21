using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class Department : BaseEntity
    {
        public Department()
        {
        }

        public Department(Department department)
        {
            Id = department.Id;
            Name = department.Name;
            GeneralDepartment = department.GeneralDepartment;
            GeneralDepartmentId = department.GeneralDepartmentId;

            // Skopiuj listę Branches, jeśli istnieje
            if (department.Branches != null)
            {
                Branches = new List<Branch>(department.Branches);
            }
            else
            {
                Branches = new List<Branch>();
            }
        }

        //Many to one relationship with General Department
        public GeneralDepartment? GeneralDepartment { get; set; }
        public int GeneralDepartmentId { get; set; }

        //One to many relationship with Branch
        [JsonIgnore]
        public List<Branch>? Branches { get; set; }
    }
}
