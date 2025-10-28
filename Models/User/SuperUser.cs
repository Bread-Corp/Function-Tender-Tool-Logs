namespace Tender_Tool_Logs_Lambda.Models.User
{
    public class SuperUser : TenderUser
    {
        public string? Organisation { get; set; }

        public SuperUser()
        {
            this.IsSuperUser = true;
            this.Role = "SuperUser";
        }
    }
}
