namespace CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions
{
    public abstract class PersistenceException : Exception
    {
        protected PersistenceException(
            string code,
            string message,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Code = code;
        }

        public string Code { get; }
    }
}