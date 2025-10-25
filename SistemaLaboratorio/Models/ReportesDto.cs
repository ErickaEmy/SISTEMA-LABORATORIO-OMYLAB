using System;
using System.Collections.Generic;

namespace SistemaLaboratorio.Models
{
    /// <summary>
    /// DTO que representa el detalle de un reactivo utilizado en los reportes.
    /// Incluye su identificador, nombre y la cantidad total disponible o consumida.
    /// Requerimiento: RF-13 - Reportes de gestión.
    /// Caso de uso: CU-13 - Generar reporte.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-30
    /// </summary>
    public class ReactivoDetalleDto
    {
        /// <summary>
        /// Identificador único del reactivo.
        /// </summary>
        public int ReactivoId { get; set; }

        /// <summary>
        /// Nombre comercial o técnico del reactivo.
        /// </summary>
        public string NombreReactivo { get; set; }

        /// <summary>
        /// Cantidad total disponible o consumida del reactivo.
        /// Se maneja como decimal para permitir valores fraccionados.
        /// </summary>
        public decimal CantidadTotal { get; set; }
    }

    /// <summary>
    /// DTO que agrupa los reactivos consumidos en un análisis específico.
    /// Permite identificar qué insumos se utilizaron y en qué cantidad.
    /// </summary>
    public class ReactivoConsumidoPorAnalisisDto
    {
        /// <summary>
        /// Identificador único del análisis.
        /// </summary>
        public int AnalisisId { get; set; }

        /// <summary>
        /// Nombre del análisis clínico en el que se usaron los reactivos.
        /// </summary>
        public string NombreAnalisis { get; set; }

        /// <summary>
        /// Lista de reactivos con sus cantidades consumidas.
        /// </summary>
        public List<ReactivoDetalleDto> Reactivos { get; set; }
    }

    /// <summary>
    /// DTO que representa el consumo total de un reactivo.
    /// Puede estar asociado a un análisis o consultarse de forma independiente.
    /// </summary>
    public class ReactivoConsumidoDto
    {
        /// <summary>
        /// Identificador del análisis relacionado (opcional).
        /// </summary>
        public int? AnalisisId { get; set; }

        /// <summary>
        /// Identificador del reactivo (opcional).
        /// </summary>
        public int? ReactivoId { get; set; }

        /// <summary>
        /// Nombre del reactivo consumido.
        /// </summary>
        public string NombreReactivo { get; set; }

        /// <summary>
        /// Cantidad total consumida del reactivo.
        /// </summary>
        public decimal CantidadTotal { get; set; }
    }

    /// <summary>
    /// DTO que representa un reactivo próximo a vencer.
    /// Permite controlar stock crítico y planificar reposición.
    /// </summary>
    public class ReactivoPorVencerDto
    {
        /// <summary>
        /// Nombre del reactivo.
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Fecha de vencimiento del reactivo.
        /// </summary>
        public DateOnly FechaVencimiento { get; set; }

        /// <summary>
        /// Presentación del reactivo (ej. frasco, caja, unidad).
        /// </summary>
        public string Presentacion { get; set; }

        /// <summary>
        /// Proveedor del reactivo.
        /// </summary>
        public string Proveedor { get; set; }

        /// <summary>
        /// Número de días restantes antes de su vencimiento.
        /// </summary>
        public int DiasPorVencer { get; set; }
    }

    /// <summary>
    /// DTO que representa la cantidad de veces que un análisis ha sido solicitado.
    /// Permite generar reportes de popularidad de análisis clínicos.
    /// </summary>
    public class AnalisisSolicitadoDto
    {
        /// <summary>
        /// Nombre del análisis clínico solicitado.
        /// </summary>
        public string NombreAnalisis { get; set; } = string.Empty;

        /// <summary>
        /// Número de veces que el análisis ha sido solicitado.
        /// </summary>
        public int Cantidad { get; set; }
    }

    /// <summary>
    /// DTO que registra la trazabilidad de actividades dentro del sistema.
    /// Incluye la acción realizada, la entidad afectada y el responsable.
    /// </summary>
    public class HistorialAuditoriaDto
    {
        /// <summary>
        /// Actividad principal realizada (ej. Creación, Modificación).
        /// </summary>
        public string Actividad { get; set; }

        /// <summary>
        /// Descripción detallada de la actividad registrada.
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// Comentarios adicionales sobre la actividad.
        /// </summary>
        public string Comentario { get; set; }

        /// <summary>
        /// Identificador de la entidad afectada por la acción.
        /// </summary>
        public int EntidadId { get; set; }

        /// <summary>
        /// Acción específica realizada (ej. Insertar, Actualizar, Eliminar).
        /// </summary>
        public string Accion { get; set; }

        /// <summary>
        /// Fecha y hora exacta en que se ejecutó la acción.
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Nombre del empleado que realizó la acción.
        /// </summary>
        public string EmpleadoNombre { get; set; }
    }

    /// <summary>
    /// DTO que representa los reactivos más utilizados en el laboratorio.
    /// Permite identificar insumos de mayor consumo para gestión de stock.
    /// </summary>
    public class ReactivoMasUtilizadoDto
    {
        /// <summary>
        /// Nombre del reactivo.
        /// </summary>
        public string NombreReactivo { get; set; }

        /// <summary>
        /// Cantidad total utilizada del reactivo.
        /// </summary>
        public int Cantidad { get; set; }
    }

    /// <summary>
    /// DTO que representa un análisis emitido a un paciente.
    /// Contiene datos del paciente, análisis y empleado responsable.
    /// </summary>
    public class AnalisisEmitidoDto
    {
        /// <summary>
        /// Documento de identidad del paciente.
        /// </summary>
        public string PacienteDni { get; set; }

        /// <summary>
        /// Nombre completo del paciente.
        /// </summary>
        public string PacienteNombreCompleto { get; set; }

        /// <summary>
        /// Nombre del análisis emitido.
        /// </summary>
        public string NombreAnalisis { get; set; }

        /// <summary>
        /// Fecha y hora en que se registró el análisis.
        /// </summary>
        public DateTime FechaHoraRegistro { get; set; }

        /// <summary>
        /// Nombre completo del empleado responsable del análisis.
        /// </summary>
        public string EmpleadoNombreCompleto { get; set; }
    }
}
