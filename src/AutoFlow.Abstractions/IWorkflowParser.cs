namespace AutoFlow.Abstractions;

/// <summary>
/// Contract for parsing YAML workflow definitions into AST model.
/// </summary>
public interface IWorkflowParser
{
    /// <summary>
    /// Parses YAML content into a workflow document.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <returns>The parsed workflow document.</returns>
    WorkflowDocument Parse(string yamlContent);
}
