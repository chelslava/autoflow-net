// Этот код нужен для парсинга внешнего DSL-документа в AST-модель.
namespace AutoFlow.Abstractions;

public interface IWorkflowParser
{
    WorkflowDocument Parse(string yamlContent);
}
