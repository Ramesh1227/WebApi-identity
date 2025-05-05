using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3.Model;
using FaceAuthenticationsFunctions.Models;
using Neurotec.Biometrics.Client;
using Neurotec.Biometrics;
using Microsoft.Extensions.Logging;
using Amazon.Runtime;

namespace IdentityServiceApi.Service
{
    public class IdentityVerificationServices
    {
        private readonly string _bucketName = "employee-identity-info";

        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly DynamoDBContext _dbContext;
        private EmployeeIdentityResponse response;
        //private readonly ILogger<IdentityVerificationServices> _logger;


        public IdentityVerificationServices(IAmazonS3 s3Client,
            IAmazonDynamoDB dynamoDbClient)
        {
            _s3Client = s3Client;
            _dynamoDbClient = dynamoDbClient;

            //var awsCredentials = new BasicAWSCredentials("AKIAQSOI4RD2GYPCEMXG", "1V+kjcccqdUIWRNEa4IVwQK7NFHuP2f7S+N1ERHz");

            //var client = new AmazonDynamoDBClient(awsCredentials, Amazon.RegionEndpoint.APSouth2);

            //_s3Client = new AmazonS3Client(awsCredentials, Amazon.RegionEndpoint.APSouth2);
            _dbContext = new DynamoDBContext(_dynamoDbClient);
            //_dbContext = new DynamoDBContext(client);
            //_logger = new LoggerFactory().CreateLogger<IdentityVerificationServices>();
        }

        /// <summary>
        /// Processes identity verification by comparing the candidate subject against stored employee images.
        /// </summary>
        /// <param name="context">The Lambda context for logging.</param>
        /// <param name="identities">A list of employee identities to compare against.</param>
        /// <param name="candidateSubject">The biometric subject created from the provided image.</param>
        /// <param name="biometricClient">The biometric client used for verification.</param>
        /// <returns>An API Gateway proxy response indicating the result of the verification.</returns>

        public async Task<EmployeeIdentityResponse> ProcessIdentityVerification(List<EmployeeIdentityEntity> identities, NSubject candidateSubject, NBiometricClient biometricClient, ILogger _logger)
        {
            foreach (var identity in identities)
            {
                // Retrieve the stored image from S3
                using var refStream = await GetS3ObjectStreamAsync(_bucketName, identity.ImageKey,_logger);
                using var refSubject = CreateSubjectFromStream(refStream, "reference", _logger);

                // Perform biometric verification
                biometricClient.Verify(refSubject, candidateSubject);

                // Retrieve the matching score
                var matchScore = refSubject.MatchingResults.FirstOrDefault()?.Score ?? 0;

                // Check if a match was found
                if (refSubject.Status == NBiometricStatus.Ok)
                {
                    response = new EmployeeIdentityResponse
                    {
                        Status = refSubject.Status,
                        MatchScore = matchScore
                    };

                    _logger.LogInformation($"Match found: Score = {matchScore}");
                    return response;
                }
            }

            // No match found
            response.Status = NBiometricStatus.MatchNotFound;
            _logger.LogInformation("No match found.");
            return response;
        }
        /// <summary>
        /// Retrieves meeting information based on the provided MeetingId.
        /// </summary>
        /// <param name="meetingId">The ID of the meeting to retrieve.</param>
        /// <returns>A ScheduledMeetingList object containing meeting details, or null if not found.</returns>

        public async Task<ScheduledMeetingList?> GetMeetingAsync(string meetingId, ILogger _logger)
        {
            try
            {
                var results = await _dbContext.ScanAsync<ScheduledMeetingList>(
                    new[] { new ScanCondition("MeetingId", ScanOperator.Equal, meetingId) }
                ).GetRemainingAsync();
                _logger.LogInformation($"Meeting information retrieved for MeetingId: {meetingId}");
                return results.FirstOrDefault();
            }
            catch (AmazonDynamoDBException ex)
            {
                throw;
            }

        }
        /// <summary>
        /// Retrieves employee identity records based on a list of employee IDs.
        /// </summary>
        /// <param name="ids">A list of employee IDs.</param>
        /// <returns>A list of EmployeeIdentityEntity objects.</returns>

        public async Task<List<EmployeeIdentityEntity>> GetEmployeesByIdsAsync(List<string> ids, ILogger _logger)
        {
            try
            {
                var batch = _dbContext.CreateBatchGet<EmployeeIdentityEntity>();
                ids.ForEach(batch.AddKey);
                await batch.ExecuteAsync();
                _logger.LogInformation($"Employee identity records retrieved for IDs: {string.Join(", ", ids)}");
                return batch.Results;
            }
            catch (AmazonDynamoDBException ex)
            {
                _logger.LogError($"AmazonDynamoDBException: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves an object from the specified Amazon S3 bucket and returns its response stream.
        /// </summary>
        /// <param name="bucket">The name of the S3 bucket.</param>
        /// <param name="key">The key (path) of the object within the bucket.</param>
        /// <returns>A stream containing the object's data.</returns>
        /// <exception cref="AmazonS3Exception">Thrown when an error occurs during the S3 GetObject operation.</exception>
        /// <exception cref="Exception">Thrown for general exceptions.</exception>
        public async Task<Stream> GetS3ObjectStreamAsync(string bucket, string key, ILogger _logger)
        {
            try
            {
                // Create a GetObjectRequest with the specified bucket and key
                var request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = key
                };

                // Execute the GetObjectAsync request to retrieve the object from S3
                var response = await _s3Client.GetObjectAsync(request);

                _logger.LogInformation($"S3 object retrieved: Bucket = {bucket}, Key = {key}");
                // Return the response stream containing the object's data
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex)
            {
                // Log and rethrow Amazon S3 specific exceptions
                // Example: access denied, bucket not found, etc.
                _logger.LogError($"AmazonS3Exception: {ex.Message}");
                throw ex;
            }
        }

        public NSubject CreateSubjectFromBase64(string base64Image, string subjectId, ILogger _logger)
        {
            //byte[] imageBytes1 = File.ReadAllBytes("C:\\4CT\\Identity\\FaceAuthendicationFunctions\\bin\\Debug\\net8.0\\Images\\Testimage.jpg");
            ////string base64String = Convert.ToBase64String(imageBytes1);

            var imageBytes = Convert.FromBase64String(base64Image);
            using var stream = new MemoryStream(imageBytes);
            return CreateSubjectFromStream(stream, subjectId, _logger);
        }
        /// <summary>
        /// Creates an NSubject from a given image stream.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="subjectId">The identifier for the subject.</param>
        /// <returns>An NSubject containing the facial image.</returns>
        public NSubject CreateSubjectFromStream(Stream imageStream, string subjectId, ILogger _logger)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    imageStream.CopyTo(fileStream);
                }

                var subject = new NSubject { Id = subjectId };
                subject.Faces.Add(new NFace { FileName = tempFilePath });

                _logger.LogInformation($"Created NSubject: {subjectId}");
                return subject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating subject for {subjectId} with error :{ex}");
                throw;
            }
        }

    }
}
