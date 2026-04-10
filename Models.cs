using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeShopAPI.Models;

// ── User ──────────────────────────────────────────────────────────────────────
public class User
{
    public int    Id           { get; set; }
    [Required] public string Name     { get; set; } = string.Empty;
    [Required] public string Email    { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty; // hashed
    public string Role        { get; set; } = "Cashier"; // Admin | Cashier | Barista
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive      { get; set; } = true;
}

// ── Category ──────────────────────────────────────────────────────────────────
public class Category
{
    public int    Id          { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? ImageUrl   { get; set; }
    public bool   IsActive    { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

// ── MenuItem ──────────────────────────────────────────────────────────────────
public class MenuItem
{
    public int    Id          { get; set; }
    [Required] public string Name        { get; set; } = string.Empty;
    public string? Description           { get; set; }
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price      { get; set; }
    public string? ImageUrl   { get; set; }
    public bool   IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId     { get; set; }
    public Category Category  { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

// ── Table ─────────────────────────────────────────────────────────────────────
public class Table
{
    public int    Id           { get; set; }
    [Required] public string TableNumber { get; set; } = string.Empty;
    public int    Capacity     { get; set; }
    public TableStatus Status  { get; set; } = TableStatus.Available;
    public string? QrCode      { get; set; }

    public ICollection<Order>       Orders       { get; set; } = new List<Order>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public enum TableStatus { Available, Occupied, Reserved, OutOfService }

// ── Reservation ───────────────────────────────────────────────────────────────
public class Reservation
{
    public int    Id              { get; set; }
    [Required] public string CustomerName  { get; set; } = string.Empty;
    [Required] public string CustomerPhone { get; set; } = string.Empty;
    public int    GuestCount      { get; set; }
    public DateTime ReservedAt    { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Notes          { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    public int   TableId { get; set; }
    public Table Table   { get; set; } = null!;
}

public enum ReservationStatus { Pending, Confirmed, Cancelled, Completed }

// ── Order ─────────────────────────────────────────────────────────────────────
public class Order
{
    public int    Id          { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // e.g. ORD-20240101-001
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderType   Type   { get; set; } = OrderType.DineIn;
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }
    public string? Notes      { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int?  TableId { get; set; }
    public Table? Table  { get; set; }

    public int  CashierId { get; set; }
    public User Cashier   { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public enum OrderStatus  { Pending, Preparing, Ready, Delivered, Cancelled }
public enum OrderType    { DineIn, Takeaway }

// ── OrderItem ─────────────────────────────────────────────────────────────────
public class OrderItem
{
    public int Id       { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal  { get; set; }
    public string? Notes     { get; set; }

    public int      OrderId    { get; set; }
    public Order    Order      { get; set; } = null!;

    public int      MenuItemId { get; set; }
    public MenuItem MenuItem   { get; set; } = null!;
}
