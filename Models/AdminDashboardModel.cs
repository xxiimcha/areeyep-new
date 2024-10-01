using Microsoft.AspNetCore.Mvc;

namespace AreEyeP.Models
{
    public class AdminDashboardModel
    {
        // Metrics you want to display on the dashboard
        public int TotalUsers { get; set; }
        public int TotalReports { get; set; }
        public int ActiveSessions { get; set; }
        public int PendingTasks { get; set; }

        // Any other necessary fields for the dashboard
        public List<string> RecentActivityLogs { get; set; }

        public AdminDashboardModel()
        {
            // Initialize the RecentActivityLogs list
            RecentActivityLogs = new List<string>();
        }
    }
}
