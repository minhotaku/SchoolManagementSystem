using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class SchoolProgramRepository : ISchoolProgramRepository
    {
        private readonly string _filePath;
        private List<SchoolProgram> _programs;

        public SchoolProgramRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "programs.csv");
            LoadData();
        }

        private void LoadData()
        {
            _programs = CsvFileHandler.ReadCsvFile<SchoolProgram>(_filePath).ToList();
        }

        public IEnumerable<SchoolProgram> GetAll()
        {
            return _programs.ToList();
        }

        public SchoolProgram GetById(string id)
        {
            return _programs.FirstOrDefault(p => p.SchoolProgramId == id);
        }

        public SchoolProgram GetByName(string name)
        {
            return _programs.FirstOrDefault(p => p.SchoolProgramName == name);
        }

        public void Add(SchoolProgram entity)
        {
            _programs.Add(entity);
        }

        public void Update(SchoolProgram entity)
        {
            var index = _programs.FindIndex(p => p.SchoolProgramId == entity.SchoolProgramId);
            if (index >= 0)
            {
                _programs[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var program = GetById(id);
            if (program != null)
            {
                _programs.Remove(program);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _programs);
        }
    }
}