using System.Globalization;
using System.Text.Json;
using BitfinexConnector.Interfaces;
using BitfinexConnector.Models;

namespace BitfinexConnector.Clients
{
    public class PortfolioCalculator : IPortfolioCalculator
    {
        private const decimal AmountBTC = 1m;
        private const decimal AmountXRP = 15000m;
        private const decimal AmountXMR = 50m;
        private const decimal AmountDASH = 30m;

        private readonly HttpClient _httpClient;
        private const string BinanceApiUrl = "https://api.binance.com/api/v3/ticker/price";

        public PortfolioCalculator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<decimal> GetPriceAsync(string symbol)
        {
            var url = $"{BinanceApiUrl}?symbol={symbol}";
            var response = await _httpClient.GetAsync(url);
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("price", out var priceElement))
            {
                return decimal.Parse(priceElement.GetString(), CultureInfo.InvariantCulture);
            }
            throw new Exception($"Не удалось получить цену для {symbol}");
        }


        public async Task<PortfolioBalance> CalculatePortfolioAsync()
        {
            decimal btcRate = await GetPriceAsync("BTCUSDT");
            decimal xrpRate = await GetPriceAsync("XRPUSDT");
            decimal xmrRate = await GetPriceAsync("XMRUSDT");
            decimal dashRate = await GetPriceAsync("DASHUSDT");

            decimal btcInUSDT = AmountBTC * btcRate;
            decimal xrpInUSDT = AmountXRP * xrpRate;
            decimal xmrInUSDT = AmountXMR * xmrRate;
            decimal dashInUSDT = AmountDASH * dashRate;

            decimal totalUSDT = btcInUSDT + xrpInUSDT + xmrInUSDT + dashInUSDT;

            var balance = new PortfolioBalance
            {
                USDT = totalUSDT,
                BTC = totalUSDT / btcRate,
                XRP = totalUSDT / xrpRate,
                XMR = totalUSDT / xmrRate,
                DASH = totalUSDT / dashRate
            };

            return balance;
        }
    }
}