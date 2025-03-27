using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class GradeRepository : IGradeRepository
    {
        private readonly string _filePath;
        private List<Grade> _grades;

        public GradeRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "grades.csv");
            LoadData();
        }

        private void LoadData()
        {
            _grades = CsvFileHandler.ReadCsvFile<Grade>(_filePath).ToList();
        }

        public IEnumerable<Grade> GetAll()
        {
            return _grades.ToList();
        }

        public Grade GetById(string id)
        {
            return _grades.FirstOrDefault(g => g.GradeId == id);
        }

        public IEnumerable<Grade> GetByEnrollment(string enrollmentId)
        {
            return _grades.Where(g => g.EnrollmentId == enrollmentId).ToList();
        }

        public void Add(Grade entity)
        {
            _grades.Add(entity);
        }

        public void Update(Grade entity)
        {
            var index = _grades.FindIndex(g => g.GradeId == entity.GradeId);
            if (index >= 0)
            {
                _grades[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var grade = GetById(id);
            if (grade != null)
            {
                _grades.Remove(grade);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _grades);
        }
    }
}