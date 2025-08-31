using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchedulerAPI.Models;
using SchedulerAPI.Services;

namespace SchedulerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    private async Task<string?> GetUserIdFromToken()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length);
        return _authService.ValidateToken(token);
    }

    [HttpGet]
    public async Task<ActionResult<List<Appointment>>> GetMyAppointments()
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var appointments = await _appointmentService.GetAllForUserAsync(userId);
        return appointments;
    }

    [HttpGet("supplier/{supplierId}")]
    public async Task<ActionResult<List<Appointment>>> GetSupplierAppointments(string supplierId)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        // Verify that the user owns this supplier
        var supplier = await _supplierService.GetByIdAsync(supplierId, userId);
        if (supplier == null)
            return NotFound("Supplier not found or you don't have access to it");

        var appointments = await _appointmentService.GetAllForSupplierAsync(supplierId);
        return appointments;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetById(string id)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Check if user has access to this appointment
        if (appointment.UserId != userId)
        {
            // Check if user is the supplier for this appointment
            var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, userId);
            if (supplier == null)
                return Forbid();
        }

        return appointment;
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        try
        {
            appointment.UserId = userId;
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
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Verify that the user is the supplier for this appointment
        var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, userId);
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
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null)
            return NotFound();

        // Check if user has access to delete this appointment
        if (appointment.UserId != userId)
        {
            // Check if user is the supplier for this appointment
            var supplier = await _supplierService.GetByIdAsync(appointment.SupplierId, userId);
            if (supplier == null)
                return Forbid();
        }

        await _appointmentService.DeleteAsync(id);
        return NoContent();
    }
}
