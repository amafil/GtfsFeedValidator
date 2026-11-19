namespace GtfsFeedValidator.Configuration
{
    public class GtfsValidatorSettings
    {
        public const string Path = "GtfsFeedValidatorConfiguration";
        [System.ComponentModel.DataAnnotations.Required]
        public string ConnectionString { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string WorkingDirectory { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string GtfsValidatorJarPath { get; set; } = string.Empty;
    }
}
