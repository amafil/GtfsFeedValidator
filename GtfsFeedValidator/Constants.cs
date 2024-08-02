namespace GtfsFeedValidator
{
    public static class Constants
    {
        public const string GtfsFeedValidationCollectionName = "GtfsFeedValidation";
        public const string GtfsFeedValidationResultCollectionName = "GtfsFeedValidationResult";

        public const string GtfsFeedValidationStatusPending = "Pending";
        public const string GtfsFeedValidationStatusProcessing = "Processing";
        public const string GtfsFeedValidationStatusCompleted = "Completed";
        public const string GtfsFeedValidationStatusError = "Error";

        public const int WorkerMsPollingInterval = 5_000;

    }
}
