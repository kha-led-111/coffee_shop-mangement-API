using CoffeeShopAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace CoffeeShopAPI.DTOs;

// ── Generic Response Wrapper ──────────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool    Success { get; set; }
    public string  Message { get; set; } = string.Empty;
    public T?      Data    { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class PagedResult<T>
{
    public List<T> Items       { get; set; } = new();
    public int     TotalCount  { get; set; }
    public int     Page        { get; set; }
    public int     PageSize    { get; set; }
    public int     TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// ── Auth DTOs ─────────────────────────────────────────────────────────────────
public class LoginRequest
{
    [Required, EmailAddress] public string Email    { get; set; } = string.Empty;
    [Required]               public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]               public string Name     { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email    { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
}

public class AuthResponse
{
    public string Token     { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int    ExpiresIn { get; set; }
    public UserDto User     { get; set; } = null!;
}

public class UserDto
{
    public int    Id    { get; set; }
    public string Name  { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role  { get; set; } = string.Empty;
}

// ── Category DTOs ─────────────────────────────────────────────────────────────
public class CategoryDto
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool    IsActive { get; set; }
    public int     ItemCount { get; set; }
}

public class CreateCategoryRequest
{
    [Required] public string Name { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    [Required] public string Name     { get; set; } = string.Empty;
    public bool IsActive              { get; set; } = true;
}

// ── Menu DTOs ─────────────────────────────────────────────────────────────────
public class MenuItemDto
{
    public int     Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public decimal Price        { get; set; }
    public string? ImageUrl     { get; set; }
    public bool    IsAvailable  { get; set; }
    public int     CategoryId   { get; set; }
    public string  CategoryName { get; set; } = string.Empty;
}

public class CreateMenuItemRequest
{
    [Required]        public string  Name        { get; set; } = string.Empty;
    public string?    Description   { get; set; }
    [Range(0, 99999)] public decimal Price       { get; set; }
    [Required]        public int     CategoryId  { get; set; }
}

public class UpdateMenuItemRequest : CreateMenuItemRequest
{
    public bool IsAvailable { get; set; } = true;
}

// ── Table DTOs ────────────────────────────────────────────────────────────────
public class TableDto
{
    public int         Id          { get; set; }
    public string      TableNumber { get; set; } = string.Empty;
    public int         Capacity    { get; set; }
    public TableStatus Status      { get; set; }
    public string      StatusLabel { get; set; } = string.Empty;
    public string?     QrCode      { get; set; }
}

public class CreateTableRequest
{
    [Required]        public string TableNumber { get; set; } = string.Empty;
    [Range(1, 20)]    public int    Capacity    { get; set; }
}

public class UpdateTableStatusRequest
{
    [Required] public TableStatus Status { get; set; }
}

// ── Reservation DTOs ──────────────────────────────────────────────────────────
public class ReservationDto
{
    public int                Id            { get; set; }
    public string             CustomerName  { get; set; } = string.Empty;
    public string             CustomerPhone { get; set; } = string.Empty;
    public int                GuestCount    { get; set; }
    public DateTime           ReservedAt    { get; set; }
    public ReservationStatus  Status        { get; set; }
    public string?            Notes         { get; set; }
    public int                TableId       { get; set; }
    public string             TableNumber   { get; set; } = string.Empty;
}

public class CreateReservationRequest
{
    [Required]     public string   CustomerName  { get; set; } = string.Empty;
    [Required]     public string   CustomerPhone { get; set; } = string.Empty;
    [Range(1, 20)] public int      GuestCount    { get; set; }
    [Required]     public DateTime ReservedAt    { get; set; }
    [Required]     public int      TableId       { get; set; }
    public string? Notes { get; set; }
}

// ── Order DTOs ────────────────────────────────────────────────────────────────
public class OrderDto
{
    public int         Id          { get; set; }
    public string      OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status      { get; set; }
    public string      StatusLabel { get; set; } = string.Empty;
    public OrderType   Type        { get; set; }
    public decimal     TotalAmount { get; set; }
    public string?     Notes       { get; set; }
    public DateTime    CreatedAt   { get; set; }
    public int?        TableId     { get; set; }
    public string?     TableNumber { get; set; }
    public string      CashierName { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int     Id           { get; set; }
    public int     MenuItemId   { get; set; }
    public string  MenuItemName { get; set; } = string.Empty;
    public string? ImageUrl     { get; set; }
    public int     Quantity     { get; set; }
    public decimal UnitPrice    { get; set; }
    public decimal SubTotal     { get; set; }
    public string? Notes        { get; set; }
}

public class CreateOrderRequest
{
    public OrderType   Type    { get; set; } = OrderType.DineIn;
    public int?        TableId { get; set; }
    public string?     Notes   { get; set; }
    [Required, MinLength(1)] public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Required]     public int    MenuItemId { get; set; }
    [Range(1, 99)] public int    Quantity   { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required] public OrderStatus Status { get; set; }
}

// ── Order Status Update (SignalR payload) ─────────────────────────────────────
public class OrderStatusUpdate
{
    public int         OrderId     { get; set; }
    public string      OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status      { get; set; }
    public string      StatusLabel { get; set; } = string.Empty;
    public DateTime    UpdatedAt   { get; set; }
}
