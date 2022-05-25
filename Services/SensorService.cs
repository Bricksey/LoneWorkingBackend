using LoneWorkingBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LoneWorkingBackend.Services
{
    public class SensorService
    {
        private readonly IMongoCollection<Sensor> _sensorsCollection;
        
        public SensorService(IOptions<LoneWorkingDatabaseSettings> loneWorkingDatabaseSettings)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb+srv://Pi:6ciXdbklDynHHH5i@vanilla.mfxfp.mongodb.net/?retryWrites=true&w=majority");
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var mongoClient = new MongoClient(settings);
            var mongoDatabase = mongoClient.GetDatabase(loneWorkingDatabaseSettings.Value.DatabaseName);
            _sensorsCollection = mongoDatabase.GetCollection<Sensor>(loneWorkingDatabaseSettings.Value.SensorsCollectionName);
        }

        public async Task<List<Sensor>> GetAsync() =>  
            await _sensorsCollection.Find(_ => true).ToListAsync();
        public async Task<Sensor?> GetAsync(string id) => 
            await _sensorsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        public async Task CreateAsync(Sensor newSensor) => 
            await _sensorsCollection.InsertOneAsync(newSensor);
        public async Task UpdateAsync(string id, Sensor UpdatedSensor) => 
            await _sensorsCollection.ReplaceOneAsync(x => x.Id == id, UpdatedSensor);
        public async Task RemoveAsync(string id) =>
            await _sensorsCollection.DeleteOneAsync(x => x.Id == id);
    }
}