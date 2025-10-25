using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    public partial class Analisis
    {
        [Key]
        public int AnalisisId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El tipo de muestra es obligatorio.")]
        [StringLength(50, ErrorMessage = "El tipo de muestra no puede exceder 50 caracteres.")]
        public string TipoMuestra { get; set; } = null!;

        [Required(ErrorMessage = "La condición es obligatoria.")]
        [StringLength(200, ErrorMessage = "La condición no puede exceder 200 caracteres.")]
        public string Condicion { get; set; } = null!;


        public string? Comentario { get; set; } = "Ninguno";


        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero.")]
        public double Precio { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public bool Estado { get; set; }

        public virtual ICollection<AnalisisComponente> AnalisisComponentes { get; set; } = new List<AnalisisComponente>();

        public virtual ICollection<AnalisisPaciente> AnalisisPacientes { get; set; } = new List<AnalisisPaciente>();

        public virtual ICollection<CitaAnalisis> CitaAnalisis { get; set; } = new List<CitaAnalisis>();

        public virtual ICollection<Resultado> Resultados { get; set; } = new List<Resultado>();
    }
}
