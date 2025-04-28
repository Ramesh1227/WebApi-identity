using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;

namespace FaceAuthenticationsFunctions.Models
{
    [DynamoDBTable("EmployeeIdentityInfo")]
    public class EmployeeIdentityEntity
    {
        [DynamoDBHashKey]
        [EmailAddress]
        public required string EmployeeId { get; set; }

        public string ImageKey { get; set; }
    }
}
