using BitfinexConnector.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BitfinexConnectorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradesController : ControllerBase
    {
        private readonly ITestConnector _connector;

        public TradesController(ITestConnector connector)
        {
            _connector = connector;
        }

        [HttpGet("{pair}/{maxCount}")]
        public async Task<IActionResult> GetTradesAsync(string pair, int maxCount)
        {
            var trades = await _connector.GetNewTradesAsync(pair, maxCount);
            
            return Ok(trades);
        }

        [HttpGet("candles/{pair}/{periodInSec}")]
        public async Task<IActionResult> GetCandlesAsync(string pair, int periodInSec, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] long? count = 100)
        {
            var candles = await _connector.GetCandleSeriesAsync(pair, periodInSec, from, to, count);
            
            return Ok(candles);
        }
    }
}