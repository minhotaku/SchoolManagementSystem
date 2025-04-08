using System;

namespace SchoolManagementSystem.Entities
{
    public class Notification
    {
        public string NotificationId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}