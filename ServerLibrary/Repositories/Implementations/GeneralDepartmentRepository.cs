using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations
{
    public class GeneralDepartmentRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<GeneralDepartment>
    {
        public async Task<List<GeneralDepartment>> GetAll() => await appDbContext.GeneralDepartment.ToListAsync();

        public async Task<GeneralDepartment> GetById(int id) => await appDbContext.GeneralDepartment.FindAsync(id);

        public async Task<GeneralResponse> Insert(GeneralDepartment item)
        {
            var checkIfNull = await CheckName(item.Name!);
            if (!checkIfNull) return new GeneralResponse(false, "General department already added");

            appDbContext.GeneralDepartment.Add(item);
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> Update(GeneralDepartment item)
        {
            var department = await appDbContext.GeneralDepartment.FindAsync(item.Id);
            if(department == null) return NotFound();

            department.Name = item.Name;
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> DeleteById(int id)
        {
            var department = await appDbContext.GeneralDepartment.FindAsync(id);
            if(department == null) return NotFound();

            appDbContext.GeneralDepartment.Remove(department);
            await Commit();
            return Success();
        }

        private static GeneralResponse NotFound() => new(false, "Sorry general department not found");
        private static GeneralResponse Success() => new(true, "Process completed");
        private async Task Commit() => await appDbContext.SaveChangesAsync();
        private async Task<bool> CheckName(string name)
        {
            var item = await appDbContext.GeneralDepartment.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));

            return item is null;
        }
    }
}
