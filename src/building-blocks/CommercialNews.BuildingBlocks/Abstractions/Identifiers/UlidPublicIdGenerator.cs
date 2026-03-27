using CommercialNews.BuildingBlocks.Abstractions.Identifiers;
using NUlid;

namespace CommercialNews.BuildingBlocks.Identifiers
{
    public sealed class UlidPublicIdGenerator : IPublicIdGenerator
    {
        public string NewId()
        {
            return Ulid.NewUlid().ToString();
        }
    }
}