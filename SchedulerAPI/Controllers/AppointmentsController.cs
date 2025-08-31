using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchedulerAPI.Models;
using SchedulerAPI.Services;

namespace SchedulerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "JWE")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly AuthService _authService;
    private readonly SupplierService _supplierService;

    public AppointmentsController(
        AppointmentService appointmentService, 
        AuthService authService,
        SupplierService supplierService)
    {
        _appointmentService = appointmentService;
        _authService = authService;
        _supplierService = supplierService;
    }

// Returns true if the token is valid
private bool IsTokenValid()
{
    if (!Request.Headers.ContainsKey("Authorization"))
        return false;

    var authHeader = Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return false;

    var token = authHeader.Substring("Bearer ".Length).Trim();

    try
    {
        var userId = _authService.ValidateToken(token);
        return !string.IsNullOrEmpty(userId);
    }
    catch
    {
        return false;
    }
}

// Returns the user ID if token is valid, otherwise null
private string? GetUserIdFromToken()
{
    if (!Request.Headers.ContainsKey("Authorization"))
        return null;

    var authHeader = Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return null;

    var token = authHeader.Substring("Bearer ".Length).Trim();

    try
    {
        return _authService.ValidateToken(token); // Returns userId or null if invalid
    }
    catch
    {
        return null;
    }
}


    [HttpGet]
    public async Task<ActionResult<List<Appointment>>> GetMyAppointments()
    {
         if (!IsTokenValid())
            return Unauthorized();

        var appointments = await _appointmentService.GetAllForUserAsync(GetUserIdFromToken());
        return appointments;
    }

    [HttpGet("supplier/{supplierId}")]
    public async Task<ActionResult<List<Appointment>>> GetSupplierAppointments(string supplierId)
    {
         if (!IsTokenValid())
            return Unauthorized();

        // Verify that the user owns this supplier
        var supplier = await _supplierService.GetByIdAsync(supplierId, GetUserIdFromToken());
        if (supplier == null)
            return NotFound("Supplier not found or you don't have access to it");

        var appointments = await _appointmentService.GetAllForSupplierAsync(supplierId);
        return appointments;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetById(string id)
    {
         if (!IsTokenValid())
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Check if user has access to this appointment
        if (appointment.UserId != GetUserIdFromToken())
        {
            // Check if user is the supplier for this appointment
            var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, GetUserIdFromToken());
            if (supplier == null)
                return Forbid();
        }

        return appointment;
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
         if (!IsTokenValid())
            return Unauthorized();

        try
        {
            appointment.UserId = GetUserIdFromToken();
            appointment.Status = "pending"; // Always start as pending
            await _appointmentService.CreateAsync(appointment);
            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] string status)
    {
         if (!IsTokenValid())
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Verify that the user is the supplier for this appointment
        var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, GetUserIdFromToken());
        if (supplier == null)
            return Forbid();

        if (!new[] { "scheduled", "completed", "cancelled", "pending" }.Contains(status))
            return BadRequest("Invalid status");

        await _appointmentService.UpdateStatusAsync(id, status);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
         if (!IsTokenValid())
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Check if user has access to delete this appointment
        if (appointment.UserId != GetUserIdFromToken())
        {
            // Check if user is the supplier for this appointment
            var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, GetUserIdFromToken());
            if (supplier == null)
                return Forbid();
        }

        await _appointmentService.DeleteAsync(id);
        return NoContent();
    }
}
