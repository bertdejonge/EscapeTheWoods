using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace EscapeFromTheWoods
{
    public class DBwriter
    {
        private string _connectionString = @"mongodb://localhost:27017";
        private IMongoClient _client;
        public IMongoDatabase _db;

        public DBwriter()
        {
            _client = new MongoClient(_connectionString);
            _db = _client.GetDatabase("EscapeTheWoodsDB");
        }

        public async Task WriteWoodRecordAsync(DBWoodRecord record)
        {
            var collection = _db.GetCollection<DBWoodRecord>("Woods");
            await collection.InsertOneAsync(record);

        }
        public async Task WriteMonkeyRecord(DBMonkeyRecord record)
        {
            var collection = _db.GetCollection<DBMonkeyRecord>("Monkeys");
            await collection.InsertOneAsync(record);
        }
    }
}