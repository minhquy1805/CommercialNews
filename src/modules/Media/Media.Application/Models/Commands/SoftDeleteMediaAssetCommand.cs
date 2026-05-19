namespace Media.Application.Models.Commands;

public sealed record SoftDeleteMediaAssetCommand(
    long MediaId,
    long? DeletedBy,
    DateTime? RestoreUntil);