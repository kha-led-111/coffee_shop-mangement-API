using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;
using CoffeeShopAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeShopAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController : ControllerBase
{
    private readonly ITableService _tableService;

    public TablesController(ITableService tableService) => _tableService = tableService;

    // ── Tables ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TableDto>>>> GetTables()
    {
        var result = await _tableService.GetTablesAsync();
        return Ok(ApiResponse<List<TableDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TableDto>>> GetTable(int id)
    {
        var result = await _tableService.GetTableByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<TableDto>.Fail("Table not found."));
        return Ok(ApiResponse<TableDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TableDto>>> CreateTable([FromBody] CreateTableRequest request)
    {
        var result = await _tableService.CreateTableAsync(request);
        return CreatedAtAction(nameof(GetTable), new { id = result.Id },
            ApiResponse<TableDto>.Ok(result, "Table created."));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<TableDto>>> UpdateTableStatus(int id, [FromBody] UpdateTableStatusRequest request)
    {
        var result = await _tableService.UpdateTableStatusAsync(id, request.Status);
        if (result == null) return NotFound(ApiResponse<TableDto>.Fail("Table not found."));
        return Ok(ApiResponse<TableDto>.Ok(result, "Table status updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTable(int id)
    {
        var result = await _tableService.DeleteTableAsync(id);
        if (!result) return NotFound(ApiResponse<object>.Fail("Table not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Table deleted."));
    }

    // ── Reservations ──────────────────────────────────────────────────────────

    /// <summary>GET /api/tables/reservations?date=2024-01-15</summary>
    [HttpGet("reservations")]
    public async Task<ActionResult<ApiResponse<List<ReservationDto>>>> GetReservations([FromQuery] DateTime? date)
    {
        var result = await _tableService.GetReservationsAsync(date);
        return Ok(ApiResponse<List<ReservationDto>>.Ok(result));
    }

    [HttpGet("reservations/{id}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> GetReservation(int id)
    {
        var result = await _tableService.GetReservationByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<ReservationDto>.Fail("Reservation not found."));
        return Ok(ApiResponse<ReservationDto>.Ok(result));
    }

    [HttpPost("reservations")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var result = await _tableService.CreateReservationAsync(request);
        return CreatedAtAction(nameof(GetReservation), new { id = result.Id },
            ApiResponse<ReservationDto>.Ok(result, "Reservation confirmed."));
    }

    [HttpPatch("reservations/{id}/status")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> UpdateReservationStatus(
        int id, [FromQuery] ReservationStatus status)
    {
        var result = await _tableService.UpdateReservationStatusAsync(id, status);
        if (result == null) return NotFound(ApiResponse<ReservationDto>.Fail("Reservation not found."));
        return Ok(ApiResponse<ReservationDto>.Ok(result, "Reservation status updated."));
    }

    [HttpDelete("reservations/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReservation(int id)
    {
        var result = await _tableService.DeleteReservationAsync(id);
        if (!result) return NotFound(ApiResponse<object>.Fail("Reservation not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Reservation deleted."));
    }
}
