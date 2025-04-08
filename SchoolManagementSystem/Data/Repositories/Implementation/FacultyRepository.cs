using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class FacultyRepository : IFacultyRepository
    {
        private readonly string _filePath;
        private List<Faculty> _faculty;

        public FacultyRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "faculty.csv");
            LoadData();
        }

        private void LoadData()
        {
            _faculty = CsvFileHandler.ReadCsvFile<Faculty>(_filePath).ToList();
        }

        public IEnumerable<Faculty> GetAll()
        {
            return _faculty.ToList();
        }

        public Faculty GetById(string id)
        {
            return _faculty.FirstOrDefault(f => f.FacultyId == id);
        }

        public Faculty GetByUserId(string userId)
        {
            return _faculty.FirstOrDefault(f => f.UserId == userId);
        }

        public void Add(Faculty entity)
        {
            _faculty.Add(entity);
        }

        public void Update(Faculty entity)
        {
            var index = _faculty.FindIndex(f => f.FacultyId == entity.FacultyId);
            if (index >= 0)
            {
                _faculty[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var faculty = GetById(id);
            if (faculty != null)
            {
                _faculty.Remove(faculty);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _faculty);
        }
    }
}