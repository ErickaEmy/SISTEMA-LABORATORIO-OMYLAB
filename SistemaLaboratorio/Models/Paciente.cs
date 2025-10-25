using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaLaboratorio.Models
{
    public partial class Paciente
    {
        [Key]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener exactamente 8 dígitos numéricos.")]
        public string Dni { get; set; } = null!;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly FechaNacimiento { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un sexo es obligatorio.")]
        public string Sexo { get; set; } = null!;

        [Required(ErrorMessage = "El celular es obligatorio.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El celular debe contener exactamente 9 dígitos numéricos.")]
        public string Celular { get; set; } = null!;

        public string? Correo { get; set; } = " ";

        public string? Direccion { get; set; } = " ";

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = null!;

        public virtual ICollection<AnalisisPaciente> AnalisisPacientes { get; set; } = new List<AnalisisPaciente>();

        public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

        public virtual ICollection<Resultado> Resultados { get; set; } = new List<Resultado>();
    }
}
