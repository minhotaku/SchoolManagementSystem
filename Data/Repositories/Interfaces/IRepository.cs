namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface gốc kết hợp các interface nhỏ hơn
    public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : class
    {
    }
}