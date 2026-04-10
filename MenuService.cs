using CoffeeShopAPI.Data;
using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShopAPI.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _db;

    public MenuService(AppDbContext db) => _db = db;

    // ── Categories ────────────────────────────────────────────────────────────
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _db.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto
            {
                Id        = c.Id,
                Name      = c.Name,
                ImageUrl  = c.ImageUrl,
                IsActive  = c.IsActive,
                ItemCount = c.MenuItems.Count(m => m.IsAvailable)
            }).ToListAsync();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var c = await _db.Categories
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? null : MapCategory(c);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category { Name = request.Name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return MapCategory(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return null;

        category.Name     = request.Name;
        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return MapCategory(category);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return false;

        category.IsActive = false; // soft delete
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Menu Items ────────────────────────────────────────────────────────────
    public async Task<PagedResult<MenuItemDto>> GetMenuItemsAsync(int page, int pageSize, int? categoryId, bool? isAvailable)
    {
        var query = _db.MenuItems.Include(m => m.Category).AsQueryable();

        if (categoryId.HasValue)  query = query.Where(m => m.CategoryId == categoryId);
        if (isAvailable.HasValue) query = query.Where(m => m.IsAvailable == isAvailable);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.CategoryId).ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => MapMenuItem(m))
            .ToListAsync();

        return new PagedResult<MenuItemDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int id)
    {
        var m = await _db.MenuItems.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        return m == null ? null : MapMenuItem(m);
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemRequest request)
    {
        var item = new MenuItem
        {
            Name        = request.Name,
            Description = request.Description,
            Price       = request.Price,
            CategoryId  = request.CategoryId
        };
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(i => i.Category).LoadAsync();
        return MapMenuItem(item);
    }

    public async Task<MenuItemDto?> UpdateMenuItemAsync(int id, UpdateMenuItemRequest request)
    {
        var item = await _db.MenuItems.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return null;

        item.Name        = request.Name;
        item.Description = request.Description;
        item.Price       = request.Price;
        item.CategoryId  = request.CategoryId;
        item.IsAvailable = request.IsAvailable;
        await _db.SaveChangesAsync();
        return MapMenuItem(item);
    }

    public async Task<bool> DeleteMenuItemAsync(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return false;
        item.IsAvailable = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MenuItemDto?> UpdateMenuItemImageAsync(int id, string imageUrl)
    {
        var item = await _db.MenuItems.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return null;
        item.ImageUrl = imageUrl;
        await _db.SaveChangesAsync();
        return MapMenuItem(item);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────
    private static CategoryDto MapCategory(Category c) => new()
    {
        Id        = c.Id,
        Name      = c.Name,
        ImageUrl  = c.ImageUrl,
        IsActive  = c.IsActive,
        ItemCount = c.MenuItems?.Count(m => m.IsAvailable) ?? 0
    };

    private static MenuItemDto MapMenuItem(MenuItem m) => new()
    {
        Id           = m.Id,
        Name         = m.Name,
        Description  = m.Description,
        Price        = m.Price,
        ImageUrl     = m.ImageUrl,
        IsAvailable  = m.IsAvailable,
        CategoryId   = m.CategoryId,
        CategoryName = m.Category?.Name ?? string.Empty
    };
}
