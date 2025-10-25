using System;
using System.Collections.Generic;

namespace SistemaLaboratorio.Models;

public partial class Prediccion
{
    public int PrediccionId { get; set; }

    public int? ReactivoId { get; set; }

    public string? Contenido { get; set; }

    public int? MesProyectado { get; set; }

    public double? ConsumoEsperado { get; set; }

    public double? LimiteInferior { get; set; }

    public double? LimiteSuperior { get; set; }

    public DateTime? FechaPrediccion { get; set; }

    public string? Tendencia { get; set; }

    public virtual Reactivo? Reactivo { get; set; }
}
