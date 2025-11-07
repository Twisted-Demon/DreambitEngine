#nullable enable
namespace Dreambit;

public readonly record struct LogEntry(
    LogLevel Level,
    string Prefix,
    string Message,
    object[]? Args
    );