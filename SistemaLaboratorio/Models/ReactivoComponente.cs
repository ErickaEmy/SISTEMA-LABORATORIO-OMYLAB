using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    public partial class ReactivoComponente
    {
        [Key]
        public int ReactivoComponenteId { get; set; }

        [Required(ErrorMessage = "El ReactivoId es obligatorio.")]
        public int ReactivoId { get; set; }

        [Required(ErrorMessage = "El ComponenteId es obligatorio.")]
        public int ComponenteId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El Componente es obligatorio.")]
        public virtual Componente Componente { get; set; } = null!;

        [Required(ErrorMessage = "El Reactivo es obligatorio.")]
        public virtual Reactivo Reactivo { get; set; } = null!;
    }
}
