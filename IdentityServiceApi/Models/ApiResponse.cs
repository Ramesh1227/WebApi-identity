namespace IdentityServiceApi.Models
{
    public class ApiResponse
    {
        public required string Message { get; set; }

        public required int StatusCode { get; set; }

        public required object? Data { get; set; }

        public string Error { get; set; }
    }
}
