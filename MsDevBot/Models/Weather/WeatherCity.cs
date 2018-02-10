using System;

namespace MsDevBot.Models.Weather
{
    [Serializable]
    public struct WeatherCity
    {
        public string Code { get; set; }
        public string NameEN { get; set; }
        public string NameFR { get; set; }
        public string ProvinceAbbr { get; set; }
    }
}