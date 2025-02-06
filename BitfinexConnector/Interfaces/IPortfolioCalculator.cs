using BitfinexConnector.Models;

namespace BitfinexConnector.Interfaces
{
    public interface IPortfolioCalculator
    {
        Task<PortfolioBalance> CalculatePortfolioAsync();
    }
}
