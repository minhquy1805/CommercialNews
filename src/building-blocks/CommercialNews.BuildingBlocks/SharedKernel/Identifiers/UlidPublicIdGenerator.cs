using NUlid;

namespace CommercialNews.BuildingBlocks.SharedKernel.Identifiers;

public sealed class UlidPublicIdGenerator : IPublicIdGenerator
{
    public string NewId()
    {
        return Ulid.NewUlid().ToString();
    }
}