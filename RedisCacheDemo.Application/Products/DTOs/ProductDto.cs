using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCacheDemo.Application.Products.DTOs
{
    public sealed record ProductDto(
     int Id,
     string Nombre,
     string Descripcion,
     decimal Precio);
}
