using MongoDB.Driver;
using SchedulerAPI.Models;
using Microsoft.Extensions.Options;

namespace SchedulerAPI.Services;

public class AppointmentService
{
    private readonly IMongoCollection<Appointment> _appointments;
    private readonly SupplierService _supplierService;

    public AppointmentService(IOptions<MongoDbSettings> settings, SupplierService supplierService)
    {
        var client = new MongoClient(settings.Value.MONGODB_URI);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _appointments = db.GetCollection<Appointment>("appointments");
        _supplierService = supplierService;
    }

    public async Task<List<Appointment>> GetAllForUserAsync(string userId) =>
        await _appointments.Find(a => a.UserId == userId).ToListAsync();

    public async Task<List<Appointment>> GetAllForSupplierAsync(string supplierId) =>
        await _appointments.Find(a => a.SupplierId == supplierId).ToListAsync();

    public async Task<Appointment?> GetByIdAsync(string id) =>
        await _appointments.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<Appointment> CreateAsync(Appointment appointment)
    {
        // Validate that the supplier exists
        var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, string.Empty);
        if (supplier == null)
            throw new Exception("Supplier not found");

        await _appointments.InsertOneAsync(appointment);
        return appointment;
    }

    public async Task UpdateAsync(string id, Appointment appointment)
    {
        var filter = Builders<Appointment>.Filter.Eq(a => a.Id, id);
        await _appointments.ReplaceOneAsync(filter, appointment);
    }

    public async Task UpdateStatusAsync(string id, string status)
    {
        var filter = Builders<Appointment>.Filter.Eq(a => a.Id, id);
        var update = Builders<Appointment>.Update.Set(a => a.Status, status);
        await _appointments.UpdateOneAsync(filter, update);
    }

    public async Task DeleteAsync(string id) =>
        await _appointments.DeleteOneAsync(a => a.Id == id);
}
