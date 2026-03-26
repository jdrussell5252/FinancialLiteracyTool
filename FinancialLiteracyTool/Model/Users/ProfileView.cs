namespace FinancialLiteracyTool.Model.Users
{
    public class ProfileView
    {
        public string? SystemUserFirstName { get; set; }
        public string? SystemUserLastName { get; set; }
        //public string SystemUserProfileImage { get; set; }
        public string? SystemUserName { get; set; }
        public int? SystemUserRole { get; set; }
        public IFormFile? SystemUserProfileImage { get; set; }
        public string? SystemUserProfileImagePath { get; set; }
        public int SystemUserID { get; set; }
        public string? SystemUserPassword { get; set; }
    }
}
