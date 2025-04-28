namespace FaceAuthenticationsFunctions.Models
{
    public class IdentityVerificationRequest
    {
        public required string MeetingId { get; set; }

        public required string Base64Image { get; set; }
    }
}
