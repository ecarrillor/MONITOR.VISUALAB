using MONITOR.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR.DTOs
{
    public class CheckBDDTO
    {
        public int Id { get; set; }

        public DateTime Fecha_hora_check { get; set; }

        public DateTime Fecha_hora_respuesta_check { get; set; }

        public double Tiempo_respuesta => (Fecha_hora_respuesta_check - Fecha_hora_check).TotalMilliseconds;

        [MaxLength]
        public string? Respuesta_data { get; set; }

        public bool Error { get; set; }

        public int BaseDatosId { get; set; }

        public string? Nombre_BD { get; set; }

        public string? IP_servidor { get; set; }

        public string? Nombre_servidor { get; set; }

        public ServidorType ServidorType { get; set; }
    }
}
