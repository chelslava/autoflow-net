using System;

namespace AutoFlow.Runtime;

public sealed record KeywordRegistration(
    string Name,
    Type HandlerType,
    Type ArgsType,
    string? Category = null,
    string? Description = null);
