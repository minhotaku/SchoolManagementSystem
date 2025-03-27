using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Formats.Asn1;

namespace SchoolManagementSystem.Data.CSV
{
    // Trách nhiệm duy nhất: Xử lý đọc/ghi file CSV
    public static class CsvFileHandler
    {
        public static IEnumerable<T> ReadCsvFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<T>().ToList();
            }
        }

        public static void WriteCsvFile<T>(string filePath, IEnumerable<T> records)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
        }
    }
}