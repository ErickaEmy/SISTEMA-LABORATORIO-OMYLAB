using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class Componente
{
    [Key]
    public int ComponenteId { get; set; }

    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El campo Categoría es obligatorio.")]
    public string Categoria { get; set; } = null!;

    [Required]
    public virtual ICollection<AnalisisComponente> AnalisisComponentes { get; set; } = new List<AnalisisComponente>();

    [Required]
    public virtual ICollection<ComponenteAnalisisPaciente> ComponenteAnalisisPacientes { get; set; } = new List<ComponenteAnalisisPaciente>();

    [Required]
    public virtual ICollection<DescripcionComponente> DescripcionComponentes { get; set; } = new List<DescripcionComponente>();

    [Required]
    public virtual ICollection<ReactivoComponente> ReactivoComponentes { get; set; } = new List<ReactivoComponente>();
}
