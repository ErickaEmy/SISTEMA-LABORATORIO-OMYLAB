using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class Cita
{
    [Key]
    public int CitaId { get; set; }

    [Required(ErrorMessage = "El campo PacienteId es obligatorio.")]
    public int? PacienteId { get; set; }

    [Required(ErrorMessage = "El campo EmpleadoId es obligatorio.")]
    public int? EmpleadoId { get; set; }

    [Required(ErrorMessage = "El campo Fecha es obligatorio.")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "El campo Hora es obligatorio.")]
    public TimeOnly Hora { get; set; }

    [Required(ErrorMessage = "El campo Estado es obligatorio.")]
    [RegularExpression("^(Pendiente|Completada|Cancelada)$", ErrorMessage = "El estado debe ser: Pendiente, Completada o Cancelada.")]
    public string Estado { get; set; } = null!;

    [Required(ErrorMessage = "El campo Sede es obligatorio.")]
    [RegularExpression("^(Blondel|Solidaridad|Centro)$", ErrorMessage = "La sede debe ser: Blondel, Solidaridad o Centro.")]
    public string Sede { get; set; } = null!;

    public string? Comentario { get; set; } = "Ninguno";

    [ValidateNever]// <-- evita que MVC intente validarlas
    public virtual Empleado? Empleado { get; set; }

    [ValidateNever]
    public virtual Paciente? Paciente { get; set; }

    [ValidateNever]
    public virtual ICollection<CitaAnalisis> CitaAnalisis { get; set; } = new List<CitaAnalisis>();

}
