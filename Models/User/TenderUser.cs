using System.ComponentModel.DataAnnotations;

namespace Tender_Tool_Logs_Lambda.Models.User
{
    public class TenderUser
    {
        [Key]
        [Required]
        public Guid UserID { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        public string? PhoneNumber { get; set; }

        public DateTime DateAppended { get; set; }

        [Required]
        public string Role { get; set; } = "StandardUser";

        public string? ProfilePicture { get; set; }

        [Required]
        public bool IsSuperUser { get; set; } = false;
    }
}
