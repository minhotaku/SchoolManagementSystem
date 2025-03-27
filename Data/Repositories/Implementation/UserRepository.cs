using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    // Class triển khai interface IUserRepository và tuân thủ SRP
    public class UserRepository : IUserRepository
    {
        private readonly string _filePath;
        private List<User> _users;

        public UserRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "users.csv");
            LoadData();
        }

        private void LoadData()
        {
            _users = CsvFileHandler.ReadCsvFile<User>(_filePath).ToList();
        }

        public IEnumerable<User> GetAll()
        {
            return _users.ToList();
        }

        public User GetById(string id)
        {
            return _users.FirstOrDefault(u => u.UserId == id);
        }

        public User GetByUsername(string username)
        {
            return _users.FirstOrDefault(u => u.Username == username);
        }

        public IEnumerable<User> GetByRole(string role)
        {
            return _users.Where(u => u.Role == role).ToList();
        }

        public void Add(User entity)
        {
            _users.Add(entity);
        }

        public void Update(User entity)
        {
            var index = _users.FindIndex(u => u.UserId == entity.UserId);
            if (index >= 0)
            {
                _users[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var user = GetById(id);
            if (user != null)
            {
                _users.Remove(user);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _users);
        }
    }
}