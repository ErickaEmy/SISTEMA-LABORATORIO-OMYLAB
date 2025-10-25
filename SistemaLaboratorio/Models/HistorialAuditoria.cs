using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class HistorialAuditoria
{
    [Key]
    public int HistorialAuditoriaId { get; set; }

    [Required(ErrorMessage = "La actividad es obligatoria.")]
    public string Actividad { get; set; } = null!;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = null!;

    public string? Comentario { get; set; } = "Ninguno";

    [Required(ErrorMessage = "El Id de la entidad es obligatorio.")]
    public int EntidadId { get; set; }

    [Required(ErrorMessage = "La acción es obligatoria.")]
    public string Accion { get; set; } = null!;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El Id del empleado es obligatorio.")]
    public int EmpleadoId { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}