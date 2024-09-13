namespace AssemblyValidatorAPI.Models
{
    public class ValidationResult
    {
        public string? AssemblyName { get; set; }
        public string? ExpectedVersion { get; set; }
        public string? ActualVersion { get; set; }
        public ValidationStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ValidationRequest
    {
        public string Path { get; set; } = string.Empty;
    }
}