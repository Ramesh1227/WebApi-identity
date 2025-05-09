using Amazon.DynamoDBv2;
using Amazon.S3;
using FaceAuthenticationsFunctions.Models;
using IdentityServiceApi.Service;
using Microsoft.AspNetCore.Mvc;
using Neurotec.Biometrics.Client;
using Neurotec.Biometrics;
using Neurotec.Licensing;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Net;
using IdentityServiceApi.Models;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IdentityController : ControllerBase
    {       
        private readonly ILogger<IdentityController> _logger;
        private readonly IdentityVerificationServices _veificationSevices;
        public IdentityController(ILogger<IdentityController> logger, IdentityVerificationServices veificationSevices)
        {
            _logger = logger;
            _veificationSevices  = veificationSevices;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyIdentity([FromBody] IdentityVerificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MeetingId) || string.IsNullOrWhiteSpace(request.Base64Image))
            {
                return BadRequest("EmployeeId and Base64Image are required.");
            }

            try
            {
                _logger.LogInformation($"Verification started for Id : {request.MeetingId}");

                // Load license before processing anything
                //LicenseActivator.Activate(_logger);

                // Retrieve meeting information based on the provided MeetingId
                var meeting = await _veificationSevices.GetMeetingAsync(request.MeetingId, _logger);
                if (meeting == null || string.IsNullOrEmpty(meeting.OrganizerId) || meeting.AttendesList?.Count == 0)
                    return BadRequest("Invalid or empty meeting.");

                // Compile a list of employee IDs involved in the meeting
                var employeeIds = new List<string> { meeting.OrganizerId };
                if (meeting.AttendesList != null && meeting.AttendesList.Count > 0)
                {
                    employeeIds.AddRange(meeting.AttendesList);
                }
                var identities = await _veificationSevices.GetEmployeesByIdsAsync(employeeIds.Distinct().ToList(),_logger);

                // Validate that employee identities were found
                if (identities == null || identities.Count == 0)
                    return BadRequest("No identity records found.");


                // Create a biometric subject from the provided base64 image
                var candidateSubject = _veificationSevices.CreateSubjectFromBase64(request.Base64Image, "candidate", _logger);
                _logger.LogInformation($"Candidate subject created with ID: {candidateSubject?.Id}");

                using var biometricClient = new NBiometricClient
                {
                    MatchingThreshold = 48,
                    FacesMatchingSpeed = NMatchingSpeed.Low,
                    FacesDetectAllFeaturePoints = false,
                };

                // Create a biometric template from the candidate subject
                var templateStatus = await biometricClient.CreateTemplateAsync(candidateSubject);
                if (templateStatus != NBiometricStatus.Ok)
                    return BadRequest("Failed to extract face template.");

                // Process identity verification against stored employee images
                var result = await _veificationSevices.ProcessIdentityVerification(identities, candidateSubject, biometricClient, _logger);
                if (result.Status == NBiometricStatus.MatchNotFound)
                {
                    _logger.LogInformation($"No match found for candidate subject ID: {candidateSubject?.Id}");
                    return Unauthorized("No match found.");
                }
                _logger.LogInformation($"Match found for candidate subject ID: {candidateSubject?.Id} with score: {result.MatchScore}");
                return Success(result);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON Deserialization Error: {jsonEx}");
                return BadRequest("Invalid JSON format.");
            }
            catch (FormatException fe)
            {
                _logger.LogError($"Image base64 decoding failed: {fe}");
                return BadRequest("Invalid image format.");
            }
            catch (AmazonS3Exception s3Ex)
            {
                _logger.LogError($"Error while getting data from S3: {s3Ex}");
                return ServerError("Error while getting data from S3.");
            }
            catch (AmazonDynamoDBException dbEx)
            {
                _logger.LogError($"Error while getting data from DynamoDB: {dbEx}");
                return ServerError("Error while getting data from DynamoDB.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in VerifyIdentityAsync: {ex}");
                return ServerError("Internal server error.");
            }

        }

        private IActionResult ServerError(string message)
        {
            var result = new ApiResponse()
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = message,
                Data = null
            };
            return StatusCode((int)HttpStatusCode.InternalServerError, result); // Automatically serializes to JSON
        }

        private IActionResult Success(EmployeeIdentityResponse response)
        {
            var result  = new ApiResponse()
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Identity verification successful.",
                Data = response
            };
            return Ok(result); // Automatically serializes to JSON
        }

        private IActionResult Unauthorized(string response)
        {
            var result = new ApiResponse()
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = response,
                Data = null
            };
            return Unauthorized(result); // Automatically serializes to JSON
        }

        private IActionResult BadRequest(string message)
        {
            var result = new ApiResponse()
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = message,
                Data = null
            };
            return BadRequest(result);
        }
    }
}
