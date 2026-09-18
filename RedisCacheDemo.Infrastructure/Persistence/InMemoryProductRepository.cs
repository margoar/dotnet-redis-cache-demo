using RedisCacheDemo.Application.Abstractions.Persistence;
using RedisCacheDemo.Domain.Entities;

namespace RedisCacheDemo.Infrastructure.Persistence;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products =
    [
        new Product
        {
            Id = 1,
            Nombre = "Teclado mecánico",
            Descripcion = "Teclado mecánico RGB",
            Precio = 59_990
        },
        new Product
        {
            Id = 2,
            Nombre = "Mouse inalámbrico",
            Descripcion = "Mouse inalámbrico ergonómico",
            Precio = 29_990
        },
        new Product
        {
            Id = 3,
            Nombre = "Monitor 27 pulgadas",
            Descripcion = "Monitor IPS 27 pulgadas",
            Precio = 189_990
        }
    ];

    public Task<Product?> GetByIdAsync(int id)
    {
        var product = _products.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(product);
    }

    public Task<IReadOnlyCollection<Product>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Product>>(_products);
    }
}