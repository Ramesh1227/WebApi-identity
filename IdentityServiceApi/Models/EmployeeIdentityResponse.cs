using Neurotec.Biometrics;

namespace FaceAuthenticationsFunctions.Models
{
    public class EmployeeIdentityResponse
    {
        public required NBiometricStatus Status { get; set; }
        public required int MatchScore { get; set; }

        // Accept base64 image
    }
}
