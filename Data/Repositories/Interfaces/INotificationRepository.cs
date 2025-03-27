using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Notification
    public interface INotificationRepository : IRepository<Notification>
    {
        IEnumerable<Notification> GetByUser(string userId);
    }
}