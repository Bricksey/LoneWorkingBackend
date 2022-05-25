using LoneWorkingBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LoneWorkingBackend.Services
{
    public class AccountsService
    {
        // CRUD service for the account collection
        private readonly IMongoCollection<Account> _accountsCollection;

       public AccountsService(IOptions<LoneWorkingDatabaseSettings> loneWorkingDatabaseSettings)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb+srv://Harry:9HKCT0xK1PqnPZUM@vanilla.mfxfp.mongodb.net/?retryWrites=true&w=majority");
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var mongoClient = new MongoClient(settings);
            var mongoDatabase = mongoClient.GetDatabase(loneWorkingDatabaseSettings.Value.DatabaseName);
            _accountsCollection = mongoDatabase.GetCollection<Account>(loneWorkingDatabaseSettings.Value.AccountsCollectionName);
        }
        public async Task<List<Account>> GetAsync() =>  
            await _accountsCollection.Find(_ => true).ToListAsync();
        public async Task<Account?> GetAsync(string id) => 
            await _accountsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        public async Task<Account?> GetAsyncEmail(string email) =>
            await _accountsCollection.Find(x => x.Email == email).FirstOrDefaultAsync(); 
        public async Task CreateAsync(Account newAccount) => 
            await _accountsCollection.InsertOneAsync(newAccount);
        public async Task UpdateAsync(string id, Account UpdatedAccount) => 
            await _accountsCollection.ReplaceOneAsync(x => x.Id == id, UpdatedAccount);
        public async Task RemoveAsync(string id) =>
            await _accountsCollection.DeleteOneAsync(x => x.Id == id);
    }
}