# AutoFlow.Abstractions

Contracts, models, and interfaces for the automation framework.

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add new workflow node type | IWorkflowNode implementations (IfNode, ForEachNode, etc.) |
| Define keyword contract | IKeywordHandler<TArgs> |
| Add execution context data | WorkflowContext.cs, StepContext.cs |
| Extend validation | IWorkflowValidator.cs, ValidationResult.cs |
| Configure keyword metadata | KeywordAttribute.cs |

## KEY INTERFACES

- **IKeywordHandler<TArgs>**: Implement for custom keywords
- **IRuntimeEngine**: Workflow execution contract
- **IWorkflowParser**: YAML → WorkflowDocument
- **IWorkflowLifecycleHook**: Intercept workflow/step events
- **ISecretProvider**: Secret resolution strategy
- **IExecutionContext**: Step execution state

## NODE TYPES

Workflow AST nodes: `StepNode`, `IfNode`, `ForEachNode`, `ParallelNode`, `CallNode`, `GroupNode`, `ConditionNode`, `RetryNode`, `OnErrorNode`

## CONVENTIONS

- All models are immutable records where possible
- Keyword args classes use `KeywordAttribute` for registration metadata
- Result types: `RunResult`, `StepExecutionResult`, `KeywordResult`
- No dependencies on other AutoFlow projects - pure contracts
