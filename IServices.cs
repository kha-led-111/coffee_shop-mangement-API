using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;

namespace CoffeeShopAPI.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserDto?> RegisterAsync(RegisterRequest request);
    Task<UserDto?> GetUserByIdAsync(int id);
}

public interface ITokenService
{
    string GenerateToken(User user);
}

public interface IMenuService
{
    Task<List<CategoryDto>>         GetCategoriesAsync();
    Task<CategoryDto?>              GetCategoryByIdAsync(int id);
    Task<CategoryDto>               CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryDto?>              UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    Task<bool>                      DeleteCategoryAsync(int id);

    Task<PagedResult<MenuItemDto>>  GetMenuItemsAsync(int page, int pageSize, int? categoryId, bool? isAvailable);
    Task<MenuItemDto?>              GetMenuItemByIdAsync(int id);
    Task<MenuItemDto>               CreateMenuItemAsync(CreateMenuItemRequest request);
    Task<MenuItemDto?>              UpdateMenuItemAsync(int id, UpdateMenuItemRequest request);
    Task<bool>                      DeleteMenuItemAsync(int id);
    Task<MenuItemDto?>              UpdateMenuItemImageAsync(int id, string imageUrl);
}

public interface IOrderService
{
    Task<PagedResult<OrderDto>>  GetOrdersAsync(int page, int pageSize, OrderStatus? status);
    Task<OrderDto?>              GetOrderByIdAsync(int id);
    Task<OrderDto>               CreateOrderAsync(CreateOrderRequest request, int cashierId);
    Task<OrderDto?>              UpdateOrderStatusAsync(int id, OrderStatus status);
    Task<bool>                   CancelOrderAsync(int id);
}

public interface ITableService
{
    Task<List<TableDto>>    GetTablesAsync();
    Task<TableDto?>         GetTableByIdAsync(int id);
    Task<TableDto>          CreateTableAsync(CreateTableRequest request);
    Task<TableDto?>         UpdateTableStatusAsync(int id, TableStatus status);
    Task<bool>              DeleteTableAsync(int id);

    Task<List<ReservationDto>>  GetReservationsAsync(DateTime? date);
    Task<ReservationDto?>       GetReservationByIdAsync(int id);
    Task<ReservationDto>        CreateReservationAsync(CreateReservationRequest request);
    Task<ReservationDto?>       UpdateReservationStatusAsync(int id, ReservationStatus status);
    Task<bool>                  DeleteReservationAsync(int id);
}

public interface IFileService
{
    Task<string> SaveImageAsync(IFormFile file, string folder);
    bool         DeleteImage(string imageUrl);
}
