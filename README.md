# dotnet-redis-cache-demo

API REST en .NET 8 que demuestra el uso de **Redis como caché distribuida** mediante el patrón **Cache-Aside**, organizada en capas siguiendo los principios de Clean Architecture.

## Qué hace el proyecto

La API expone un endpoint para consultar productos por ID. La primera vez que se solicita un producto, se obtiene desde el repositorio y se guarda en Redis. Las solicitudes siguientes se responden directamente desde Redis mientras la entrada no expire.

**Tecnologías**

- .NET 8 / ASP.NET Core Web API
- Redis (ejecutado en Docker)
- StackExchange.Redis
- System.Text.Json para serializar los valores guardados en caché
- Swagger (disponible en entorno Development)

## Redis en este proyecto

Redis es un almacén clave-valor en memoria. Aquí cumple el rol de **caché distribuida**: vive fuera del proceso de la API, por lo que los datos cacheados sobreviven a un reinicio de la aplicación y pueden compartirse entre varias instancias.

Cada producto se guarda como un string JSON bajo la key `product:{id}`, con una expiración de 10 minutos.

### Cache-Aside

En el patrón Cache-Aside la aplicación es responsable de gestionar la caché: primero consulta Redis y, solo si el dato no está, lo busca en el origen y lo guarda en Redis para las siguientes lecturas. Redis no conoce el origen de los datos.

### Cache Hit y Cache Miss

- **Cache Hit**: la key existe en Redis. El producto se devuelve desde la caché y no se consulta el repositorio.
- **Cache Miss**: la key no existe (primera consulta o expiró). Se consulta el repositorio y el resultado se guarda en Redis.

### Flujo implementado

```
Cliente ──► GET /api/products/1
                │
                ▼
        ProductService consulta Redis ("product:1")
                │
        ┌───────┴────────┐
        │ Existe         │ No existe
        ▼                ▼
   Cache Hit        Cache Miss
        │                │
        │                ├─► Consulta IProductRepository
        │                └─► Guarda en Redis (TTL: 10 min)
        │                │
        └───────┬────────┘
                ▼
        Devuelve el producto
```

Implementación en `ProductService`:

```csharp
public async Task<ProductDto?> GetByIdAsync(int id)
{
    var cacheKey = $"product:{id}";

    var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);

    if (cachedProduct is not null)
    {
        return cachedProduct;                                   // Cache Hit
    }

    var product = await _productRepository.GetByIdAsync(id);   // Cache Miss

    if (product is null)
    {
        return null;
    }

    var productDto = new ProductDto(
        product.Id,
        product.Nombre,
        product.Descripcion,
        product.Precio);

    await _cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(10));

    return productDto;
}
```

Si el producto no existe en el repositorio, el servicio devuelve `null`, no se guarda nada en Redis y la API responde `404 Not Found`.

## Arquitectura

```
Api
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application
 ↓
Domain
```

- **Domain** no depende de ningún otro proyecto.
- **Application** depende solo de Domain.
- **Infrastructure** implementa las abstracciones definidas en Application.
- **Api** es el punto de entrada y registra las dependencias.

### Por qué Application depende de abstracciones

`ProductService` no conoce Redis ni StackExchange.Redis. Depende de dos interfaces definidas en la propia capa Application:

- `ICacheService`: operaciones de caché (`GetAsync`, `SetAsync`, `RemoveAsync`).
- `IProductRepository`: acceso a los productos.

