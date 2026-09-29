using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using API.Models;

namespace DataSorter
{
    class Struct
    {
        struct stationData
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

        static void Main(string[] args)
        {
            stationData data = new stationData();
            data.fileType = "Fin100";
            data.measurementCount = 5;
            data.UUID = 1827433;
            data.Time = new DateTime[5];
            {
                data.Time[0] = new DateTime(2025, 02, 1, 10, 30, 00);
                data.Time[1] = new DateTime(2025, 02, 1, 11, 30, 00);
                data.Time[2] = new DateTime(2025, 02, 1, 12, 30, 00);
                data.Time[3] = new DateTime(2025, 02, 1, 13, 30, 00);
                data.Time[4] = new DateTime(2025, 02, 1, 14, 30, 00);
            }
            data.Temperature = new float[5];
            {
                data.Temperature[0] = 22.543f;
                data.Temperature[1] = 23.422f;
                data.Temperature[2] = 23.521f;
                data.Temperature[3] = 24.12f;
                data.Temperature[4] = 24.51f;
            }

            data.Humidity = new float[5];
            {
                data.Humidity[0] = 50f;
                data.Humidity[1] = 51f;
                data.Humidity[2] = 51f;
                data.Humidity[3] = 53f;
                data.Humidity[4] = 50f;
            }

            data.dustDensity = new float[5];
            {
                data.Humidity[0] = 50f;
                data.Humidity[1] = 51f;
                data.Humidity[2] = 51f;
                data.Humidity[3] = 53f;
                data.Humidity[4] = 50f;
            }


            string[] formattedTemperature = new string[data.measurementCount];
            for (int i = 0; i < data.measurementCount; i++)
            {
                formattedTemperature[i] = data.Temperature[i].ToString("F1");
            }


            string[] formattedHumidity = new string[data.measurementCount];
            for (int i = 0; i < data.measurementCount; i++)
            {
                formattedHumidity[i] = data.Humidity[i].ToString("F0");
            }


            //string[] formattedWindSpeed = new string[data.measurementCount];
            //for (int i = 0; i < data.measurementCount; i++)
            //{
            //    formattedWindSpeed[i] = data.windSpeed[i].ToString("F2");
            //}

            //byte[] byteArray = StructToByteArray(data);
            //data.checkSum = CalCrc(byteArray, (short)byteArray.Length, 0);



            Watcher();

            //Console.WriteLine("Calculated CRC: " + data.checkSum.ToString("X4"));

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            //WriteData(data);
            //ReadData(data);
        }

        static void WriteData(stationData data)
        {
            FileStream fs = new FileStream(@"C:\BinaryFiles\time.bin", FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(data.checkSum);
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
            fs.Close();

        }

        static void ReadData(stationData data)
        {
            FileStream fs = new FileStream(@"C:\BinaryFiles\2831172-20251117_162311.bin", FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            ushort checkSum = br.ReadUInt16();
            string fileType = br.ReadString();
            short measurementCount = br.ReadInt16();
            int uuid = br.ReadInt32();

            for (int i = 0; i < measurementCount; i++)
            {
                DateTime time = DateTime.FromBinary(br.ReadInt64());
                float temp = br.ReadSingle();
                float hum = br.ReadSingle();
                float wind = br.ReadSingle();

                Console.WriteLine($"Reading {i}: {time} {temp} {hum} {wind}");
            }

        }

        static void DataBaseConnection()
        {
            string connectionString = "Server = DESKTOP-IK9RFT0\\SQLEXPRESS; Database = WeatherData; User Id=Julio;Password=1234;";

            string query = "SELECT * FROM Stations";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader dr = command.ExecuteReader();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    Console.Write(dr.GetName(i) + "\t");
                }
                Console.WriteLine();


                while (dr.Read())
                {
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        Console.Write(dr.GetValue(i).ToString() + "\t");
                    }
                    Console.WriteLine();
                }

                dr.Close();
                Console.ReadKey();
            }
        }

