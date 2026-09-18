using RedisCacheDemo.Domain.Entities;

namespace RedisCacheDemo.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);

    Task<IReadOnlyCollection<Product>> GetAllAsync();
}