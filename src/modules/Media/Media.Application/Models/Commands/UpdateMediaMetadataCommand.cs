namespace Media.Application.Models.Commands;

public sealed record UpdateMediaMetadataCommand(
    long MediaId,
    string? AltText,
    string? MetadataJson,
    long? UpdatedBy);