using RedisCacheDemo.Application.Products.DTOs;

namespace RedisCacheDemo.Application.Products.Services;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
}