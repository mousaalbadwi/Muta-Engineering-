namespace MutaEngineering.Models
{
    public class StudyPlan
    {
        public string DepartmentSlug { get; set; } = default!; // civil, mech, ...
        public string TitleAr { get; set; } = default!;
        public string TitleEn { get; set; } = default!;
        public string PdfPath { get; set; } = default!;         // ~/docs/plans/civil/plan-2023.pdf
        public int Year { get; set; }                           // 2023, 2024 ...
    }
}
