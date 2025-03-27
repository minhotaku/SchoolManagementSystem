using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class AdminRepository : IAdminRepository
    {
        private readonly string _filePath;
        private List<Admin> _admins;

        public AdminRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "admins.csv");
            LoadData();
        }

        private void LoadData()
        {
            _admins = CsvFileHandler.ReadCsvFile<Admin>(_filePath).ToList();
        }

        public IEnumerable<Admin> GetAll()
        {
            return _admins.ToList();
        }

        public Admin GetById(string id)
        {
            return _admins.FirstOrDefault(a => a.AdminId == id);
        }

        public Admin GetByUserId(string userId)
        {
            return _admins.FirstOrDefault(a => a.UserId == userId);
        }

        public void Add(Admin entity)
        {
            _admins.Add(entity);
        }

        public void Update(Admin entity)
        {
            var index = _admins.FindIndex(a => a.AdminId == entity.AdminId);
            if (index >= 0)
            {
                _admins[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var admin = GetById(id);
            if (admin != null)
            {
                _admins.Remove(admin);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _admins);
        }
    }
}