        static void DataBaseWrite(string filePath)
        {


            if (!File.Exists(filePath))
            {
                LogError("The file does not exist.");
                return;
            }

            string fileName = Path.GetFileName(filePath);


            FileStream fs = new FileStream(@"C:\BinaryFiles\" + fileName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            stationData file = new stationData();

            try
            {

                UInt16 initialCheckSum = br.ReadUInt16();
                file.fileType = br.ReadString();
                file.measurementCount = br.ReadInt16();
                file.UUID = br.ReadInt32();

                file.Time = new DateTime[file.measurementCount];
                file.Temperature = new float[file.measurementCount];
                file.Humidity = new float[file.measurementCount];
                file.dustDensity = new float[file.measurementCount];
                for (int i = 0; i < file.measurementCount; i++)
                {
                    file.Time[i] = DateTime.FromBinary(br.ReadInt64());
                    file.Temperature[i] = br.ReadSingle();
                    file.Humidity[i] = br.ReadSingle();
                    file.dustDensity[i] = br.ReadSingle();
                }

                byte[] byteArray = StructToByteArray(file);
                file.checkSum = CalCrc(byteArray, (short)byteArray.Length, 0);

                Console.WriteLine(initialCheckSum.ToString());
                Console.WriteLine(file.checkSum.ToString());

                if (file.checkSum != initialCheckSum)
                {
                    LogError("Calculated Checksum does not match initial Checksum, data is not valid.");
                    return;
                }


                string[] formattedTemperature = new string[file.measurementCount];
                for (int i = 0; i < file.measurementCount; i++)
                {
                    formattedTemperature[i] = file.Temperature[i].ToString("F1");
                }


                string[] formattedHumidity = new string[file.measurementCount];
                for (int i = 0; i < file.measurementCount; i++)
                {
                    formattedHumidity[i] = file.Humidity[i].ToString("F0");
                }


                string[] formattedDustDensity = new string[file.measurementCount];
                for (int i = 0; i < file.measurementCount; i++)
                {
                    formattedDustDensity[i] = file.dustDensity[i].ToString("F2");
                }


                string connectionString = "Server = DESKTOP-IK9RFT0\\SQLEXPRESS; Database = WeatherData; Trusted_Connection=True;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT COUNT(*) FROM Station_Measurements WHERE Measurement_Time = @timeValue";
                    using (var command = new SqlCommand(query, conn))
                    {

                        for (int i = 0; i < file.measurementCount; i++)
                        {

                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@timeValue", file.Time[i]);

                            int count = Convert.ToInt32(command.ExecuteScalar());

                            if (count > 0)
                            {
                                Console.WriteLine("Entry " + i + " already exists.");

                                string updateQuery = "UPDATE Station_Measurements SET Temperature = @Temperature, Humidity = @Humidity, Dust_Density = @Dust_Density WHERE Measurement_Time = @Measurement_Time";
                                using (SqlCommand updateCommand = new SqlCommand(updateQuery, conn))
                                {
                                    updateCommand.Parameters.AddWithValue("@Temperature", formattedTemperature[i]);
                                    updateCommand.Parameters.AddWithValue("@Humidity", formattedHumidity[i]);
                                    updateCommand.Parameters.AddWithValue("@Dust_Density", formattedDustDensity[i]);
                                    updateCommand.Parameters.AddWithValue("@Measurement_Time", file.Time[i]);

                                    updateCommand.ExecuteNonQuery();
                                    Console.WriteLine("Entry " + i + " updated.");

                                }

                            }
                            else
                            {
                                Console.WriteLine("Entry " + i + " does not exist.");
                                {
                                    string insertQuery = "INSERT INTO Station_Measurements (Station_ID, Measurement_Time, Temperature, Humidity, Dust_Density) " +
                                                         "VALUES (@Station_ID, @Measurement_Time, @Temperature, @Humidity, @Dust_Density)";
                                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, conn))
                                    {
                                        insertCommand.Parameters.AddWithValue("@Station_ID", file.UUID);
                                        insertCommand.Parameters.AddWithValue("@Measurement_Time", file.Time[i]);
                                        insertCommand.Parameters.AddWithValue("@Temperature", formattedTemperature[i]);
                                        insertCommand.Parameters.AddWithValue("@Humidity", formattedHumidity[i]);
                                        insertCommand.Parameters.AddWithValue("@Dust_Density", formattedDustDensity[i]);

                                        insertCommand.ExecuteNonQuery();
                                        Console.WriteLine("Entry " + i + " inserted.");

                                    }

                                }
                            }
                        }

                    }

                    conn.Close();
                    br.Close();
                    fs.Close();


                    string processedFilePath = "C:\\BinaryFiles\\ProcessedFiles\\";


                    string destinationPath = Path.Combine(processedFilePath, fileName);


                    File.Move(filePath, destinationPath);
                    Console.WriteLine($"File moved to: {destinationPath}");

                }
            }
            catch (IOException ex)
            {
                LogError(ex.Message);
            }
            catch (SqlException ex)
            {
                LogError(ex.Message);
            }
            
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
            }

        }


        public static ushort CalCrc(byte[] ptr, short count, ushort startCrc)
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



        private static byte[] StructToByteArray(stationData data)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(data.fileType);  
                bw.Write(data.measurementCount);  
                bw.Write(data.UUID); 
                foreach (var time in data.Time)
                {
                    bw.Write(time.ToBinary());
                }
                foreach (var temp in data.Temperature)
                {
                    bw.Write(temp);
                }
                foreach (var humidity in data.Humidity)
                {
                    bw.Write(humidity);
                }
                foreach (var dust in data.dustDensity)
                {
                    bw.Write(dust);
                }
                return ms.ToArray();
            }
        }



        static void LogError(string message)
        {
            string logFilePath = @"C:\BinaryFiles\\error.log";
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message);
            }
        }


        public static void Watcher()
        {
        using var watcher = new FileSystemWatcher(@"C:\BinaryFiles");

         
            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Error += OnError;

            watcher.Filter = "*.bin";
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;
            

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                LogError($"Message: {ex.Message}\nStacktrace:\n{ex.StackTrace}");
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Created: {e.FullPath}");
            ProcessFile(e.FullPath);
        }


        private static ConcurrentDictionary<string, DateTime> fileChangeTimes = new ConcurrentDictionary<string, DateTime>();

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            var now = DateTime.Now;
            fileChangeTimes[e.FullPath] = now;

            Task.Delay(1000).ContinueWith(_ =>
            {
                if (fileChangeTimes.TryGetValue(e.FullPath, out var lastChange) && (now - lastChange).TotalMilliseconds >= 1000)
                {
                    fileChangeTimes.TryRemove(e.FullPath, out DateTime _);
                    if (ArchiveBit(e.FullPath))
                    {
                        Console.WriteLine($"Changed: {e.FullPath}");
                        ProcessFile(e.FullPath);
                    }
                    else
                    {
                        Console.WriteLine("Archive bit is not set, skipping file.");
                    }
                }
            });
        }




        private static void ProcessFile(string filePath)
        {
            if (ArchiveBit(filePath))
            {
                Console.WriteLine($"Processing file: {filePath}");
                DataBaseWrite(filePath);
            }
            else
            {
                Console.WriteLine($"File is not ready: {filePath}");
                Task.Delay(1000).ContinueWith(_ => ProcessFile(filePath));
            }
        }

        private static bool ArchiveBit(string filePath)
        {
            FileAttributes attributes = File.GetAttributes(filePath);
            return (attributes & FileAttributes.Archive) == FileAttributes.Archive;
        }

    }
}
      