Las implementaciones concretas (`RedisCacheService` e `InMemoryProductRepository`) viven en Infrastructure y se conectan mediante Dependency Injection. Esto aplica el **principio de inversión de dependencias**: la lógica de la aplicación no queda acoplada a una tecnología concreta, y cambiar la implementación de caché no requiere modificar `ProductService`.

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}
```

### Por qué `IConnectionMultiplexer` se registra como Singleton

`ConnectionMultiplexer` es el objeto de StackExchange.Redis que gestiona la conexión con el servidor. Es costoso de crear, es thread-safe y está diseñado para **reutilizarse durante toda la vida de la aplicación**. Crear una conexión por cada request agregaría latencia y podría agotar las conexiones disponibles.

```csharp
services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var redisOptions = serviceProvider
        .GetRequiredService<IOptions<RedisOptions>>()
        .Value;

    return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
});
```

## Estructura del proyecto

```
dotnet-redis-cache-demo
├── dotnet-redis-cache-demo              # Proyecto Razor Pages original
├── RedisCacheDemo.Api
│   ├── Controllers/ProductsController.cs
│   ├── Program.cs
│   └── appsettings.json
├── RedisCacheDemo.Application
│   ├── Abstractions/
│   │   ├── Caching/ICacheService.cs
│   │   └── Persistence/IProductRepository.cs
│   └── Products/
│       ├── DTOs/ProductDto.cs
│       └── Services/
│           ├── IProductService.cs
│           └── ProductService.cs
├── RedisCacheDemo.Domain
│   └── Entities/Product.cs
└── RedisCacheDemo.Infrastructure
    ├── Caching/RedisCacheService.cs
    ├── Configuration/RedisOptions.cs
    ├── DependencyInjection/ServiceCollectionExtensions.cs
    └── Persistence/InMemoryProductRepository.cs
```

| Proyecto | Contenido |
|---|---|
| **Domain** | Entidad `Product` (`Id`, `Nombre`, `Descripcion`, `Precio`). |
| **Application** | `ICacheService`, `IProductRepository`, `ProductDto`, `IProductService` y `ProductService`. |
| **Infrastructure** | `RedisCacheService` (StackExchange.Redis), `InMemoryProductRepository`, `RedisOptions` y el registro de dependencias. |
| **Api** | `ProductsController` con el endpoint `GET /api/products/{id}` y la configuración en `appsettings.json`. |

`InMemoryProductRepository` contiene tres productos de ejemplo en memoria (IDs 1, 2 y 3); el proyecto no usa base de datos.

## Configuración

La conexión a Redis se define en `RedisCacheDemo.Api/appsettings.json`:

```json
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

Esta sección se enlaza a la clase `RedisOptions` y se usa al crear el `IConnectionMultiplexer`.

## Requisitos

- .NET SDK 8.0 o superior
- Docker

## Ejecución

### 1. Levantar Redis con Docker

```bash
docker run -d --name redis-cache-demo -p 6379:6379 redis:7-alpine
```

También puede levantarse con el `docker-compose.yml` incluido en el repositorio:

```bash
docker compose up -d
```

### 2. Comprobar que Redis está funcionando

```bash
docker exec -it redis-cache-demo redis-cli ping
```

Respuesta esperada:

```
PONG
```

### 3. Ejecutar la API

```bash
dotnet restore
dotnet build
dotnet run --project RedisCacheDemo.Api
```

La API queda disponible en `http://localhost:5146` y Swagger en `http://localhost:5146/swagger`.

> Redis debe estar en ejecución antes de iniciar la API.

## Probar el endpoint

### `GET /api/products/{id}`

```bash
curl http://localhost:5146/api/products/1
```

Respuesta `200 OK`:

```json
{
  "id": 1,
  "nombre": "Teclado mecánico",
  "descripcion": "Teclado mecánico RGB",
  "precio": 59990
}
```

Si el producto no existe (por ejemplo, `/api/products/99`), la API responde `404 Not Found`.

## Verificar la caché en Redis

Después del primer request a `/api/products/1`, el producto queda guardado en Redis.

### Consultar la key `product:1`

```bash
docker exec -it redis-cache-demo redis-cli GET product:1
```

```
"{\"Id\":1,\"Nombre\":\"Teclado mec\\u00E1nico\",\"Descripcion\":\"Teclado mec\\u00E1nico RGB\",\"Precio\":59990}"
```

### Comprobar el TTL

```bash
docker exec -it redis-cache-demo redis-cli TTL product:1
```

```
(integer) 587
```

El valor indica los segundos restantes antes de que la key expire (máximo 600, es decir, 10 minutos). Cuando llega a cero, Redis elimina la key y el siguiente request vuelve a ser un Cache Miss.

Mientras la key exista, las siguientes llamadas a `GET /api/products/1` se responden desde Redis (Cache Hit).
