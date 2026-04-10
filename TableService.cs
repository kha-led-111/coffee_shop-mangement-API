using CoffeeShopAPI.Data;
using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShopAPI.Services;

public class TableService : ITableService
{
    private readonly AppDbContext _db;

    public TableService(AppDbContext db) => _db = db;

    // ── Tables ────────────────────────────────────────────────────────────────
    public async Task<List<TableDto>> GetTablesAsync()
    {
        return await _db.Tables.Select(t => MapTable(t)).ToListAsync();
    }

    public async Task<TableDto?> GetTableByIdAsync(int id)
    {
        var t = await _db.Tables.FindAsync(id);
        return t == null ? null : MapTable(t);
    }

    public async Task<TableDto> CreateTableAsync(CreateTableRequest request)
    {
        var table = new Table
        {
            TableNumber = request.TableNumber,
            Capacity    = request.Capacity
        };
        _db.Tables.Add(table);
        await _db.SaveChangesAsync();
        return MapTable(table);
    }

    public async Task<TableDto?> UpdateTableStatusAsync(int id, TableStatus status)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return null;
        table.Status = status;
        await _db.SaveChangesAsync();
        return MapTable(table);
    }

    public async Task<bool> DeleteTableAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return false;
        _db.Tables.Remove(table);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Reservations ──────────────────────────────────────────────────────────
    public async Task<List<ReservationDto>> GetReservationsAsync(DateTime? date)
    {
        var query = _db.Reservations.Include(r => r.Table).AsQueryable();
        if (date.HasValue)
            query = query.Where(r => r.ReservedAt.Date == date.Value.Date);

        return await query
            .OrderBy(r => r.ReservedAt)
            .Select(r => MapReservation(r))
            .ToListAsync();
    }

    public async Task<ReservationDto?> GetReservationByIdAsync(int id)
    {
        var r = await _db.Reservations.Include(x => x.Table).FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? null : MapReservation(r);
    }

    public async Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request)
    {
        var reservation = new Reservation
        {
            CustomerName  = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            GuestCount    = request.GuestCount,
            ReservedAt    = request.ReservedAt,
            TableId       = request.TableId,
            Notes         = request.Notes,
            Status        = ReservationStatus.Confirmed
        };

        // Mark table as reserved
        var table = await _db.Tables.FindAsync(request.TableId);
        if (table != null) table.Status = TableStatus.Reserved;

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();
        await _db.Entry(reservation).Reference(r => r.Table).LoadAsync();
        return MapReservation(reservation);
    }

    public async Task<ReservationDto?> UpdateReservationStatusAsync(int id, ReservationStatus status)
    {
        var reservation = await _db.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.Id == id);
        if (reservation == null) return null;

        reservation.Status = status;

        // Free the table if reservation is cancelled/completed
        if (status is ReservationStatus.Cancelled or ReservationStatus.Completed)
        {
            var table = await _db.Tables.FindAsync(reservation.TableId);
            if (table?.Status == TableStatus.Reserved)
                table.Status = TableStatus.Available;
        }

        await _db.SaveChangesAsync();
        return MapReservation(reservation);
    }

    public async Task<bool> DeleteReservationAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation == null) return false;
        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Mappers ───────────────────────────────────────────────────────────────
    private static TableDto MapTable(Table t) => new()
    {
        Id          = t.Id,
        TableNumber = t.TableNumber,
        Capacity    = t.Capacity,
        Status      = t.Status,
        StatusLabel = t.Status.ToString(),
        QrCode      = t.QrCode
    };

    private static ReservationDto MapReservation(Reservation r) => new()
    {
        Id            = r.Id,
        CustomerName  = r.CustomerName,
        CustomerPhone = r.CustomerPhone,
        GuestCount    = r.GuestCount,
        ReservedAt    = r.ReservedAt,
        Status        = r.Status,
        Notes         = r.Notes,
        TableId       = r.TableId,
        TableNumber   = r.Table?.TableNumber ?? string.Empty
    };
}
