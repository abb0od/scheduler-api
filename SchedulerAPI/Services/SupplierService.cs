using MongoDB.Driver;
using SchedulerAPI.Models;
using Microsoft.Extensions.Options;

namespace SchedulerAPI.Services;

public class SupplierService
{
    private readonly IMongoCollection<Supplier> _suppliers;

    public SupplierService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _suppliers = db.GetCollection<Supplier>("suppliers");
    }

    public async Task<List<Supplier>> GetAllAsync(string userId) =>
        await _suppliers.Find(s => s.UserId == userId).ToListAsync();

    public async Task<Supplier?> GetByIdAsync(string id, string userId) =>
        await _suppliers.Find(s => s.Id == id && s.UserId == userId).FirstOrDefaultAsync();

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        await _suppliers.InsertOneAsync(supplier);
        return supplier;
    }

    public async Task UpdateAsync(string id, Supplier supplier) =>
        await _suppliers.ReplaceOneAsync(s => s.Id == id && s.UserId == supplier.UserId, supplier);

    public async Task DeleteAsync(string id, string userId) =>
        await _suppliers.DeleteOneAsync(s => s.Id == id && s.UserId == userId);
}
