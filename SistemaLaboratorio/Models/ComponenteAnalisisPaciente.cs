using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

public partial class ComponenteAnalisisPaciente
{
    [Key]
    public int ComponenteAnalisisPacienteId { get; set; }

    public int AnalisisPacienteId { get; set; }

    public int ComponenteId { get; set; }

    public double ResultadoId { get; set; }

    [Required(ErrorMessage = "Debe ingresar un valor numérico según el rango indicado")]
    public double ValorResultado { get; set; }
    public string Resultado { get; set; } = null!;

   
    public virtual AnalisisPaciente AnalisisPaciente { get; set; } = null!;

   
    public virtual Componente Componente { get; set; } = null!;
}
