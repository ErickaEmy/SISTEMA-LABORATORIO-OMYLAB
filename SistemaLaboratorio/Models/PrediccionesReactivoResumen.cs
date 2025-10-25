using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaLaboratorio.Models
{
    /// <summary>
    /// Representa un resumen consolidado de predicciones para un reactivo específico,
    /// incluyendo tendencias promedio, periodos de mayor y menor consumo, y conclusiones generadas.
    /// Esta clase facilita la interpretación global de las predicciones de consumo para la gestión del inventario.
    /// Requerimiento: RF-11 - Generar Predicción por Machine Learning.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-09
    /// </summary>
    public class PrediccionesReactivoResumen
    {
        /// <summary>
        /// Identificador único del resumen de predicción.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Número correlativo de la predicción a la que pertenece este resumen.
        /// Permite correlacionar múltiples registros y su evolución.
        /// </summary>
        [Required]
        public int NumeroPrediccion { get; set; }

        /// <summary>
        /// Identificador del reactivo asociado al resumen (clave foránea).
        /// </summary>
        [Required]
        public int ReactivoId { get; set; }

        /// <summary>
        /// Nombre descriptivo del reactivo, para facilitar la identificación en reportes.
        /// Limitado a 100 caracteres.
        /// </summary>
        [StringLength(100)]
        public string NombreReactivo { get; set; }

        /// <summary>
        /// Tendencia promedio calculada a partir de los datos históricos o predicciones,
        /// expresada usualmente en porcentaje mensual.
        /// Este valor indica la dirección general del consumo del reactivo.
        /// </summary>
        public double? TendenciaPromedio { get; set; }

        /// <summary>
        /// Mes en que se proyecta el mayor consumo del reactivo,
        /// almacenado en formato de fecha sin hora para indicar periodo específico.
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime? MesMayorConsumo { get; set; }

        /// <summary>
        /// Mes en que se proyecta el menor consumo del reactivo,
        /// almacenado en formato de fecha sin hora para indicar periodo específico.
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime? MesMenorConsumo { get; set; }

        /// <summary>
        /// Texto descriptivo o conclusión generada por el sistema,
        /// que sintetiza la interpretación de las predicciones y tendencias,
        /// para facilitar la toma de decisiones del laboratorio.
        /// </summary>
        public string TextoConclusion { get; set; }

        /// <summary>
        /// Fecha y hora en que se generó el resumen de predicción,
        /// permitiendo controlar la vigencia y rastrear actualizaciones.
        /// Valor por defecto es la fecha actual al crear el registro.
        /// </summary>
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    }
}
