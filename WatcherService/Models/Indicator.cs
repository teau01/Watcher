using System;

namespace WatcherService.Models
{
    public class Indicator
    {
        public DateTime DateTime { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
    }

    public class IndicatorDto
    {
        public string DateTime { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
    }

    public class SimpleIndicator
    {
        public DateTime DateTime { get; set; }
        public float Value { get; set; }
    }
}