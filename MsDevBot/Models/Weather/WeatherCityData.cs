using System;

namespace MsDevBot.Models.Weather
{
    [Serializable]
    public struct WeatherCityData
    {
        public double? Temperature { get; set; }
        public string TemperatureUnit { get; set; }
        public string Conditions { get; set; }
        public string IconCode { get; set; }
        public string IconType { get; set; }
        public string IconFileName => $"{IconCode}.{IconType}";
    }
}