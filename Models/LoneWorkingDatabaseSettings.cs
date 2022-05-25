namespace LoneWorkingBackend.Models
{
    public class LoneWorkingDatabaseSettings
    {
        public string ConnectionString {get; set;} = null!;
        public string DatabaseName {get; set;} = null!;
        public string AccountsCollectionName {get; set;} = null!;
        public string SensorsCollectionName {get; set;} = null!;
    }
}