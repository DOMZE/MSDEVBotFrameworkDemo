using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MsDevBot.Models.Weather
{
    [Serializable]
    public class WeatherDataProcessor
    {
        private volatile IEnumerable<WeatherCity> _cities;
        private readonly object _lock = new object();
        public IEnumerable<WeatherCity> Cities
        {
            get
            {
                if (_cities != null)
                    return _cities;

                lock (_lock)
                {
                    if (_cities != null)
                        return _cities;
                    _cities = StoreCitiesData();
                }

                return _cities;
            }
        }

        private IEnumerable<WeatherCity> StoreCitiesData()
        {
            var fileLocation = Path.Combine(Path.GetTempPath(), "weathergc.siteList.xml");
            var client = new WebClient();
            client.DownloadFile(new Uri("http://dd.weather.gc.ca/citypage_weather/xml/siteList.xml"), fileLocation);
            //var fileLocation = @"C:\Users\dstamand\Downloads\siteList.xml";
            var data = ProcessFile(fileLocation);
            return data;
        }

        public async Task<WeatherCityData> GetWeather(WeatherCity city)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(new Uri($"http://dd.weather.gc.ca/citypage_weather/xml/{city.ProvinceAbbr}/{city.Code}_e.xml"));

            // Get HTTP response from completed task.
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var fileLocation = Path.Combine(Path.GetTempPath(), $"{city.Code}_e.xml");
            using (var fileStream = File.Create(fileLocation))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream);
            }
            var data = await ProcessCityFileAsync(fileLocation);
            return data;
        }

        private async Task<WeatherCityData> ProcessCityFileAsync(string fileName)
        {
            var task = Task.Factory.StartNew(() =>
            {
                var xml = XDocument.Load(fileName);
                var cityData = new WeatherCityData();
                var currentConditionElement = xml.Root.Element("currentConditions");
                cityData.Conditions = currentConditionElement.Element("condition").Value;
                var iconCodeElement = currentConditionElement.Element("iconCode");
                cityData.IconCode = iconCodeElement.Value;
                cityData.IconType = iconCodeElement.Attribute("format").Value;
                var temperatureElement = currentConditionElement.Element("temperature");
                cityData.Temperature = String.IsNullOrEmpty(temperatureElement.Value) ? (double?) null : Convert.ToDouble(temperatureElement.Value);
                cityData.TemperatureUnit = temperatureElement.Attribute("units").Value;
                return cityData;
            });
            await task;
            return task.Result;
        }

        private IEnumerable<WeatherCity> ProcessFile(string fileName)
        {
            var xml = XDocument.Load(fileName);

            var data = (from c in xml.Root.Elements()
                        select new WeatherCity
                        {
                            Code = c.Attribute("code").Value,
                            NameEN = c.Element("nameEn").Value,
                            NameFR = c.Element("nameFr").Value,
                            ProvinceAbbr = c.Element("provinceCode").Value
                        }).ToList();
            return data;
        }
    }
}