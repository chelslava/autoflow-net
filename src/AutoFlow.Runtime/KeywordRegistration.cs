// Этот код нужен для хранения информации о зарегистрированном keyword.
using System;

namespace AutoFlow.Runtime;

public sealed record KeywordRegistration(
    string Name,
    Type HandlerType,
    Type ArgsType);
