using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    public class EmpleadoOtp
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string Codigo { get; set; } = null!;
        public DateTime Expiracion { get; set; }
        public bool Usado { get; set; } = false;

        public virtual Empleado Empleado { get; set; } = null!;
    }
}