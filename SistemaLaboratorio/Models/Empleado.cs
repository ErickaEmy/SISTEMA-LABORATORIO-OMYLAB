using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaLaboratorio.Models;

public partial class Empleado : IValidatableObject
{
    [Key]
    public int EmpleadoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    public string Apellidos { get; set; } = null!;

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener exactamente 8 números.")]
    public string Dni { get; set; } = null!;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El celular es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El celular debe contener exactamente 9 números.")]
    public string Celular { get; set; } = null!;

    public string? Correo { get; set; } = " ";

    public string? Direccion { get; set; } = " ";

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [RegularExpression("^(Administrador|Supervisor|Biologo|Recepcionista)$", ErrorMessage = "El rol debe ser: Administrador, Supervisor, Biologo o Recepcionista.")]
    public string Rol { get; set; } = null!;

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = null!;

    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = null!;

    public virtual ICollection<AnalisisPaciente> AnalisisPacientes { get; set; } = new List<AnalisisPaciente>();

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<HistorialAuditoria> HistorialAuditoria { get; set; } = new List<HistorialAuditoria>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validar que la fecha de nacimiento no sea futura
        if (FechaNacimiento > DateOnly.FromDateTime(DateTime.Now))
        {
            yield return new ValidationResult(
                "La fecha de nacimiento no puede ser una fecha futura.",
                new[] { nameof(FechaNacimiento) }
            );
        }
    }
    public virtual ICollection<EmpleadoOtp> Otp { get; set; } = new List<EmpleadoOtp>();


}
