namespace Seo.Application.Models.Commands;

public sealed record SlugRegistryDeactivateByResourceCommand(
    string Scope,
    string ResourceType,
    string ResourcePublicId,
    long? ActorUserId,
    int? ExpectedVersion);