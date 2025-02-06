using Microsoft.AspNetCore.Mvc;
using BitfinexConnector.Models;

namespace BitfinexConnectorAPI.Controllers
{
    public class PortfolioMVCController : Controller
    {
        private readonly HttpClient _httpClient;

        public PortfolioMVCController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7038/api/"); 
        }

        public async Task<IActionResult> Index()
        {
            var balance = await _httpClient.GetFromJsonAsync<PortfolioBalance>("portfolio");

            return View("~/Views/Index.cshtml", balance);
        }
    }
}