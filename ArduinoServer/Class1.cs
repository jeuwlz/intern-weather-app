using System;

namespace Test
{
    public class Struct
    {
        struct stationData
        {
            public string fileType;
            public int UUID;
            public short measurementCount;
            public DateTime[] Time;
            public float[] Temperature;
            public float[] Humidity;
            public float[] windSpeed;
            public ushort checkSum;

        }
    }

}
