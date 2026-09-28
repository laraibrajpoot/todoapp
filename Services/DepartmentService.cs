using todolist.Models;

namespace todolist.Services
{
    public class DepartmentService
    {
        // Replace with your actual DbContext or data storage logic
        private readonly List<Department> _departments = new();

        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            return await Task.FromResult(_departments);
        }

        public async Task AddDepartmentAsync(Department department)
        {
            department.Id = _departments.Any() ? _departments.Max(d => d.Id) + 1 : 1;
            _departments.Add(department);
            await Task.CompletedTask;
        }

        public async Task UpdateDepartmentAsync(Department department)
        {
            var existing = _departments.FirstOrDefault(d => d.Id == department.Id);
            if (existing != null)
            {
                existing.Name = department.Name;
                existing.ManagerName = department.ManagerName;
                existing.Description = department.Description;
            }
            await Task.CompletedTask;
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            var existing = _departments.FirstOrDefault(d => d.Id == id);
            if (existing != null)
            {
                _departments.Remove(existing);
            }
            await Task.CompletedTask;
        }
    }
}