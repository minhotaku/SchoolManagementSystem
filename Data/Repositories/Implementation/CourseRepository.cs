using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class CourseRepository : ICourseRepository
    {
        private readonly string _filePath;
        private List<Course> _courses;

        public CourseRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "courses.csv");
            LoadData();
        }

        private void LoadData()
        {
            _courses = CsvFileHandler.ReadCsvFile<Course>(_filePath).ToList();
        }

        public IEnumerable<Course> GetAll()
        {
            return _courses.ToList();
        }

        public Course GetById(string id)
        {
            return _courses.FirstOrDefault(c => c.CourseId == id);
        }

        public IEnumerable<Course> GetByFaculty(string facultyId)
        {
            return _courses.Where(c => c.FacultyId == facultyId).ToList();
        }

        public IEnumerable<Course> GetByCredits(int credits)
        {
            return _courses.Where(c => c.Credits == credits).ToList();
        }

        public void Add(Course entity)
        {
            _courses.Add(entity);
        }

        public void Update(Course entity)
        {
            var index = _courses.FindIndex(c => c.CourseId == entity.CourseId);
            if (index >= 0)
            {
                _courses[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var course = GetById(id);
            if (course != null)
            {
                _courses.Remove(course);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _courses);
        }
    }
}