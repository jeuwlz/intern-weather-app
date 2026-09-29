using System;

namespace API.Models
{
    public class Data
    {
        public int Id { get; set; }
        public DateTime Measurement_Time { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Wind_Speed { get; set; }
    }
}
