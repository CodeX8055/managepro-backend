namespace backend.Models
{
    public static class RoleConfig
    {
        public static readonly string[] ManagerRoles = { "Project Manager", "HR Manager", "Tech Lead", "Product Owner", "Resource Manager" };
        public static readonly string[] EmployeeRoles = { "Sr. Developer", "Jr. Developer", "Full-Stack Developer", "QA Tester", "UI/UX Designer", "Intern" };
        
        public static bool IsManager(string role) => System.Array.Exists(ManagerRoles, r => r == role);
        public static bool IsEmployee(string role) => System.Array.Exists(EmployeeRoles, r => r == role);
        
        // Comma-separated strings for the [Authorize(Roles="...")] attributes
        public const string AllManagers = "Project Manager,HR Manager,Tech Lead,Product Owner,Resource Manager";
        public const string AllManagersAndAdmins = "OrgAdmin,Project Manager,HR Manager,Tech Lead,Product Owner,Resource Manager,SuperAdmin";
        public const string Everybody = "OrgAdmin,Project Manager,HR Manager,Tech Lead,Product Owner,Resource Manager,Sr. Developer,Jr. Developer,Full-Stack Developer,QA Tester,UI/UX Designer,Intern,SuperAdmin";
    }
}
