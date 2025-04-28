using System.ComponentModel.DataAnnotations;

namespace FaceAuthenticationsFunctions.Models
{
    public class EmployeeIdentityRequest
    {
        [EmailAddress]
        public required string EmployeeId { get; set; }
        public required string Base64Image { get; set; } // Accept base64 image
    }
}
