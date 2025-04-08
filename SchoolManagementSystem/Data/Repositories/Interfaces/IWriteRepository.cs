namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface chỉ chứa các phương thức ghi dữ liệu
    public interface IWriteRepository<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        void Delete(string id);
        void Save();
    }
}