namespace Media.Application.Models.Commands;

public sealed record RestoreMediaAssetCommand(
    long MediaId,
    long? RestoredBy);