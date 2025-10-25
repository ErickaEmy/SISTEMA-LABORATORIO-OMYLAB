using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    public partial class Reactivo
    {
        [Key]
        public int ReactivoId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "La capacidad es obligatoria.")]
        public int Capacidad { get; set; }

        [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly FechaIngreso { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly FechaVencimiento { get; set; }

        public string? Proveedor { get; set; } = " ";

        [Required(ErrorMessage = "La presentación es obligatoria.")]
        public string Presentacion { get; set; } = null!;

        [Required(ErrorMessage = "La cantidad total es obligatoria.")]
        public int CantidadTotal { get; set; }

        [Required(ErrorMessage = "La capacidad total es obligatoria.")]
        public int CapacidadTotal { get; set; }

        [Required(ErrorMessage = "La disponibilidad es obligatoria.")]
        public int Disponibilidad { get; set; }

        public virtual ICollection<Consumo> Consumos { get; set; } = new List<Consumo>();

        public virtual ICollection<Prediccion> Prediccions { get; set; } = new List<Prediccion>();

        public virtual ICollection<ReactivoComponente> ReactivoComponentes { get; set; } = new List<ReactivoComponente>();
    }
}
