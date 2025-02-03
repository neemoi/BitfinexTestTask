using System.Text.Json;
using BitfinexConnector.Interfaces;
using BitfinexConnector.Models;

namespace BitfinexConnector.Clients
{
    public class BitfinexClient : ITestConnector
    {
        private readonly HttpClient _httpClient;
        private const string bitfinexUrl = "https://api-pub.bitfinex.com/v2/";

        public BitfinexClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            string url = $"{bitfinexUrl}trades/t{pair}/hist?limit={maxCount}";
            var response = await _httpClient.GetStringAsync(url);

            return ParseTrades(response, pair);
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            string timeframe = GetTimeframe(periodInSec);
            string url = $"{bitfinexUrl}candles/trade:{timeframe}:t{pair}/hist?limit={count}";
            var response = await _httpClient.GetStringAsync(url);

            return ParseCandles(response, pair);
        }

        private IEnumerable<Trade> ParseTrades(string json, string pair)
        {
            var trades = JsonSerializer.Deserialize<List<JsonElement>>(json);
            var tradeList = new List<Trade>();

            foreach (var tradeElement in trades)
            {
                var tradeArray = tradeElement.EnumerateArray().ToArray();

                tradeList.Add(new Trade
                {
                    Pair = pair,
                    Id = tradeArray[0].GetRawText(),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeArray[1].GetInt64()),
                    Amount = tradeArray[2].GetDecimal(),
                    Price = tradeArray[3].GetDecimal(),
                    Side = tradeArray[2].GetDecimal() > 0 ? "buy" : "sell"
                });
            }

            return tradeList;
        }

        private IEnumerable<Candle> ParseCandles(string json, string pair)
        {
            var candles = JsonSerializer.Deserialize<List<JsonElement>>(json);
            var candleList = new List<Candle>();

            foreach (var candleElement in candles)
            {
                var candleArray = candleElement.EnumerateArray().ToArray();

                candleList.Add(new Candle
                {
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleArray[0].GetInt64()),
                    OpenPrice = candleArray[1].GetDecimal(),
                    HighPrice = candleArray[2].GetDecimal(),
                    LowPrice = candleArray[3].GetDecimal(),
                    ClosePrice = candleArray[4].GetDecimal(),
                    TotalVolume = candleArray[5].GetDecimal()
                });
            }

            return candleList;
        }

        private string GetTimeframe(int periodInSec)
        {
            return periodInSec switch
            {
                60 => "1m",
                300 => "5m",
                900 => "15m",
                3600 => "1h",
                86400 => "1D",
                _ => throw new ArgumentException("Unsupported period")
            };
        }
    }
}