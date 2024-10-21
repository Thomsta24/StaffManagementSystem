using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class Branch : BaseEntity
    {
        public Branch() { }

        public Branch(Branch branch)
        {
            Id = branch.Id;
            Name = branch.Name;
            Department = branch.Department;
            DepartmentId = branch.DepartmentId;

            if (branch.Employees != null)
            {
                Employees = new List<Employee>(branch.Employees);
            }
            else
            {
                Employees = new List<Employee>();
            }
        }

        //Many to one relationship with Department
        public Department? Department { get; set; }
        public int DepartmentId { get; set; }

        //Relationship: One to Many with Employee
        [JsonIgnore]
        public List<Employee>? Employees { get; set; }
    }
}
