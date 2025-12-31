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

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}", DateTimeOffset.Now);
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
