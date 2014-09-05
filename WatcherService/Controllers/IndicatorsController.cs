using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using WatcherService.Helpers;
using WatcherService.Models;

namespace WatcherService.Controllers
{
    [AllowCrossSiteJson]
    public class IndicatorsController : ApiController
    {
        private List<Indicator> indicators;

        public IndicatorsController()
        {
            GenerateIndicatorValues();
        }

        /// <summary>
        ///  Get data by interval, can be grouped by: hours, days, months
        /// </summary>
        /// <param name="startDate">Start interval</param>
        /// <param name="endDate">End interval</param>
        /// <param name="step">  Hours = 0, Day = 1, Months = 2, Week = 3, Year = 4</param>
        /// <returns></returns>
        public IEnumerable<IndicatorDto> Get(string startDate, string endDate, string step)
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || string.IsNullOrEmpty(step))
                throw new ArgumentNullException("params");

            var sd = DateTime.Parse(startDate).Date;
            var ed = DateTime.Parse(endDate).Date;

            if (DateTime.Compare(ed, sd) < 0) return null;

            var range = indicators.Where(x => x.DateTime.Date >= sd && x.DateTime.Date <= ed).ToList();

            var currentStep = (Step)Enum.Parse(typeof(Step), step);

            IEnumerable<IndicatorDto> result = null;

            switch (currentStep)
            {
                case Step.Hours: result = GetIndicatorsByHours(range); break;
                case Step.Day: result = GetIndicatorsByDay(range); break;
                case Step.Months: result = GetIndicatorsByMonths(range); break;
            }

            return result;
        }

        /// <summary>
        /// Get all temeprature values
        /// </summary>
        /// <returns></returns>
        [ActionName("GetAllTemperatureData")]
        public IEnumerable<SimpleIndicator> GetAllTemperatureData()
        {
            var results = indicators.Select(g => new SimpleIndicator
            {
                DateTime = g.DateTime,
                Value = g.Temperature
            });
            return results;
        }

        /// <summary>
        // Get all humidity values
        /// </summary>
        /// <returns></returns>
        [ActionName("GetAllHumidityData")]
        public IEnumerable<SimpleIndicator> GetAllHumidityData()
        {
            var results = indicators.Select(g => new SimpleIndicator
            {
                DateTime = g.DateTime,
                Value = g.Humidity,
            });
            return results;
        }

        /// <summary>
        /// Get indicators values (temperature & humidity) for the last : 12 hours, day, week, month, year
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ActionName("GetData")]
        public IEnumerable<IndicatorDto> GetData(string param)
        {
            var currentStep = (Step)Enum.Parse(typeof(Step), param);

            IEnumerable<IndicatorDto> result = null;

            switch (currentStep)
            {
                case Step.Hours: result = GetIndicatorsByLast12Hours(); break;
                case Step.Day: result = GetIndicatorsByLastDay(); break;
                case Step.Week: result = GetIndicatorsByLastWeek(); break;
                case Step.Months: result = GetIndicatorsByLastMonth(); break;
                case Step.Year: result = GetIndicatorsByLastYear(); break;
            }

            return result;
        }


        /// <summary>
        /// Get indicators' values  for the last year.
        /// Values will be grouped by days.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndicatorDto> GetIndicatorsByLastYear()
        {
            var today = DateTime.Now;
            var month = new DateTime(today.Year, 1, 1);

            var range = indicators.Where(x => x.DateTime.Date >= month).ToList();

            var result = GetIndicatorsByDay(range);

            return result;
        }

        /// <summary>
        /// Get indicators' values  for the last month.
        /// Values will be grouped by days.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndicatorDto> GetIndicatorsByLastMonth()
        {
            var today = DateTime.Now;
            var month = new DateTime(today.Year, today.Month, 1);

            var range = indicators.Where(x => x.DateTime.Date >= month).ToList();

            var result = GetIndicatorsByDay(range);

            return result;
        }

        /// <summary>
        /// Get indicators' values  for the last week.
        /// Values will be grouped by days.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndicatorDto> GetIndicatorsByLastWeek()
        {
            var today = DateTime.Now;

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek).Date;

            var range = indicators.Where(x => x.DateTime.Date >= startOfWeek).ToList();

            var result = GetIndicatorsByDay(range);

            return result;

        }

        /// <summary>
        /// Get indicators' values  for the last day.
        /// Values will be grouped by hours.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndicatorDto> GetIndicatorsByLastDay()
        {
            var today = DateTime.Now;

            var range = indicators.Where(x => x.DateTime >= today.AddHours(-today.Hour)).ToList();

            var result = GetIndicatorsByHours(range);

            return result;
        }

        /// <summary>
        /// Get indicators' values  for the last 12 hours.
        /// Values will be grouped by hours.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndicatorDto> GetIndicatorsByLast12Hours()
        {
            var today = DateTime.Now;

            var range = indicators.Where(x => x.DateTime >= today.AddHours(-12)).ToList();

            var result = GetIndicatorsByHours(range);

            return result;
        }

        private IEnumerable<IndicatorDto> GetIndicatorsByMonths(IEnumerable<Indicator> range)
        {
            var results = range.GroupBy(i => i.DateTime.Month)
                        .Select(g => new IndicatorDto
                        {
                            DateTime = g.Key.ToString(),
                            Humidity = g.Average(a => a.Humidity),
                            Temperature = g.Average(a => a.Temperature)
                        });
            return results;
        }

        private IEnumerable<IndicatorDto> GetIndicatorsByDay(IEnumerable<Indicator> range)
        {
            var results = range.GroupBy(i => i.DateTime.Date)
                         .Select(g => new IndicatorDto
                         {
                             DateTime = g.Key.ToString("dd/MM/yyyy", new CultureInfo("en-GB")),
                             Humidity = g.Average(a => a.Humidity),
                             Temperature = g.Average(a => a.Temperature)
                         });
            return results;
        }

        private IEnumerable<IndicatorDto> GetIndicatorsByHours(IEnumerable<Indicator> range)
        {
            var results = range.GroupBy(x => new { x.DateTime.Date, x.DateTime.Hour })
                         .Select(g => new IndicatorDto
                         {
                             DateTime = string.Format("{0}, h:{1}", g.Key.Date.ToString("dd/MM/yyyy", new CultureInfo("en-GB")), g.Key.Hour),
                             Humidity = g.Average(a => a.Humidity),
                             Temperature = g.Average(a => a.Temperature)
                         });
            return results;
        }

        private void GenerateIndicatorValues()
        {
            var rnd = new Random();
            indicators = new List<Indicator>();
            var date = DateTime.Now;
            date = date.AddDays(-1);

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 24; j++)
                {
                    for (var k = 0; k < 59; k = k + 5)
                    {
                        indicators.Add(new Indicator
                        {
                            DateTime = date.AddDays(i).AddHours(j).AddMinutes(k),
                            Humidity = rnd.Next(100),
                            Temperature = rnd.Next(100)
                        });
                    }
                }
            }
        }
    }

    public enum Step
    {
        Hours = 0, Day = 1, Months = 2, Week = 3, Year = 4
    }
}
