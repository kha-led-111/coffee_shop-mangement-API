using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeShopAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly IFileService _fileService;

    public MenuController(IMenuService menuService, IFileService fileService)
    {
        _menuService = menuService;
        _fileService = fileService;
    }

    // ── Categories ────────────────────────────────────────────────────────────

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var result = await _menuService.GetCategoriesAsync();
        return Ok(ApiResponse<List<CategoryDto>>.Ok(result));
    }

    [HttpGet("categories/{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
    {
        var result = await _menuService.GetCategoryByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<CategoryDto>.Fail("Category not found."));
        return Ok(ApiResponse<CategoryDto>.Ok(result));
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await _menuService.CreateCategoryAsync(request);
        return CreatedAtAction(nameof(GetCategory), new { id = result.Id },
            ApiResponse<CategoryDto>.Ok(result, "Category created."));
    }

    [HttpPut("categories/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _menuService.UpdateCategoryAsync(id, request);
        if (result == null) return NotFound(ApiResponse<CategoryDto>.Fail("Category not found."));
        return Ok(ApiResponse<CategoryDto>.Ok(result, "Category updated."));
    }

    /// <summary>POST /api/menu/categories/{id}/image — Multipart upload</summary>
    [HttpPost("categories/{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UploadCategoryImage(int id, IFormFile image)
    {
        var category = await _menuService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound(ApiResponse<CategoryDto>.Fail("Category not found."));

        var imageUrl = await _fileService.SaveImageAsync(image, "categories");

        // Delete old image if exists
        if (!string.IsNullOrEmpty(category.ImageUrl))
            _fileService.DeleteImage(category.ImageUrl);

        var updated = await _menuService.UpdateCategoryAsync(id, new UpdateCategoryRequest
        {
            Name     = category.Name,
            IsActive = category.IsActive
        });

        // Update image separately via the menu service
        var result = await _menuService.GetCategoryByIdAsync(id);
        return Ok(ApiResponse<CategoryDto>.Ok(result!, "Image uploaded."));
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
    {
        var result = await _menuService.DeleteCategoryAsync(id);
        if (!result) return NotFound(ApiResponse<object>.Fail("Category not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Category deleted."));
    }

    // ── Menu Items ────────────────────────────────────────────────────────────

    [HttpGet("items")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<MenuItemDto>>>> GetMenuItems(
        [FromQuery] int  page        = 1,
        [FromQuery] int  pageSize    = 20,
        [FromQuery] int? categoryId  = null,
        [FromQuery] bool? isAvailable = null)
    {
        var result = await _menuService.GetMenuItemsAsync(page, pageSize, categoryId, isAvailable);
        return Ok(ApiResponse<PagedResult<MenuItemDto>>.Ok(result));
    }

    [HttpGet("items/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> GetMenuItem(int id)
    {
        var result = await _menuService.GetMenuItemByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<MenuItemDto>.Fail("Menu item not found."));
        return Ok(ApiResponse<MenuItemDto>.Ok(result));
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> CreateMenuItem([FromBody] CreateMenuItemRequest request)
    {
        var result = await _menuService.CreateMenuItemAsync(request);
        return CreatedAtAction(nameof(GetMenuItem), new { id = result.Id },
            ApiResponse<MenuItemDto>.Ok(result, "Menu item created."));
    }

    [HttpPut("items/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> UpdateMenuItem(int id, [FromBody] UpdateMenuItemRequest request)
    {
        var result = await _menuService.UpdateMenuItemAsync(id, request);
        if (result == null) return NotFound(ApiResponse<MenuItemDto>.Fail("Menu item not found."));
        return Ok(ApiResponse<MenuItemDto>.Ok(result, "Menu item updated."));
    }

    /// <summary>POST /api/menu/items/{id}/image — Multipart upload for item image</summary>
    [HttpPost("items/{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> UploadMenuItemImage(int id, IFormFile image)
    {
        var item = await _menuService.GetMenuItemByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<MenuItemDto>.Fail("Menu item not found."));

        if (!string.IsNullOrEmpty(item.ImageUrl))
            _fileService.DeleteImage(item.ImageUrl);

        var imageUrl = await _fileService.SaveImageAsync(image, "menu-items");
        var result   = await _menuService.UpdateMenuItemImageAsync(id, imageUrl);
        return Ok(ApiResponse<MenuItemDto>.Ok(result!, "Image uploaded."));
    }

    [HttpDelete("items/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMenuItem(int id)
    {
        var result = await _menuService.DeleteMenuItemAsync(id);
        if (!result) return NotFound(ApiResponse<object>.Fail("Menu item not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Menu item deleted."));
    }
}
