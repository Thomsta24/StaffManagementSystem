﻿using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations
{
    public class EmployeeRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<Employee>
    {
        public async Task<List<Employee>> GetAll()
        {
            var employees = await appDbContext.Employees
                .AsNoTracking()
                .Include(t => t.Town)
                .ThenInclude(b => b.City)
                .ThenInclude(c => c.Country)
                .Include(b => b.Branch)
                .ThenInclude(d => d.Department)
                .ThenInclude(gd => gd.GeneralDepartment)
                .ToListAsync();

            return employees;
        }

        public async Task<Employee> GetById(int id)
        {
            var employee = await appDbContext.Employees
                .Include(t => t.Town)
                .ThenInclude(b => b.City)
                .ThenInclude(c => c.Country)
                .Include(b => b.Branch)
                .ThenInclude(d => d.Department)
                .ThenInclude(gd => gd.GeneralDepartment)
                .FirstOrDefaultAsync(ei => ei.Id == id)!;

            return employee!;
        }

        public async Task<GeneralResponse> Insert(Employee employee)
        {
            if (!await CheckName(employee.Name!)) return new GeneralResponse(false, "Employee already added");

            appDbContext.Employees.Add(employee);
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> Update(Employee employee)
        {
            var findUser = await appDbContext.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
            if (findUser == null) return new GeneralResponse(false, "Employee does not exist");

            findUser.Name = employee.Name;
            findUser.Other = employee.Other;
            findUser.Address = employee.Address;
            findUser.TelNumber = employee.TelNumber;
            findUser.BranchId = employee.BranchId;
            findUser.TownId = employee.TownId;
            findUser.CivilId = employee.CivilId;
            findUser.FileNumber = employee.FileNumber;
            findUser.JobName = employee.JobName;
            findUser.Photo = employee.Photo;

            await appDbContext.SaveChangesAsync();
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> DeleteById(int id)
        {
            var item = await appDbContext.Employees.FindAsync(id);
            if (item == null) return NotFound();

            appDbContext.Employees.Remove(item);
            await Commit();
            return Success();
        }

        private async Task Commit() => await appDbContext.SaveChangesAsync();

        private static GeneralResponse NotFound() => new(false, "Sorry, employee not found");
        private static GeneralResponse Success() => new(true, "Process completed");
        private async Task<bool> CheckName(string name)
        {
            var item = await appDbContext.Employees.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
            return item == null ? true : false;
        }
    }
}