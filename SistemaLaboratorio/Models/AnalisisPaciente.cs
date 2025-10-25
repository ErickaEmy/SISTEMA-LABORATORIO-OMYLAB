using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    /// <summary>
    /// Representa el registro de un análisis realizado a un paciente.
    /// Esta clase relaciona el análisis, el paciente y el empleado responsable, 
    /// incluyendo estado y fecha de registro.
    /// Requerimiento: RF-07 - Gestión de análisis pacientes.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-09
    /// </summary>
    public partial class AnalisisPaciente
    {
        /// <summary>
        /// Identificador único del análisis paciente.
        /// </summary>
        [Key]
        public int AnalisisPacienteId { get; set; }

        /// <summary>
        /// Referencia al análisis realizado (FK).
        /// Campo obligatorio para garantizar la integridad referencial.
        /// </summary>
        [Required(ErrorMessage = "El campo AnalisisId es obligatorio.")]
        public int AnalisisId { get; set; }

        /// <summary>
        /// Referencia al paciente al que se realiza el análisis (FK).
        /// </summary>
        [Required(ErrorMessage = "El campo PacienteId es obligatorio.")]
        public int PacienteId { get; set; }

        /// <summary>
        /// Identificador del empleado que registra o atiende el análisis (FK).
        /// </summary>
        [Required(ErrorMessage = "El campo EmpleadoId es obligatorio.")]
        public int EmpleadoId { get; set; }

        /// <summary>
        /// Fecha y hora en que se registró el análisis para trazabilidad.
        /// Formato: día/mes/año hora:minuto.
        /// </summary>
        [Required(ErrorMessage = "El campo FechaHoraRegistro es obligatorio.")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaHoraRegistro { get; set; }

        /// <summary>
        /// Estado actual del análisis (Ej. Pendiente, Cancelado, Completado).
        /// Se limita a 50 caracteres para evitar almacenamiento innecesario.
        /// </summary>
        [Required(ErrorMessage = "El campo Estado es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo Estado no puede superar los 50 caracteres.")]
        public string Estado { get; set; } = null!;

        /// <summary>
        /// Relación con la entidad Análisis para acceso a datos específicos.
        /// </summary>
        public virtual Analisis Analisis { get; set; } = null!;

        /// <summary>
        /// Relación con la entidad Empleado para identificar al responsable.
        /// </summary>
        public virtual Empleado Empleado { get; set; } = null!;

        /// <summary>
        /// Relación con la entidad Paciente para identificar el paciente asociado.
        /// </summary>
        public virtual Paciente Paciente { get; set; } = null!;

        /// <summary>
        /// Colección de componentes específicos que forman parte del análisis 
        /// realizado, con resultados individuales.
        /// </summary>
        public virtual ICollection<ComponenteAnalisisPaciente> ComponenteAnalisisPacientes { get; set; } = new List<ComponenteAnalisisPaciente>();
    }
}
