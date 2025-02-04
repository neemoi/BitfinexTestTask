using BitfinexConnector.Models;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using BitfinexConnector.Interfaces;

namespace BitfinexConnector.Clients
{
    public class BitfinexSocketClient : ITestSocketConnector
    {
        private const string bitfinexSocketUrl = "wss://api-pub.bitfinex.com/ws/2";
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
      
        public event Action<Trade> NewBuyTrade = delegate { };
        public event Action<Trade> NewSellTrade = delegate { };
        public event Action<Candle> CandleSeriesProcessing = delegate { };

        public BitfinexSocketClient()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(ProcessMessagesAsync);
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                Console.WriteLine("Error: The pair cannot be empty");
                return;
            }

            string formattedPair = $"t{pair.Trim().ToUpper()}";

            var request = new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = formattedPair
            };

            try
            {
                SendWebSocketMessageAsync(request).Wait();
                Console.WriteLine($"Subscription request sent: {formattedPair}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when subscribing to: {formattedPair}: {ex.Message}");
            }
        }

        public void UnsubscribeTrades(string pair)
        {
            var request = new
            {
                @event = "unsubscribe",
                channel = "trades",
                symbol = $"t{pair.Trim().ToUpper()}"
            };

            SendWebSocketMessageAsync(request).Wait();
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            var timeframe = GetTimeframe(periodInSec);
            var request = new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = $"t{pair.Trim().ToUpper()}"
            };

            SendWebSocketMessageAsync(request).Wait();
        }

        public void UnsubscribeCandles(string pair)
        {
            var request = new
            {
                @event = "unsubscribe",
                channel = "trades",
                symbol = $"t{pair.ToUpper()}"
            };

            SendWebSocketMessageAsync(request).Wait();
        }

        
        public async Task DisconnectAsync()
        {
            _cancellationTokenSource.Cancel();
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }

        private void HandleTradeUpdate(string updateType, JsonElement tradeData)
        {
            try
            {
                var trade = new Trade
                {
                    Id = tradeData[0].GetRawText(),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeData[1].GetInt64()),
                    Amount = tradeData[2].GetDecimal(),
                    Price = tradeData[3].GetDecimal(),
                    Side = tradeData[2].GetDecimal() > 0 ? "buy" : "sell"
                };

                if (updateType == "te")
                {
                    Console.WriteLine($"The deal is done: {trade.Amount} {trade.Side} by {trade.Price}");
                }
                else if (updateType == "tu")
                {
                    Console.WriteLine($"Updating the deal: {trade.Amount} {trade.Side} by {trade.Price}");
                }

                if (trade.Amount > 0)
                    NewBuyTrade?.Invoke(trade);
                else
                    NewSellTrade?.Invoke(trade);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when processing the transaction update: {ex.Message}");
            }
        }

        private async Task ProcessMessagesAsync()
        {
            var buffer = new byte[2048];

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    Console.WriteLine("WebSocket is not open waiting..");
                    await Task.Delay(1000);
                    continue;
                }

                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("WebSocket closed by server");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received message: {message}");

                HandleMessage(message);
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                var json = JsonDocument.Parse(message).RootElement;

                if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("event", out var eventType))
                {
                    if (eventType.GetString() == "subscribed")
                    {
                        Console.WriteLine("The subscription was successful");
                        return;
                    }
                    else if (eventType.GetString() == "unsubscribed")
                    {
                        Console.WriteLine("The unsubscription was successful");
                        return;
                    }
                }
                else if (json.ValueKind == JsonValueKind.Array)
                {
                    if (json.GetArrayLength() < 2) return;

                    var channelId = json[0].GetInt32();
                    var secondElement = json[1];

                    if (secondElement.ValueKind == JsonValueKind.String && secondElement.GetString() == "hb")
                    {
                        Console.WriteLine("Received a heartbeat skip it");
                        return;
                    }

                    if (secondElement.ValueKind == JsonValueKind.String)
                    {
                        string updateType = secondElement.GetString();
                        var tradeData = json[2];

                        if (tradeData.ValueKind == JsonValueKind.Array)
                        {
                            HandleTradeUpdate(updateType, tradeData);
                        }
                    }
                    else if (secondElement.ValueKind == JsonValueKind.Array)
                    {
                        HandleTradeMessage(secondElement);
                    }
                    else
                    {
                        Console.WriteLine("Mistake: Unsupported data format");
                    }
                }
                else
                {
                    Console.WriteLine("Error: Incorrect message format");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when processing WebSocket message: {ex.Message}");
            }
        }

        private void HandleTradeMessage(JsonElement data)
        {
            foreach (var trade in data.EnumerateArray())
            {
                var tradeObj = new Trade
                {
                    Id = trade[0].GetRawText(),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
                    Amount = trade[2].GetDecimal(),
                    Price = trade[3].GetDecimal(),
                    Side = trade[2].GetDecimal() > 0 ? "buy" : "sell"
                };

                if (trade[2].GetDecimal() > 0)
                    NewBuyTrade?.Invoke(tradeObj);
                else
                    NewSellTrade?.Invoke(tradeObj);
            }
        }

        private async Task SendWebSocketMessageAsync(object message)
        {
            if (_webSocket.State != WebSocketState.Open)
                await _webSocket.ConnectAsync(new Uri(bitfinexSocketUrl), _cancellationTokenSource.Token);

            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
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