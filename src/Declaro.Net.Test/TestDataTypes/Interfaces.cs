namespace Declaro.Net.Test.TestDataTypes
{
    public interface IWeatherRequest
    {
        string City { get; set; }

        string Date { get; set; }
    }

    public interface IWeatherResponse
    {
        int Celsius { get; set; }

        string City { get; set; }
    }
}
