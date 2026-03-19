namespace CommercialNews.BuildingBlocks.Messaging.Outbox
{
    public static class OutboxMessageStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Published = "Published";
        public const string Failed = "Failed";
        public const string DeadLetter = "DeadLetter";
    }
}