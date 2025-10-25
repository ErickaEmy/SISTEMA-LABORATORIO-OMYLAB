using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.ViewModel
{
    public class ActualizarResultadoViewModel
    {
        public int ResultadoId { get; set; }

        public string NombrePaciente { get; set; } = string.Empty;

        public string NombreAnalisis { get; set; } = string.Empty;

        public List<ComponenteResultadoDTO> Componentes { get; set; } = new();
        public List<ReferenciaComponenteDTO> Referencias { get; set; } = new();
    }

    public class ComponenteResultadoDTO
    {
        public int ComponenteAnalisisPacienteId { get; set; }

        public string NombreComponente { get; set; } = string.Empty;
        [Required(ErrorMessage = "Debe ingresar un valor numérico según el rango indicado")]
        public double ValorResultado { get; set; }

        public string Resultado { get; set; } = string.Empty;
    }
    public class ReferenciaComponenteDTO
    {
        public string NombreComponente { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public double? EdadMinima { get; set; }
        public double? EdadMaxima { get; set; }
        public double ValorMinimo { get; set; }
        public double ValorMaximo { get; set; }
        public string Unidad { get; set; } = string.Empty;
    }
}
