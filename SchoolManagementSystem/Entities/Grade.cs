namespace SchoolManagementSystem.Entities
{
    public class Grade
    {
        public string GradeId { get; set; }
        public string EnrollmentId { get; set; }
        public string Component { get; set; }
        public decimal Score { get; set; }
    }
}