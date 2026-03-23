using System.ComponentModel;

public interface IWeatherService
{
    [Description("Get the weather for a given location.")]
    string GetWeather([Description("The location to get the weather for.")] string location);
     
}