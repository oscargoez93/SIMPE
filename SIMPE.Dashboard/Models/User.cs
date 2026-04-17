using System.ComponentModel.DataAnnotations;

namespace SIMPE.Dashboard.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        // true = Aprobado y puede entrar. false = Pendiente de aprobación.
        public bool IsApproved { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
