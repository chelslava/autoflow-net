namespace AutoFlow.Abstractions;

public interface IWorkflowValidator
{
    ValidationResult Validate(WorkflowDocument document);
}
