using System;

public class Struct
    {
        public struct stationData
        {
            public string fileType;
            public int UUID;
            public short measurementCount;
            public DateTime[] Time;
            public float[] Temperature;
            public float[] Humidity;
            public float[] dustDensity;
            public ushort checkSum;

        }

}


