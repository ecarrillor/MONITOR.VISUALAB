using MONITOR.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR.DTOs
{
    public class SistemaInternoDTO
    {
        public int Id { get; set; }

        public DateTime Fecha_hora { get; set; }

        [MaxLength(250, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? Componente { get; set; }

        public double Indicador { get; set; }

        public double Indicador_total { get; set; }

        public MedidaType MedidaType { get; set; }

        public ComponenteType ComponenteType { get; set; }

        public int ServidorId { get; set; }

        public int ParametroId { get; set; }

        [MaxLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? IP { get; set; }

        [MaxLength(1000, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string? Nombre_servidor { get; set; }

        public ServidorType ServidorType { get; set; }
    }
}
