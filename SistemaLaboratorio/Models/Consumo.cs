using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaLaboratorio.Models;

/// <summary>
/// Representa un registro del consumo de un reactivo en el laboratorio en un día específico.
/// Esta clase almacena información histórica sobre la cantidad utilizada de cada reactivo,
/// facilitando análisis estadísticos, control de inventario y trazabilidad del uso de reactivos.
/// Requerimiento: Gestión de inventario y control de consumos.
/// Autor: Ericka Esther Martinez Yufra
/// Fecha: 2025-08-16
/// </summary>
public partial class Consumo
{
    /// <summary>
    /// Identificador único del registro de consumo.
    /// </summary>
    [Key]
    public int ConsumoId { get; set; }

    /// <summary>
    /// Fecha específica en la que se registró el consumo.
    /// Utiliza el tipo DateOnly para almacenar únicamente la fecha sin hora.
    /// </summary>
    public DateOnly Fecha { get; set; }

    /// <summary>
    /// Nombre del día de la semana correspondiente a la fecha del consumo.
    /// Ejemplo: "Lunes", "Martes", etc., para facilitar análisis por días.
    /// </summary>
    public string DiaSemana { get; set; } = null!;

    /// <summary>
    /// Número del mes en que se registró el consumo (1 a 12), útil para agrupar datos por mes.
    /// </summary>
    public int Mes { get; set; }

    /// <summary>
    /// Año en que se registró el consumo, para análisis histórico y generación de reportes.
    /// </summary>
    public int Año { get; set; }

    /// <summary>
    /// Identificador del reactivo consumido (clave foránea).
    /// </summary>
    public int ReactivoId { get; set; }

    /// <summary>
    /// Nombre del reactivo consumido, utilizado para mostrar información clara en reportes y vistas.
    /// </summary>
    public string NombreReactivo { get; set; } = null!;

    /// <summary>
    /// Cantidad de reactivo efectivamente consumida en la fecha indicada.
    /// Se almacena en tipo decimal para permitir valores precisos.
    /// </summary>
    public decimal CantidadConsumida { get; set; }

    /// <summary>
    /// Comentario opcional que describe particularidades del consumo o notas importantes
    /// sobre el registro, como incidencias o ajustes manuales.
    /// </summary>
    public string? Comentario { get; set; }

    /// <summary>
    /// Identificador opcional del análisis asociado al consumo, si corresponde.
    /// Permite vincular consumos a análisis específicos realizados en el laboratorio.
    /// </summary>
    public int? AnalisisId { get; set; }

    /// <summary>
    /// Propiedad de navegación hacia la entidad Reactivo.
    /// Permite acceder a toda la información del reactivo asociado al consumo.
    /// </summary>
    public virtual Reactivo Reactivo { get; set; } = null!;
}
