using BitfinexConnector.Interfaces;
using BitfinexConnector.Models;
using Microsoft.AspNetCore.Mvc;

namespace BitfinexConnectorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioCalculator _portfolioCalculator;

        public PortfolioController(IPortfolioCalculator portfolioCalculator)
        {
            _portfolioCalculator = portfolioCalculator;
        }

        [HttpGet]
        public async Task<ActionResult<PortfolioBalance>> GetPortfolio()
        {
            var balance = await _portfolioCalculator.CalculatePortfolioAsync();
            
            return Ok(balance);
        }
    }
}