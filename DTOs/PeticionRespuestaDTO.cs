using MONITOR.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR.DTOs
{
    public class PeticionRespuestaDTO
    {
        public int Id { get; set; }

        [MaxLength(250, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? Url { get; set; }

        [MaxLength(250, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? Nombre_servicio { get; set; }

        [MaxLength]
        public string? Peticion_data { get; set; }

        public DateTime Fecha_hora_peticion { get; set; }

        public MethodType MethdType { get; set; }

        public DateTime Fecha_hora_respuesta { get; set; }

        public int StatusResponse { get; set; }

        [MaxLength]
        public string? Respuesta_data { get; set; }

        public int FlujoId { get; set; }

        [MaxLength(250, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? Nombre_flujo { get; set; }
    }
}
