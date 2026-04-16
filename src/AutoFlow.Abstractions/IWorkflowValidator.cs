namespace AutoFlow.Abstractions;

/// <summary>
/// Contract for validating workflow documents before execution.
/// </summary>
public interface IWorkflowValidator
{
    /// <summary>
    /// Validates a workflow document.
    /// </summary>
    /// <param name="document">The workflow document to validate.</param>
    /// <returns>The validation result with any errors found.</returns>
    ValidationResult Validate(WorkflowDocument document);
}
