using System.Text.Json;
using System.Threading.Tasks;

namespace Dotnet.GenAI.RawImplementation.Services
{
    public class WeatherService
    {
        public async Task<string> GetWeather(string location)
        {
            // Simulate API call
            await Task.Delay(500);

            // Mock data based on location
            var weatherData = location.ToLower() switch
            {
                var l when l.Contains("london") => new
                {
                    temperature = 15,
                    condition = "Cloudy",
                    humidity = 75
                },
                var l when l.Contains("athens") => new
                {
                    temperature = 25,
                    condition = "Sunny",
                    humidity = 80
                },
                _ => new
                {
                    temperature = 20,
                    condition = "Partly Cloudy",
                    humidity = 60
                }
            };

            return JsonSerializer.Serialize(weatherData);
        }
    }
}
