using System;

namespace SchoolManagementSystem.Models
{
    public class NotificationViewModel
    {
        public string NotificationId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string FormattedTimestamp => Timestamp.ToString("dd/MM/yyyy HH:mm");
    }
}