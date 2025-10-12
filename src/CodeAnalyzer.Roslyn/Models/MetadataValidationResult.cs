using System.Collections.Generic;

namespace CodeAnalyzer.Roslyn.Models
{
    /// <summary>
    /// Result of metadata validation and normalization for method call data.
    /// </summary>
    public class MetadataValidationResult
    {
        /// <summary>
        /// Whether the metadata is valid and can be stored.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors found during normalization.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// The normalized method call data ready for storage.
        /// </summary>
        public MethodCallInfo NormalizedCall { get; set; } = new MethodCallInfo();
    }
}
