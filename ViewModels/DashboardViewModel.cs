namespace MutaEngineering.ViewModels
{
    public class DashboardViewModel
    {
        public int DepartmentsCount { get; set; }
        public int FacultyCount { get; set; }
        public int ExamsCount { get; set; }
        public int AlertsCount { get; set; }
        public int NewsCount { get; set; }

        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
