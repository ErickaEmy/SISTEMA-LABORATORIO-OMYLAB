using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    public partial class AnalisisComponente
    {
        [Key]
        public int AnalisisComponenteId { get; set; }

        [Required(ErrorMessage = "El campo AnalisisId es obligatorio.")]
        public int AnalisisId { get; set; }

        [Required(ErrorMessage = "El campo ComponenteId es obligatorio.")]
        public int ComponenteId { get; set; }

        public virtual Analisis Analisis { get; set; } = null!;

        public virtual Componente Componente { get; set; } = null!;
    }
}
