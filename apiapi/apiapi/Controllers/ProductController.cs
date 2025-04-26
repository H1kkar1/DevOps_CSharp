using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using apiapi.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System;

namespace apiapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductController> _logger;
        private readonly IConfiguration _configuration;

        public ProductController(
            DataContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<ProductController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Сохраняем в базу данных
            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            try
            {
                // Отправляем данные в другой сервис
                await NotifyExternalService(product);
                _logger.LogInformation("Product notified to external service: {ProductId}", product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying external service about product");
                // Можно добавить компенсирующую логику или ретраи
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        private async Task NotifyExternalService(Product product)
        {
            var client = _httpClientFactory.CreateClient("ExternalService");
            var endpoint = _configuration["ExternalService:ProductsEndpoint"] ?? "products";

            var response = await client.PostAsJsonAsync(endpoint, new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Quantity,
                CreatedAt = DateTime.UtcNow
            });

            response.EnsureSuccessStatusCode();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }
    }

}
