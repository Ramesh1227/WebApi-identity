using Neurotec.Licensing;

namespace IdentityServiceApi.Service
{
    public static class LicenseActivator
    {
        private const string components = "FaceMatcher,FaceExtractor";
        public static void Activate(ILogger _logger)
        {
            try
            {
                var outputPath = Directory.GetCurrentDirectory();
                var licensePath = Path.Combine(outputPath, "License");
                if (!Directory.Exists(licensePath))
                {
                    throw new Exception("License directoy doest not exist");
                }
                else
                {
                    _logger.LogInformation($"License directory: {licensePath}");
                    var licenseFiles = Directory.GetFiles(licensePath, "*.lic", SearchOption.AllDirectories);

                    if (licenseFiles.Length > 0)
                    {
                        foreach (var file in licenseFiles)
                        {
                            if (!File.Exists(file))
                            {
                                throw new Exception($"License file does not exist: {file}");
                            }
                            else
                            {
                                _logger.LogInformation($"License file found: {file}");
                                var content = File.ReadAllText(file);

                                NLicenseManager.WritableStoragePath = licensePath;
                                NLicense.Add(content);

                                if (!NLicense.Obtain("/local", 5000, components))
                                {
                                    _logger.LogError($"Trial license activation failed for: {components}");
                                    throw new Exception("Failed to obtain license.");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"License Activated");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to activate licenses: {ex?.InnerException} , Message : {ex?.Message}");
                throw;
            }
        }
    }
}

