using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolManagementSystem.Data.CSV;
using SchoolManagementSystem.Data.Repositories.Interfaces;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Implementation
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly string _filePath;
        private List<Notification> _notifications;

        public NotificationRepository(string basePath)
        {
            _filePath = Path.Combine(basePath, "notifications.csv");
            LoadData();
        }

        private void LoadData()
        {
            _notifications = CsvFileHandler.ReadCsvFile<Notification>(_filePath).ToList();
        }

        public IEnumerable<Notification> GetAll()
        {
            return _notifications.ToList();
        }

        public Notification GetById(string id)
        {
            return _notifications.FirstOrDefault(n => n.NotificationId == id);
        }

        public IEnumerable<Notification> GetByUser(string userId)
        {
            return _notifications.Where(n => n.UserId == userId).ToList();
        }

        public void Add(Notification entity)
        {
            _notifications.Add(entity);
        }

        public void Update(Notification entity)
        {
            var index = _notifications.FindIndex(n => n.NotificationId == entity.NotificationId);
            if (index >= 0)
            {
                _notifications[index] = entity;
            }
        }

        public void Delete(string id)
        {
            var notification = GetById(id);
            if (notification != null)
            {
                _notifications.Remove(notification);
            }
        }

        public void Save()
        {
            CsvFileHandler.WriteCsvFile(_filePath, _notifications);
        }
    }
}