using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class StudentRepository : IStudentRepository
    {
        private readonly string _filePath;
        private List<Student> _students;

        public StudentRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "students.csv");
            LoadData();
        }

        private void LoadData()
        {
            _students = CsvFileHandler.ReadCsvFile<Student>(_filePath).ToList();
        }

        public IEnumerable<Student> GetAll()
        {
            return _students.ToList();
        }

        public Student GetById(string id)
        {
            return _students.FirstOrDefault(s => s.StudentId == id);
        }

        public Student GetByUserId(string userId)
        {
            return _students.FirstOrDefault(s => s.UserId == userId);
        }

        public IEnumerable<Student> GetBySchoolProgram(string schoolProgramId)
        {
            return _students.Where(s => s.SchoolProgramId == schoolProgramId).ToList();
        }

        public void Add(Student entity)
        {
            _students.Add(entity);
        }

        public void Update(Student entity)
        {
            var index = _students.FindIndex(s => s.StudentId == entity.StudentId);
            if (index >= 0)
            {
                _students[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var student = GetById(id);
            if (student != null)
            {
                _students.Remove(student);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _students);
        }
    }
}