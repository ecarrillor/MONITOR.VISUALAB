using Microsoft.Data.SqlClient;
using MONITOR.DTOs;
using MONITOR.Enums;
using MONITOR.SERVICE.VISUALAB.Helpers;
using MONITOR.SERVICE.VISUALAB.Repositories;
using Npgsql;
using OpenHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MONITOR.SERVICE.VISUALAB
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRepository _repository;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IRepository repository, IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _configuration = configuration;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        //RAM info
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    string hostName = Dns.GetHostName();
                    string hostIP = "NA";
                    var ips = Dns.GetHostEntry(hostName).AddressList.LastOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    if (ips != null)
                    {
                        hostIP = ips.ToString();
                    }

                    DateTime checkRegistroSQL = DateTime.Now;
                    try
                    {                        

                        string conectionSQLserver = _configuration["ConnectionSQLserver"]!;
                        SqlConnection con = new SqlConnection(conectionSQLserver);
                        con.Open();
                        _logger.LogInformation($"Conexión exitosa a SQL server en: {DateTimeOffset.Now}.");
                        SqlCommand command = new SqlCommand("SELECT name, state_desc, recovery_model_desc FROM sys.databases WHERE name = 'BDCHS'", con);
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            string? nameDB = reader["name"].ToString();
                            _logger.LogInformation($"Nombre de BD: {nameDB}.");

                            CheckBDDTO checkBDDTO = new CheckBDDTO
                            {
                                Error = false,
                                Fecha_hora_check = checkRegistroSQL,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_BD = nameDB,
                                Respuesta_data = "Ok"
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/bd", checkBDDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de CHECK BD available {message}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"No se encontraron datos");
                        }
                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error de conexión a SQL server: {message}", ex.Message);
                        try
                        {
                            CheckBDDTO checkBDDTO = new CheckBDDTO
                            {
                                Error = true,
                                Fecha_hora_check = checkRegistroSQL,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_BD = "Sin identificar.",
                                Respuesta_data = $"Error de conexión a SQL server: {ex.Message}",
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/bd", checkBDDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de CHECK BD {message}");
                            }
                        }
                        catch(Exception exp)
                        {
                            _logger.LogError("Error en envío a servicio de API: {message}", exp.Message);
                        }
                    }

                    DateTime checkRegistroNP = DateTime.Now;
                    try
                    {
                        string connectionPostgreSQL = _configuration["ConnectionPostgreSQL"]!;
                        NpgsqlConnection conpg = new NpgsqlConnection(connectionPostgreSQL);
                        conpg.Open();
                        _logger.LogInformation($"Conexión exitosa a PostgreSQL en: {DateTimeOffset.Now}.");
                        NpgsqlCommand command = new NpgsqlCommand("SELECT current_database()");
                        NpgsqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            _logger.LogInformation($"Nombre de BD: {reader.GetString(0)}.");

                            CheckBDDTO checkBDDTO = new CheckBDDTO
                            {
                                Error = false,
                                Fecha_hora_check = checkRegistroNP,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_BD = reader.GetString(0),
                                Respuesta_data = "Ok"
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/bd", checkBDDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de CHECK BD {message}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"No se encontraron datos");
                        }
                        conpg.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error de conexión a PostgreSQL: {message}", ex.Message);
                        try
                        {
                            CheckBDDTO checkBDDTO = new CheckBDDTO
                            {
                                Error = true,
                                Fecha_hora_check = checkRegistroNP,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_BD = "Sin identificar.",
                                Respuesta_data = $"Error de conexión a Postgre SQL: {ex.Message}",
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/bd", checkBDDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de CHECK BD {message}");
                            }
                        }
                        catch (Exception exp)
                        {
                            _logger.LogError("Error en envío a servicio de API: {message}", exp.Message);
                        }
                    }

                    DateTime checkRegistroProcess = DateTime.Now;
                    try
                    {
                        _logger.LogInformation($"Inicio de chequeo de procesos en: {DateTimeOffset.Now}.");
                        Process[] processes = Process.GetProcesses();
                        foreach (Process process in processes) 
                        {
                            Console.WriteLine($"Process Name: {process.ProcessName}, ID: {process.Id}");
                            _logger.LogInformation($"Process Name: {process.ProcessName}, ID: {process.Id}");
                        }

                        try
                        {
                            string processName = string.Empty;
                            if(processes.FirstOrDefault() !=  null)
                            {
                                processName = processes.FirstOrDefault()!.ProcessName;
                            }
                            else
                            {
                                processName = "No identificado";
                            }

                            CheckServicioDTO checkServicioDTO = new CheckServicioDTO
                            {
                                Error = false,
                                Fecha_hora_check = checkRegistroProcess,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_Servicio = processName,
                                Respuesta_data = "Ok",
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/services", checkServicioDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error de GET en procesos {message}");
                            }
                        }
                        catch (Exception exp)
                        {
                            _logger.LogError("Error al envío de SERVICE: {message}", exp.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error de chequeo de procesos: {message}", ex.Message);
                        try
                        {
                            CheckServicioDTO checkServicioDTO = new CheckServicioDTO
                            {
                                Error = true,
                                Fecha_hora_check = checkRegistroProcess,
                                Fecha_hora_respuesta_check = DateTime.Now,
                                IP_servidor = hostIP,
                                Nombre_servidor = hostName,
                                ServidorType = ServidorType.Contenedor,
                                Nombre_Servicio = "Sin identificar.",
                                Respuesta_data = $"Error de GET en procesos: {ex.Message}",
                            };
                            var httpResponseBD = await _repository.Post("/api/monitores/services", checkServicioDTO);
                            if (httpResponseBD.Error)
                            {
                                var message = await httpResponseBD.GetErrorMessageAsync();
                                _logger.LogError($"Error de GET en procesos {message}");
                            }
                        }
                        catch (Exception exp)
                        {
                            _logger.LogError("Error al envío de SERVICE: {message}", exp.Message);
                        }
                    }

                    string drivers = string.Empty;
                    DriveInfo[] allDrives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in allDrives)
                    {
                        drivers += "Drive name: " + d.Name + Environment.NewLine + "Drive type: " + d.DriveType + Environment.NewLine;
                        if (d.IsReady)
                        {
                            drivers += "Label: " + d.VolumeLabel + Environment.NewLine +
                                "File system: " + d.DriveFormat + Environment.NewLine +
                                "Available space: " + d.AvailableFreeSpace / (1024 * 1024) + " MB" + Environment.NewLine +
                                "Total size: " + (d.TotalSize / (1024 * 1024)) + " MB." + Environment.NewLine;

                            SistemaInternoDTO monitorSistemaHD = new SistemaInternoDTO
                            {
                                Componente = "Space HD",
                                ComponenteType = ComponenteType.Interno_HD,
                                Fecha_hora = DateTime.Now,
                                Indicador = (double)(d.AvailableFreeSpace / (1024 * 1024)),
                                Indicador_total = (double)(d.TotalSize / (1024 * 1024)),
                                MedidaType = MedidaType.MB,
                                Nombre_servidor = hostName,
                                IP = hostIP,
                                ServidorType = ServidorType.Local,
                            };

                            try
                            {
                                var httpResponseHD = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaHD);
                                if (httpResponseHD.Error)
                                {
                                    var message = await httpResponseHD.GetErrorMessageAsync();
                                    _logger.LogError($"Error al envío de HD available {message}");
                                }
                            }
                            catch(Exception ex) 
                            { 
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }

                    ulong installedMemory;
                    ulong availMemory;
                    MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(memStatus))
                    {
                        installedMemory = memStatus.ullTotalPhys;
                        availMemory = memStatus.ullAvailPhys;
                        SistemaInternoDTO monitorSistemaRAM = new SistemaInternoDTO
                        {
                            Componente = "RAM",
                            ComponenteType = ComponenteType.Interno_RAM,
                            Fecha_hora = DateTime.Now,
                            Indicador = (double)(availMemory),
                            Indicador_total = (double)(installedMemory),
                            MedidaType = MedidaType.MB,
                            Nombre_servidor = hostName,
                            IP = hostIP,
                            ServidorType = ServidorType.Local,
                        };

                        try
                        {
                            var httpResponseRAM = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaRAM);
                            if (httpResponseRAM.Error)
                            {
                                var message = await httpResponseRAM.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de HD available {message}");
                            }
                        }
                        catch (Exception ex) 
                        {
                            Console.WriteLine(ex.ToString());
                        }

                        drivers += "Instaled RAM: " + installedMemory + Environment.NewLine + "RAM disponible: " + availMemory;
                    }

                    string CPUName = Convert.ToString(Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\HARDWARE\\DESCRIPTION\\SYSTEM\\CentralProcessor\\0", "ProcessorNameString", null));
                    ManagementObjectCollection moc;
                    var Processor = new Dictionary<string, string>();

                    try
                    {
                        moc = new ManagementObjectSearcher("select * from Win32_Processor").Get();

                        foreach (ManagementObject obj in moc)
                        {
                            double loadProcesor = 0;
                            try
                            {
                                Processor.Add("CpuStatus", obj["CpuStatus"].ToString());
                            }
                            catch
                            {
                            }

                            try
                            {
                                Processor.Add("Level", obj["Level"].ToString());
                            }
                            catch
                            {
                            }

                            try
                            {
                                Processor.Add("LoadPercentage", obj["LoadPercentage"].ToString());
                                loadProcesor = Convert.ToDouble(obj["LoadPercentage"].ToString());
                            }
                            catch
                            {
                            }

                            try
                            {
                                Processor.Add("Name", obj["Name"].ToString());
                            }
                            catch
                            {
                            }

                            try
                            {
                                Processor.Add("NumberOfCores", obj["NumberOfCores"].ToString());
                            }
                            catch
                            {
                            }

                            try
                            {
                                SistemaInternoDTO monitorSistemaCPU = new SistemaInternoDTO
                                {
                                    Componente = $"Name: {obj["Name"].ToString()}",
                                    ComponenteType = ComponenteType.Interno_CPU,
                                    Fecha_hora = DateTime.Now,
                                    Indicador = loadProcesor + 1,
                                    Indicador_total = 0,
                                    MedidaType = MedidaType.C,
                                    Nombre_servidor = hostName,
                                    IP = hostIP,
                                    ServidorType = ServidorType.Local,
                                };
                                var httpResponseCPU = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaCPU);
                                if (httpResponseCPU.Error)
                                {
                                    var message = await httpResponseCPU.GetErrorMessageAsync();
                                    _logger.LogError($"Error al envío de CPU available {message}");
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                    catch
                    {

                    }

                    drivers += "Read network information..." + Environment.NewLine;
                    NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in adapters)
                    {
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                        drivers += "Name: " + adapter.Name + Environment.NewLine;
                        drivers += "Description: " + adapter.Description + Environment.NewLine;
                        drivers += "Speed: " + adapter.Speed + Environment.NewLine;
                        drivers += "Bytes Sent: " + stats.BytesSent + Environment.NewLine;
                        drivers += "Bytes Received: " + stats.BytesReceived + Environment.NewLine;

                        try
                        {
                            SistemaInternoDTO monitorSistemaSpeed = new SistemaInternoDTO
                            {
                                Componente = $"Name: {adapter.Name}, Description: {adapter.Description}",
                                ComponenteType = ComponenteType.Network,
                                Fecha_hora = DateTime.Now,
                                Indicador = adapter.Speed / 1024,
                                Indicador_total = 0,
                                MedidaType = MedidaType.KB_s,
                                Nombre_servidor = hostName,
                                IP = hostIP,
                                ServidorType = ServidorType.Local,
                            };
                            var httpResponseSpeed = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaSpeed);
                            if (httpResponseSpeed.Error)
                            {
                                var message = await httpResponseSpeed.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de Speed network {message}");
                            }

                            SistemaInternoDTO monitorSistemaReciving = new SistemaInternoDTO
                            {
                                Componente = $"Name: {adapter.Name}, Description: {adapter.Description}, RECIVING",
                                ComponenteType = ComponenteType.Network,
                                Fecha_hora = DateTime.Now,
                                Indicador = stats.BytesReceived / 2 / 1000000,
                                Indicador_total = 0,
                                MedidaType = MedidaType.MB,
                                Nombre_servidor = hostName,
                                IP = hostIP,
                                ServidorType = ServidorType.Local,
                            };
                            var httpResponseReciving = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaReciving);
                            if (httpResponseReciving.Error)
                            {
                                var message = await httpResponseReciving.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de Reciving network {message}");
                            }

                            SistemaInternoDTO monitorSistemaSending = new SistemaInternoDTO
                            {
                                Componente = $"Name: {adapter.Name}, Description: {adapter.Description}, SENDING",
                                ComponenteType = ComponenteType.Network,
                                Fecha_hora = DateTime.Now,
                                Indicador = stats.BytesSent / 2 / 1000000,
                                Indicador_total = 0,
                                MedidaType = MedidaType.MB,
                                Nombre_servidor = hostName,
                                IP = hostIP,
                                ServidorType = ServidorType.Local,
                            };
                            var httpResponseSending = await _repository.Post("/api/monitores/servidor/sistema", monitorSistemaSending);
                            if (httpResponseSending.Error)
                            {
                                var message = await httpResponseSending.GetErrorMessageAsync();
                                _logger.LogError($"Error al envío de Sending network {message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}", DateTimeOffset.Now);
                        _logger.LogInformation($"{drivers}");
                    }

                    await Task.Delay(180000, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Servicio cancelado en: {time}", DateTimeOffset.Now);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        public static async Task<SystemInfo> ReadSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                SystemInfo systemInfo = new SystemInfo();

                SystemVisitor updateVisitor = new SystemVisitor();
                Computer computer = new Computer();

                try
                {
                    computer.Open();
                    computer.CPUEnabled = true;

                    computer.Accept(updateVisitor);

                    foreach (IHardware hw in computer.Hardware
                        .Where(hw => hw.HardwareType == HardwareType.CPU))
                    {
                        foreach (ISensor sensor in hw.Sensors)
                        {
                            switch (sensor.SensorType)
                            {
                                case SensorType.Load:
                                    systemInfo.AddOrUpdateCoreLoad(
                                    sensor.Name, sensor.Value.GetValueOrDefault(0));

                                    break;
                                case SensorType.Temperature:
                                    systemInfo.AddOrUpdateCoreTemp(
                                    sensor.Name, sensor.Value.GetValueOrDefault(0));

                                    break;
                            }
                        }
                    }
                }
                finally
                {
                    computer.Close();
                }

                return systemInfo;
            });
        }
    }
}
