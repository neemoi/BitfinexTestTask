using BitfinexConnector.Models;

namespace BitfinexConnector.Interfaces
{
    public interface ITestConnector
    {
        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
        
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);
    }
}