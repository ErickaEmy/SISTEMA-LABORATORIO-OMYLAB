using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaLaboratorio.Models
{
    /// <summary>
    /// Representa una predicción del consumo esperado de un reactivo en un mes específico.
    /// Esta clase almacena resultados de modelos de machine learning que pronostican el uso futuro
    /// de reactivos para optimizar la gestión de inventario y abastecimiento.
    /// Requerimiento: RF-11 - Generar Predicción por Machine Learning.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-09
    /// </summary>
    public class PrediccionesReactivo
    {
        /// <summary>
        /// Identificador único de la predicción.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Número correlativo de la predicción generada, útil para seguimiento y auditoría.
        /// </summary>
        [Required]
        public int NumeroPrediccion { get; set; }

        /// <summary>
        /// Identificador del reactivo al que corresponde la predicción (FK).
        /// </summary>
        [Required]
        public int ReactivoId { get; set; }

        /// <summary>
        /// Nombre descriptivo del reactivo, para facilitar la interpretación del pronóstico.
        /// Limitado a 100 caracteres.
        /// </summary>
        [StringLength(100)]
        public string NombreReactivo { get; set; }

        /// <summary>
        /// Mes al que corresponde la predicción, almacenado en formato fecha sin hora.
        /// Representa el periodo para el cual se estima el consumo.
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime? Mes { get; set; }

        /// <summary>
        /// Valor numérico del consumo esperado de reactivo para el mes indicado.
        /// Puede ser nulo si no se dispone de predicción para ese periodo.
        /// </summary>
        public double? ConsumoEsperado { get; set; }

        /// <summary>
        /// Porcentaje estimado de cambio respecto al consumo histórico o predicción previa.
        /// Ayuda a identificar tendencias de aumento o disminución en el consumo.
        /// </summary>
        public double? PorcentajeCambio { get; set; }

        /// <summary>
        /// Fecha y hora en que se generó esta predicción.
        /// Valor por defecto es la fecha actual al crear el registro.
        /// </summary>
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    }
}
