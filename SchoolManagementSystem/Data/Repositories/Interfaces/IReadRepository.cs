using System.Collections.Generic;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface chỉ chứa các phương thức đọc dữ liệu
    public interface IReadRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T GetById(string id);
    }
}