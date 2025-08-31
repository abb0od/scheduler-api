using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchedulerAPI.Models;
using SchedulerAPI.Services;

namespace SchedulerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly SupplierService _supplierService;
    private readonly AuthService _authService;

    public SuppliersController(SupplierService supplierService, AuthService authService)
    {
        _supplierService = supplierService;
        _authService = authService;
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
    public async Task<ActionResult<List<Supplier>>> GetAll()
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var suppliers = await _supplierService.GetAllAsync(userId);
        return suppliers;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Supplier>> GetById(string id)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var supplier = await _supplierService.GetByIdAsync(id, userId);
        if (supplier == null)
            return NotFound();

        return supplier;
    }

    [HttpPost]
    public async Task<ActionResult<Supplier>> Create(Supplier supplier)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        supplier.UserId = userId;
        await _supplierService.CreateAsync(supplier);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Supplier supplier)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var existingSupplier = await _supplierService.GetByIdAsync(id, userId);
        if (existingSupplier == null)
            return NotFound();

        supplier.Id = id;
        supplier.UserId = userId;
        await _supplierService.UpdateAsync(id, supplier);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = await GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();

        var supplier = await _supplierService.GetByIdAsync(id, userId);
        if (supplier == null)
            return NotFound();

        await _supplierService.DeleteAsync(id, userId);
        return NoContent();
    }
}
