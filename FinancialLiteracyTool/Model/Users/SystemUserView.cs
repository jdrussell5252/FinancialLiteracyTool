namespace FinancialLiteracyTool.Model.Users
{
    public class SystemUserView
    {
        public string? UserFName { get; set; }
        public string? UserLName { get; set; }
        public string? SystemUsername { get; set; }
        public int SystemUserRole { get; set; }
        public int UserID { get; set; }
        public int SystemUserID { get; set; }
        public bool IsAdmin { get; set; }
        public string? CoachFirstName { get; set; }
        public string? CoachLastName { get; set; }
    }// End of 'SystemUserView' Class.
}// End of 'namespace'.
