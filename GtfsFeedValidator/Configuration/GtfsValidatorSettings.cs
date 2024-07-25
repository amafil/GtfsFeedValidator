namespace GtfsFeedValidator.Configuration
{
    public class GtfsValidatorSettings
    {
        public const string Path = "GtfsFeedValidatorConfiguration";
        public string? ConnectionString { get; set; }
        public string? WorkingDirectory { get; set; }
        public string? GtfsValidatorJarPath { get; set; }
    }
}
