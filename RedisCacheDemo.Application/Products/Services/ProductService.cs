using RedisCacheDemo.Application.Abstractions.Caching;
using RedisCacheDemo.Application.Abstractions.Persistence;
using RedisCacheDemo.Application.Products.DTOs;

namespace RedisCacheDemo.Application.Products.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public ProductService(
        IProductRepository productRepository,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";

        var cachedProduct =
            await _cacheService.GetAsync<ProductDto>(cacheKey);

        if (cachedProduct is not null)
        {
            return cachedProduct;
        }

        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        var productDto = new ProductDto(
            product.Id,
            product.Nombre,
            product.Descripcion,
            product.Precio);

        await _cacheService.SetAsync(
            cacheKey,
            productDto,
            TimeSpan.FromMinutes(10));

        return productDto;
    }
}