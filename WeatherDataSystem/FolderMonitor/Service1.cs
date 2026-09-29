using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace FolderMonitor
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Task.Run(() => Watcher());
        }

        protected override void OnStop()
        {
        }

        static void DataBaseWrite(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogError("File not found: " + filePath);
                return;
            }

            string fileName = Path.GetFileName(filePath);
            LogEvent($"Starting processing: {fileName}");

            stationData file = new stationData();
            FileStream fs = null;
            BinaryReader br = null;

            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                br = new BinaryReader(fs);

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

                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(file.fileType);
                    bw.Write(file.measurementCount);
                    bw.Write(file.UUID);
                    for (int i = 0; i < file.measurementCount; i++)
                    {
                        bw.Write(file.Time[i].ToBinary());
                        bw.Write(file.Temperature[i]);
                        bw.Write(file.Humidity[i]);
                        bw.Write(file.dustDensity[i]);
                    }
                    byte[] payload = ms.ToArray();
                    ushort calc = CalCrc(payload, (short)payload.Length, 0);
                    if (calc != initialCheckSum)
                    {
                        LogError($"CRC FAILED: File=0x{initialCheckSum:X4} Calc=0x{calc:X4}");
                        return;
                    }
                    LogEvent("CRC valid");
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
                                LogEvent("Entry " + i + " already exists.");

                                string updateQuery = "UPDATE Station_Measurements SET Temperature = @Temperature, Humidity = @Humidity, Dust_Density = @Dust_Density WHERE Measurement_Time = @Measurement_Time";
                                using (SqlCommand updateCommand = new SqlCommand(updateQuery, conn))
                                {
                                    updateCommand.Parameters.AddWithValue("@Temperature", formattedTemperature[i]);
                                    updateCommand.Parameters.AddWithValue("@Humidity", formattedHumidity[i]);
                                    updateCommand.Parameters.AddWithValue("@Dust_Density", formattedDustDensity[i]);
                                    updateCommand.Parameters.AddWithValue("@Measurement_Time", file.Time[i]);

                                    updateCommand.ExecuteNonQuery();
                                    LogEvent("Entry " + i + " updated.");

                                }

                            }
                            else
                            {
                                LogEvent("Entry " + i + " does not exist.");
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
                                        LogEvent("Entry " + i + " inserted.");

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
                    LogEvent($"File moved to: {destinationPath}");

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



        static void LogError(string message)
        {
            string logFilePath = @"C:\BinaryFiles\\error.log";
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message);
            }
        }

        private FileSystemWatcher watcher;

        public void Watcher()
        {

            watcher = new FileSystemWatcher(@"C:\BinaryFiles");


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

            LogEvent("FileSystemWatcher started.");

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
            LogEvent($"Created: {e.FullPath}");
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
                        LogEvent($"Changed: {e.FullPath}");
                        ProcessFile(e.FullPath);
                    }
                    else
                    {
                        LogEvent("Archive bit is not set, skipping file.");
                    }
                }
            });
        }




        private static void ProcessFile(string filePath)
        {
            if (ArchiveBit(filePath))
            {
                LogEvent($"Processing file: {filePath}");
                DataBaseWrite(filePath);
            }
            else
            {
                LogEvent($"File is not ready: {filePath}");
                Task.Delay(1000).ContinueWith(_ => ProcessFile(filePath));
            }
        }

        private static bool ArchiveBit(string filePath)
        {
            FileAttributes attributes = File.GetAttributes(filePath);
            return (attributes & FileAttributes.Archive) == FileAttributes.Archive;
        }


        private static void LogEvent(string message)
        {
            string logFilePath = @"C:\BinaryFiles\event.log";
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message);
            }
        }

    }


}
