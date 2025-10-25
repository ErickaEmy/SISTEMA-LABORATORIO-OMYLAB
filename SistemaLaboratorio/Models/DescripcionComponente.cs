using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class DescripcionComponente : IValidatableObject
{
    [Key]
    public int DescripcionComponenteId { get; set; }

    [Required(ErrorMessage = "El campo ComponenteId es obligatorio.")]
    public int ComponenteId { get; set; }

    [Required(ErrorMessage = "El campo ValorMinimo es obligatorio.")]
    public double ValorMinimo { get; set; }

    [Required(ErrorMessage = "El campo ValorMaximo es obligatorio.")]
    public double ValorMaximo { get; set; }

    public string Unidad { get; set; }

    [Required(ErrorMessage = "El campo Sexo es obligatorio.")]
    [RegularExpression("^(Femenino|Masculino|Ambos)$", ErrorMessage = "El campo Sexo solo puede ser 'Femenino', 'Masculino' o 'Ambos'.")]
    public string Sexo { get; set; } = null!;

    [Range(0, 100, ErrorMessage = "La EdadMinima debe estar entre 0 y 100.")]
    public double? EdadMinima { get; set; }

    [Range(0, 100, ErrorMessage = "La EdadMaxima debe estar entre 0 y 100.")]
    public double? EdadMaxima { get; set; }

    [Required]
    public virtual Componente Componente { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EdadMinima.HasValue && EdadMaxima.HasValue)
        {
            if (EdadMinima > EdadMaxima)
            {
                yield return new ValidationResult(
                    "La EdadMinima no puede ser mayor que la EdadMaxima.",
                    new[] { nameof(EdadMinima), nameof(EdadMaxima) }
                );
            }
        }

        if (ValorMinimo > ValorMaximo)
        {
            yield return new ValidationResult(
                "El ValorMinimo no puede ser mayor que el ValorMaximo.",
                new[] { nameof(ValorMinimo), nameof(ValorMaximo) }
            );
        }
    }
}
