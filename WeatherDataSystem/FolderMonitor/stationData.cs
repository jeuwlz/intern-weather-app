using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderMonitor
{
    internal class stationData
    {
        public string fileType;
        public int UUID;
        public short measurementCount;
        public DateTime[] Time;
        public float[] Temperature;
        public float[] Humidity;
        public float[] dustDensity;
        public ushort checkSum;
        public string[] formattedTemperature;
        public string[] formattedHumidity;
        public string[] formattedWindSpeed;

    }
}
