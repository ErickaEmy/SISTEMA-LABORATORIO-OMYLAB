using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models
{
    /// <summary>
    /// Representa el resultado de un análisis clínico registrado para un paciente.
    /// Está vinculado al análisis, al paciente y al registro de análisis realizado previamente.
    /// Una vez almacenado, el resultado no puede ser modificado para garantizar la trazabilidad 
    /// y confiabilidad de la información médica.
    /// Requerimiento: RF-08 - Gestión de resultados.
    /// Caso de uso: CU-08 - Gestionar resultados.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-30
    /// </summary>
    public partial class Resultado
    {
        /// <summary>
        /// Identificador único del resultado (PK).
        /// Se genera automáticamente para diferenciar cada registro.
        /// </summary>
        [Key]
        public int ResultadoId { get; set; }

        /// <summary>
        /// Identificador del análisis clínico al que pertenece este resultado (FK).
        /// Permite establecer la relación directa con la entidad Análisis.
        /// </summary>
        [Required(ErrorMessage = "El campo AnalisisId es obligatorio.")]
        public int AnalisisId { get; set; }

        /// <summary>
        /// Identificador del paciente al que corresponde el resultado (FK).
        /// Permite vincular el resultado directamente con los datos del paciente.
        /// </summary>
        [Required(ErrorMessage = "El campo PacienteId es obligatorio.")]
        public int PacienteId { get; set; }

        /// <summary>
        /// Estado actual del resultado (Ej. Registrado, Validado).
        /// Se establece como no editable una vez guardado.
        /// </summary>
        [Required(ErrorMessage = "El campo Estado es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo Estado no puede superar los 50 caracteres.")]
        public string Estado { get; set; } = null!;

        /// <summary>
        /// Fecha en la que se registró el resultado.
        /// Utiliza el tipo DateOnly para asegurar que solo se guarde la fecha
        /// sin componente de hora.
        /// </summary>
        [Required(ErrorMessage = "El campo FechaRegistro es obligatorio.")]
        public DateOnly FechaRegistro { get; set; }

        /// <summary>
        /// Identificador de la relación con el análisis del paciente (FK).
        /// Garantiza la trazabilidad entre el análisis solicitado y el resultado generado.
        /// </summary>
        [Required(ErrorMessage = "El campo AnalisisPacienteId es obligatorio.")]
        public int AnalisisPacienteId { get; set; }

        /// <summary>
        /// Relación de navegación hacia la entidad Análisis.
        /// Permite acceder a los datos descriptivos del análisis realizado.
        /// </summary>
        public virtual Analisis Analisis { get; set; } = null!;

        /// <summary>
        /// Relación de navegación hacia la entidad Paciente.
        /// Permite acceder a la información del paciente al que pertenece el resultado.
        /// </summary>
        public virtual Paciente Paciente { get; set; } = null!;
    }
}
