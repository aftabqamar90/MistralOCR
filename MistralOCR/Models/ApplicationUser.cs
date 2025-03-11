using Microsoft.AspNetCore.Identity;

namespace MistralOCR.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add custom user properties here if needed
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 