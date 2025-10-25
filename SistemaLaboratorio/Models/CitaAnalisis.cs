using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class CitaAnalisis
{
    public int CitaAnalisisId { get; set; }

    [Required(ErrorMessage = "El campo CitaId es obligatorio.")]
    public int CitaId { get; set; }

    [Required(ErrorMessage = "El campo AnalisisId es obligatorio.")]
    public int AnalisisId { get; set; }

    [ValidateNever]          // <- evita que MVC intente validarlas
    public virtual Analisis? Analisis { get; set; }

    [ValidateNever]
    public virtual Cita? Cita { get; set; }
}
