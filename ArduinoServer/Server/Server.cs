using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Struct;


class Server
{
    static async Task Main(string[] args)
    {

        IPEndPoint ipEndPoint = new(IPAddress.Any, 12_345);
        using Socket listener = new(
            ipEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(100);
        Console.WriteLine($"Server listening on: {listener.LocalEndPoint}");

        var handler = await listener.AcceptAsync();

        while (true)
        {
            var buffer = new byte[255];
            var received = await handler.ReceiveAsync(buffer, SocketFlags.None);

           
            if (received == 0)
            {
                Console.WriteLine("Client disconnected.");
                break;
            }

            var response = Encoding.UTF8.GetString(buffer, 0, received);
            var crcTag = "<|CRC|>";
            var eocTag = "<|EOC|>";

            if (response.Contains(crcTag) && response.Contains(eocTag))
            {
                int crcStart = response.IndexOf(crcTag) + crcTag.Length;
                int eocIndex = response.IndexOf(eocTag);

                string crcStr = response.Substring(crcStart, eocIndex - crcStart).Trim();
                string payload = response.Substring(eocIndex + eocTag.Length);


                payload = payload.TrimEnd();
                byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);
                ushort computedCrc = CalCrc(payloadBytes, (short)payloadBytes.Length, 0xFFFF);

                if (ushort.TryParse(crcStr, out ushort receivedCrc) && receivedCrc == computedCrc)
                {
                    Console.WriteLine($"Valid message: \"{payload}\"");
                    var ackMessage = "<|ACK|>\n";
                    var echoBytes = Encoding.ASCII.GetBytes(ackMessage);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    Console.WriteLine("Acknowledgment sent.");
                    string[] tokens = payload.Split('|');
                    
                    
                    stationData data = new stationData();
                    data.fileType = "Fin100";
                    data.UUID = int.Parse(tokens[0]);
                    data.measurementCount = short.Parse(tokens[1]);
                    data.checkSum = computedCrc;

                    
                    data.Time = new DateTime[data.measurementCount];
                    data.Temperature = new float[data.measurementCount];
                    data.Humidity = new float[data.measurementCount];
                    data.dustDensity = new float[data.measurementCount];

                    for (int i = 0; i < data.measurementCount; i++)
                    {
                        data.Time[i] = DateTime.Now;
                    }

                    
                    for (int i = 0; i < data.measurementCount; i++)
                    {
                        int tokenIndex = 2 + i * 3;
                        data.Temperature[i] = float.Parse(tokens[tokenIndex]);
                        data.Humidity[i] = float.Parse(tokens[tokenIndex + 1]);
                        data.dustDensity[i] = float.Parse(tokens[tokenIndex + 2]);
                    }

                    WriteData(data);

                }
                else
                {
                    Console.WriteLine("CRC mismatch. Message discarded.");
                    Console.WriteLine($"CRC: {crcStr}, CalCRC: {computedCrc} Payload: \"{payload}\"");
                }
            }

            




        }





    }
    static void WriteData(stationData data)
    {
        string timestamp = data.Time[0].ToString("yyyyMMdd_HHmmss");
        string filename = $@"C:\BinaryFiles\{data.UUID}-{timestamp}.bin";

        // Step 1: Build payload WITHOUT checksum
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(data.fileType);
        bw.Write(data.measurementCount);
        bw.Write(data.UUID);

        for (int i = 0; i < data.measurementCount; i++)
        {
            bw.Write(data.Time[i].ToBinary());
            bw.Write(data.Temperature[i]);
            bw.Write(data.Humidity[i]);
            bw.Write(data.dustDensity[i]);
        }

        byte[] payload = ms.ToArray();

        // Step 2: Calculate CRC over payload only
        data.checkSum = CalCrc(payload, (short)payload.Length, 0);

        // Step 3: Write checksum + payload
        using var fs = new FileStream(filename, FileMode.Create);
        using var finalBw = new BinaryWriter(fs);
        finalBw.Write(data.checkSum);    // ← checksum first
        finalBw.Write(payload);          // ← then all data

        // Optional: Set archive bit so service knows file is complete
        File.SetAttributes(filename, File.GetAttributes(filename) | FileAttributes.Archive);

        Console.WriteLine($"File written: {Path.GetFileName(filename)} | CRC: 0x{data.checkSum:X4}");
    }
    static ushort CalCrc(byte[] ptr, short count, ushort startCrc)
    {
        ushort crc = startCrc;

        while (--count >= 0)
        {
            crc ^= (ushort)(ptr[count] << 8);

            for (int i = 0; i < 8; ++i)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }
        return (ushort)(crc & 0xFFFF);
    }
}

