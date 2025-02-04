using BitfinexConnector.Clients;
using BitfinexConnector.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BitfinexConnectorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly ITestSocketConnector _socketConnector;

        public WebSocketController(ITestSocketConnector socketConnector)
        {
            _socketConnector = socketConnector;
        }

        [HttpPost("subscribe/trades/{pair}")]
        public IActionResult SubscribeTrades(string pair, int maxCount = 100)
        {
            _socketConnector.SubscribeTrades(pair, maxCount);
            
            return Ok($"Subscribed to trades for {pair}");
        }

        [HttpPost("unsubscribe/trades/{pair}")]
        public IActionResult UnsubscribeTrades(string pair)
        {
            _socketConnector.UnsubscribeTrades(pair);
            
            return Ok($"Unsubscribed from trades for {pair}");
        }

        [HttpPost("subscribe/candles/{pair}/{periodInSec}")]
        public IActionResult SubscribeCandles(string pair, int periodInSec)
        {
            _socketConnector.SubscribeCandles(pair, periodInSec);
            
            return Ok($"Subscribed to candles for {pair} with period {periodInSec} sec");
        }

        [HttpPost("unsubscribe/candles/{pair}")]
        public IActionResult UnsubscribeCandles(string pair)
        {
            _socketConnector.UnsubscribeCandles(pair);
           
            return Ok($"Unsubscribed from candles for {pair}");
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            if (_socketConnector is BitfinexSocketClient client)
            {
                await client.DisconnectAsync();
                
                return Ok("WebSocket disconnected");
            }
            
            return BadRequest("Invalid connector instance");
        }
    }
}