using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly string _filePath;
        private List<Enrollment> _enrollments;

        public EnrollmentRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "enrollments.csv");
            LoadData();
        }

        private void LoadData()
        {
            _enrollments = CsvFileHandler.ReadCsvFile<Enrollment>(_filePath).ToList();
        }

        public IEnumerable<Enrollment> GetAll()
        {
            return _enrollments.ToList();
        }

        public Enrollment GetById(string id)
        {
            return _enrollments.FirstOrDefault(e => e.EnrollmentId == id);
        }

        public IEnumerable<Enrollment> GetByStudent(string studentId)
        {
            return _enrollments.Where(e => e.StudentId == studentId).ToList();
        }

        public IEnumerable<Enrollment> GetByCourse(string courseId)
        {
            return _enrollments.Where(e => e.CourseId == courseId).ToList();
        }

        public IEnumerable<Enrollment> GetBySemester(string semester)
        {
            return _enrollments.Where(e => e.Semester == semester).ToList();
        }

        public void Add(Enrollment entity)
        {
            _enrollments.Add(entity);
        }

        public void Update(Enrollment entity)
        {
            var index = _enrollments.FindIndex(e => e.EnrollmentId == entity.EnrollmentId);
            if (index >= 0)
            {
                _enrollments[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var enrollment = GetById(id);
            if (enrollment != null)
            {
                _enrollments.Remove(enrollment);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _enrollments);
        }
    }
}