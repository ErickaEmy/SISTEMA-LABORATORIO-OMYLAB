using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SistemaLaboratorio.Models;
/// <summary>
/// Contexto principal de Entity Framework Core para la base de datos del laboratorio.
/// Esta clase permite la interacción con todas las tablas mediante DbSet,
/// gestionando consultas, inserciones, actualizaciones y eliminaciones.
/// </summary>
public partial class DblaboratorioContext : DbContext
{
    /// /// <summary>
    /// Constructor por defecto, permite inicializar el contexto sin opciones explícitas.
    /// </summary>
    public DblaboratorioContext()
    {
    }
    /// <summary>
    /// Constructor que recibe opciones de configuración para el DbContext.
    /// Permite inyectar la cadena de conexión, proveedor de base de datos y otras configuraciones.
    /// </summary>
    /// <param name="options">Opciones de configuración del DbContext.</param>
    public DblaboratorioContext(DbContextOptions<DblaboratorioContext> options)
        : base(options)
    {
    }

    // ========================
    // Definición de DbSets
    // ========================
    // Cada DbSet representa una tabla en la base de datos
    // y permite trabajar con ella mediante LINQ y operaciones CRUD.

    public virtual DbSet<Analisis> Analisis { get; set; }

    public virtual DbSet<AnalisisComponente> AnalisisComponente { get; set; }

    public virtual DbSet<AnalisisPaciente> AnalisisPaciente { get; set; }

    public virtual DbSet<CitaAnalisis> CitaAnalisis { get; set; }

    public virtual DbSet<Cita> Cita { get; set; }

    public virtual DbSet<Componente> Componente { get; set; }

    public virtual DbSet<ComponenteAnalisisPaciente> ComponenteAnalisisPaciente { get; set; }

    public virtual DbSet<Consumo> Consumo { get; set; }

    public virtual DbSet<DescripcionComponente> DescripcionComponente { get; set; }

    public virtual DbSet<Empleado> Empleado { get; set; }

    public virtual DbSet<HistorialAuditoria> HistorialAuditoria { get; set; }

    public virtual DbSet<Insumo> Insumo { get; set; }

    public virtual DbSet<Paciente> Paciente { get; set; }

    public virtual DbSet<Prediccion> Prediccion { get; set; }

    public virtual DbSet<PrediccionesReactivo> PrediccionesReactivo { get; set; }

    public virtual DbSet<PrediccionesReactivoResumen> PrediccionesReactivoResumen { get; set; }

    public virtual DbSet<Reactivo> Reactivo { get; set; }

    public virtual DbSet<ReactivoComponente> ReactivoComponente { get; set; }

    public virtual DbSet<Resultado> Resultados { get; set; }
    public virtual DbSet<EmpleadoOtp> EmpleadoOtp { get; set; } = null!;
    /// <summary>
    /// Método que permite configurar el DbContext si no se han especificado opciones externas.
    /// Normalmente se utiliza para definir la cadena de conexión y el proveedor de base de datos.
    /// Actualmente se deja vacío porque la configuración se realiza mediante inyección de dependencias.
    /// </summary>
    /// <param name="optionsBuilder">Objeto que permite configurar el DbContext.</param>

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    /// <summary>
    /// Método que configura el modelo de datos y las relaciones entre entidades usando Fluent API.
    /// Se utiliza para definir claves primarias, restricciones, tipos de datos, longitudes y relaciones FK.
    /// </summary>
    /// <param name="modelBuilder">Objeto que permite configurar la estructura de la base de datos.</param>

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ==========================
        // Configuración de la entidad Analisis
        // ==========================
        modelBuilder.Entity<Analisis>(entity =>
        {
            // Define la clave primaria de la tabla Analisis
            entity.HasKey(e => e.AnalisisId).HasName("PK__Analisis__FC7EFAF4509E7423");

            // Nombre de la tabla en la base de datos
            entity.ToTable("Analisis");

            // Configuración de propiedades con restricciones de longitud y tipo
            entity.Property(e => e.Comentario)
                .HasMaxLength(300)      // Máximo 300 caracteres
                .IsUnicode(false);      // No utiliza Unicode (ASCII)

            entity.Property(e => e.Condicion)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Estado)
                .HasDefaultValue(true); // Valor por defecto en la base de datos

            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.TipoMuestra)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        // ==========================
        // Configuración de la entidad AnalisisComponente
        // ==========================
        modelBuilder.Entity<AnalisisComponente>(entity =>
        {
            // Define la clave primaria de la tabla AnalisisComponente
            entity.HasKey(e => e.AnalisisComponenteId).HasName("PK__Analisis__D5901BC881A07BD7");

            // Nombre de la tabla en la base de datos
            entity.ToTable("AnalisisComponente");

            // Define relación muchos a uno con Analisis
            entity.HasOne(d => d.Analisis)
                .WithMany(p => p.AnalisisComponentes)
                .HasForeignKey(d => d.AnalisisId)
                .HasConstraintName("FK__AnalisisC__Anali__72C60C4A");

            // Define relación muchos a uno con Componente
            entity.HasOne(d => d.Componente)
                .WithMany(p => p.AnalisisComponentes)
                .HasForeignKey(d => d.ComponenteId)
                .HasConstraintName("FK__AnalisisC__Compo__73BA3083");
        });

        // ==========================
        // Configuración de la entidad AnalisisPaciente
        // ==========================
        modelBuilder.Entity<AnalisisPaciente>(entity =>
        {
            // Clave primaria de la tabla AnalisisPaciente
            entity.HasKey(e => e.AnalisisPacienteId).HasName("PK__Analisis__C3B91627C2F9C24F");

            // Nombre de la tabla
            entity.ToTable("AnalisisPaciente");

            // Configuración de propiedades
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.FechaHoraRegistro)
                .HasColumnType("datetime"); // Tipo datetime en la base de datos

            // Relaciones muchos a uno con las tablas Analisis, Empleado y Paciente
            entity.HasOne(d => d.Analisis)
                .WithMany(p => p.AnalisisPacientes)
                .HasForeignKey(d => d.AnalisisId)
                .HasConstraintName("FK__AnalisisP__Anali__76969D2E");

            entity.HasOne(d => d.Empleado)
                .WithMany(p => p.AnalisisPacientes)
                .HasForeignKey(d => d.EmpleadoId)
                .HasConstraintName("FK__AnalisisP__Emple__778AC167");

            entity.HasOne(d => d.Paciente)
                .WithMany(p => p.AnalisisPacientes)
                .HasForeignKey(d => d.PacienteId)
                .HasConstraintName("FK__AnalisisP__Pacie__787EE5A0");
        });



        // ==========================
        // Configuración de la entidad CitaAnalisis
        // ==========================
        modelBuilder.Entity<CitaAnalisis>(entity =>
        {
            // Define la clave primaria de la tabla CitaAnalisis
            entity.HasKey(e => e.CitaAnalisisId).HasName("PK__CitaAnal__EC03E88939938A55");

            // Nombre de la tabla en la base de datos
            entity.ToTable("CitaAnalisis");

            // Relación muchos a uno con la tabla Analisis
            entity.HasOne(d => d.Analisis)
                .WithMany(p => p.CitaAnalisis)
                .HasForeignKey(d => d.AnalisisId)
                .HasConstraintName("FK__CitaAnali__Anali__6EF57B66");

            // Relación muchos a uno con la tabla Cita
            entity.HasOne(d => d.Cita)
                .WithMany(p => p.CitaAnalisis)
                .HasForeignKey(d => d.CitaId)
                .HasConstraintName("FK__CitaAnali__CitaI__6FE99F9F");
        });

        // ==========================
        // Configuración de la entidad Cita
        // ==========================
        modelBuilder.Entity<Cita>(entity =>
        {
            // Define la clave primaria de la tabla Cita
            entity.HasKey(e => e.CitaId).HasName("PK__Cita__F0E2D9D2DCC9B19A");

            // Configuración de propiedades de la tabla
            entity.Property(e => e.Comentario).HasColumnType("text"); // Comentarios largos
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false); // Estado de la cita
            entity.Property(e => e.Sede)
                .HasMaxLength(100)
                .IsUnicode(false); // Sede donde se realizará la cita

            // Relación muchos a uno con la tabla Empleado
            entity.HasOne(d => d.Empleado)
                .WithMany(p => p.Cita)
                .HasForeignKey(d => d.EmpleadoId)
                .HasConstraintName("FK__Cita__EmpleadoId__6B24EA82");

            // Relación muchos a uno con la tabla Paciente
            entity.HasOne(d => d.Paciente)
                .WithMany(p => p.Cita)
                .HasForeignKey(d => d.PacienteId)
                .HasConstraintName("FK__Cita__PacienteId__6C190EBB");
        });

        // ==========================
        // Configuración de la entidad Componente
        // ==========================
        modelBuilder.Entity<Componente>(entity =>
        {
            // Clave primaria de la tabla Componente
            entity.HasKey(e => e.ComponenteId).HasName("PK__Componen__CFD1B39E8525715D");

            // Nombre de la tabla en la base de datos
            entity.ToTable("Componente");

            // Configuración de propiedades
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .IsUnicode(false); // Categoría del componente

            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false); // Nombre del componente
        });

        // ==========================
        // Configuración de la entidad ComponenteAnalisisPaciente
        // ==========================
        modelBuilder.Entity<ComponenteAnalisisPaciente>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.ComponenteAnalisisPacienteId).HasName("PK__Componen__90F9790495B51F0E");

            // Nombre de la tabla
            entity.ToTable("ComponenteAnalisisPaciente");

            // Propiedad Resultado, tipo texto para resultados largos
            entity.Property(e => e.Resultado).HasColumnType("text");

            // Relación muchos a uno con AnalisisPaciente
            entity.HasOne(d => d.AnalisisPaciente)
                .WithMany(p => p.ComponenteAnalisisPacientes)
                .HasForeignKey(d => d.AnalisisPacienteId)
                .HasConstraintName("FK__Component__Anali__7B5B524B");

            // Relación muchos a uno con Componente
            entity.HasOne(d => d.Componente)
                .WithMany(p => p.ComponenteAnalisisPacientes)
                .HasForeignKey(d => d.ComponenteId)
                .HasConstraintName("FK__Component__Compo__7C4F7684");
        });

        // ==========================
        // Configuración de la entidad Consumo
        // ==========================
        modelBuilder.Entity<Consumo>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.ConsumoId).HasName("PK__Consumo__206D9D26B68FB7FB");

            // Nombre de la tabla
            entity.ToTable("Consumo");

            // Configuración de propiedades
            entity.Property(e => e.Año)
                .HasDefaultValueSql("(datepart(year,getdate()))"); // Año actual por defecto

            entity.Property(e => e.CantidadConsumida)
                .HasColumnType("decimal(10, 2)"); // Cantidad consumida con precisión decimal

            entity.Property(e => e.Comentario)
                .HasMaxLength(250)
                .IsUnicode(false); // Comentario opcional sobre consumo

            entity.Property(e => e.DiaSemana)
                .HasMaxLength(10)
                .IsUnicode(false); // Día de la semana

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())"); // Fecha actual por defecto

            entity.Property(e => e.Mes)
                .HasDefaultValueSql("(datepart(month,getdate()))"); // Mes actual por defecto

            entity.Property(e => e.NombreReactivo)
                .HasMaxLength(100)
                .IsUnicode(false); // Nombre del reactivo consumido

            // Relación muchos a uno con Reactivo, no permite eliminar el reactivo si existen consumos asociados
            entity.HasOne(d => d.Reactivo)
                .WithMany(p => p.Consumos)
                .HasForeignKey(d => d.ReactivoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Consumo__Reactiv__01142BA1");
        });

        // ==========================
        // Configuración de la entidad DescripcionComponente
        // ==========================
        modelBuilder.Entity<DescripcionComponente>(entity =>
        {
            // Define la clave primaria de la tabla DescripcionComponente
            // Esta propiedad identifica de manera única cada registro en la tabla
            entity.HasKey(e => e.DescripcionComponenteId).HasName("PK__Descripc__F99A4FF1FFD6BF07");

            // Se asigna explícitamente el nombre de la tabla en la base de datos
            entity.ToTable("DescripcionComponente");

            // Propiedad Sexo: longitud máxima de 10 caracteres, sin codificación Unicode
            // Se utiliza para limitar la entrada y optimizar almacenamiento
            entity.Property(e => e.Sexo)
                .HasMaxLength(10)
                .IsUnicode(false);

            // Propiedad Unidad: longitud máxima de 15 caracteres, sin codificación Unicode
            // Representa la unidad de medida del componente (por ejemplo mg/dL, mmol/L)
            entity.Property(e => e.Unidad)
                .HasMaxLength(15)
                .IsUnicode(false);

            // Definición de relación muchos a uno con la entidad Componente
            // Permite navegar desde DescripcionComponente hacia su Componente asociado
            entity.HasOne(d => d.Componente)
                .WithMany(p => p.DescripcionComponentes)
                .HasForeignKey(d => d.ComponenteId)
                .HasConstraintName("FK__Descripci__Compo__03F0984C");
        });

        // ==========================
        // Configuración de la entidad Empleado
        // ==========================
        modelBuilder.Entity<Empleado>(entity =>
        {
            // Clave primaria que identifica de forma única cada empleado
            entity.HasKey(e => e.EmpleadoId).HasName("PK__Empleado__958BE910908CCC9B");

            // Se define el nombre de la tabla
            entity.ToTable("Empleado");

            // Índices únicos para garantizar que DNI y Usuario no se repitan
            entity.HasIndex(e => e.Dni, "UQ__Empleado__C030857565590CFB").IsUnique();
            entity.HasIndex(e => e.Usuario, "UQ__Empleado__E3237CF7C5F00F38").IsUnique();

            // Configuración detallada de las propiedades
            // Limitando longitud y evitando Unicode innecesario para optimizar almacenamiento
            entity.Property(e => e.Apellidos).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Celular).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Contrasena).HasMaxLength(100).IsUnicode(false); // Encriptar antes de guardar
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Direccion).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Dni).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Estado).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.FechaNacimiento).HasDefaultValue(new DateOnly(1900, 1, 1)); // Valor por defecto para registros incompletos
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Rol).HasMaxLength(50).IsUnicode(false); // Roles predefinidos: Administrador, Supervisor, Biólogo, Recepcionista
            entity.Property(e => e.Usuario).HasMaxLength(50).IsUnicode(false);
        });

        // ==========================
        // Configuración de la entidad HistorialAuditoria
        // ==========================
        modelBuilder.Entity<HistorialAuditoria>(entity =>
        {
            // Clave primaria que identifica cada acción registrada en el historial
            entity.HasKey(e => e.HistorialAuditoriaId).HasName("PK__Historia__E68B82F305B9B4C6");

            // Configuración de campos de texto y fecha
            entity.Property(e => e.Accion).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Actividad).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Comentario).HasColumnType("text"); // Comentarios largos
            entity.Property(e => e.Descripcion).HasColumnType("text"); // Detalle de la acción
            entity.Property(e => e.Fecha).HasColumnType("datetime"); // Fecha exacta de la acción

            // Relación muchos a uno con Empleado, indicando quién realizó la acción
            entity.HasOne(d => d.Empleado)
                .WithMany(p => p.HistorialAuditoria)
                .HasForeignKey(d => d.EmpleadoId)
                .HasConstraintName("FK__Historial__Emple__0B91BA14");
        });

        // Configuración de EmpleadoOtp
        modelBuilder.Entity<EmpleadoOtp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EmpleadoOtp");

            entity.Property(e => e.Codigo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.Expiracion)
                .IsRequired();

            entity.Property(e => e.Usado)
                .HasDefaultValue(false);

            // Relación con Empleado
            entity.HasOne(d => d.Empleado)
                .WithMany(p => p.Otp)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmpleadoOtp_Empleado");
        });

        // ==========================
        // Configuración de la entidad Insumo
        // ==========================
        modelBuilder.Entity<Insumo>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.InsumoId).HasName("PK__Insumo__C10BE95686EDA84E");

            // Nombre de la tabla
            entity.ToTable("Insumo");

            // Propiedades principales con longitud máxima
            entity.Property(e => e.Descripcion).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.Estado).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.UnidadMedida).HasMaxLength(50).IsUnicode(false); // Unidad de consumo del insumo
        });

        // ==========================
        // Configuración de la entidad Paciente
        // ==========================
        modelBuilder.Entity<Paciente>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.PacienteId).HasName("PK__Paciente__9353C01F88E1CB4D");

            // Nombre de la tabla
            entity.ToTable("Paciente");

            // Índice único en el DNI para evitar duplicados
            entity.HasIndex(e => e.Dni, "UQ__Paciente__C030857554545813").IsUnique();

            // Propiedades de texto y fechas
            entity.Property(e => e.Apellidos).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Celular).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Direccion).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Dni).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Estado).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.FechaNacimiento).HasDefaultValue(new DateOnly(1900, 1, 1));
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Sexo).HasMaxLength(15).IsUnicode(false);
        });

        // ==========================
        // Configuración de la entidad Prediccion
        // ==========================
        modelBuilder.Entity<Prediccion>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.PrediccionId).HasName("PK__Predicci__67A9814128FEAC7A");

            // Nombre de la tabla
            entity.ToTable("Prediccion");

            // Propiedades
            entity.Property(e => e.Contenido).HasColumnType("text"); // Texto de la predicción generada
            entity.Property(e => e.FechaPrediccion).HasColumnType("datetime"); // Fecha y hora de generación
            entity.Property(e => e.Tendencia).HasMaxLength(100).IsUnicode(false); // Descripción corta de la tendencia

            // Relación con la entidad Reactivo, indicando a qué reactivo pertenece la predicción
            entity.HasOne(d => d.Reactivo)
                .WithMany(p => p.Prediccions)
                .HasForeignKey(d => d.ReactivoId)
                .HasConstraintName("FK__Prediccio__React__09A971A2");
        });

        // ==========================
        // Configuración de la entidad PrediccionesReactivo
        // ==========================
        modelBuilder.Entity<PrediccionesReactivo>(entity =>
        {
            // Clave primaria
            entity.HasKey(e => e.Id).HasName("PK__Predicci__3214EC071A36A484");

            // Nombre de la tabla
            entity.ToTable("PrediccionesReactivo");

            // Propiedades
            entity.Property(e => e.FechaGeneracion).HasColumnType("datetime"); // Fecha de generación de la predicción
            entity.Property(e => e.NombreReactivo).HasMaxLength(100); // Nombre del reactivo asociado
        });

        // ==========================
        // Configuración de la entidad PrediccionesReactivoResumen
        // ==========================
        modelBuilder.Entity<PrediccionesReactivoResumen>(entity =>
        {
            // Clave primaria que identifica de forma única cada registro de resumen de predicciones
            entity.HasKey(e => e.Id).HasName("PK__Predicci__3214EC07C1AE3FD6");

            // Propiedad FechaGeneracion: almacena la fecha y hora de creación del resumen
            entity.Property(e => e.FechaGeneracion).HasColumnType("datetime");

            // Propiedad NombreReactivo: nombre del reactivo asociado al resumen de predicción
            entity.Property(e => e.NombreReactivo).HasMaxLength(100);
        });

        // ==========================
        // Configuración de la entidad Reactivo
        // ==========================
        modelBuilder.Entity<Reactivo>(entity =>
        {
            // Clave primaria que identifica cada reactivo de laboratorio
            entity.HasKey(e => e.ReactivoId).HasName("PK__Reactivo__A4847928C0811837");

            // Nombre de la tabla en la base de datos
            entity.ToTable("Reactivo");

            // Propiedades principales del reactivo
            entity.Property(e => e.Nombre)       // Nombre del reactivo
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.Presentacion) // Presentación física o empaque del reactivo
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.Proveedor)    // Nombre del proveedor del reactivo
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        // ==========================
        // Configuración de la entidad ReactivoComponente
        // ==========================
        modelBuilder.Entity<ReactivoComponente>(entity =>
        {
            // Clave primaria que identifica la relación entre reactivos y componentes
            entity.HasKey(e => e.ReactivoComponenteId).HasName("PK__Reactivo__D72AF2D7C9EDEC18");

            // Nombre de la tabla
            entity.ToTable("ReactivoComponente");

            // Relación muchos a uno con Componente
            // Permite navegar desde ReactivoComponente hacia el Componente asociado
            entity.HasOne(d => d.Componente)
                .WithMany(p => p.ReactivoComponentes)
                .HasForeignKey(d => d.ComponenteId)
                .HasConstraintName("FK__ReactivoC__Compo__2DE6D218");

            // Relación muchos a uno con Reactivo
            // Permite navegar desde ReactivoComponente hacia el Reactivo asociado
            entity.HasOne(d => d.Reactivo)
                .WithMany(p => p.ReactivoComponentes)
                .HasForeignKey(d => d.ReactivoId)
                .HasConstraintName("FK__ReactivoC__React__2EDAF651");
        });

        // ==========================
        // Configuración de la entidad Resultado
        // ==========================
        modelBuilder.Entity<Resultado>(entity =>
        {
            // Clave primaria que identifica cada resultado individual de análisis de paciente
            entity.HasKey(e => e.ResultadoId).HasName("PK__Resultad__7904DD613D99713F");

            // Nombre de la tabla en la base de datos
            entity.ToTable("Resultado");

            // Propiedad Estado: indica si el resultado está Pendiente, Completado, etc.
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);

            // Relación muchos a uno con Analisis
            // Permite navegar desde Resultado hacia el Análisis asociado
            entity.HasOne(d => d.Analisis)
                .WithMany(p => p.Resultados)
                .HasForeignKey(d => d.AnalisisId)
                .HasConstraintName("FK__Resultado__Anali__10566F31");

            // Relación muchos a uno con Paciente
            // Permite navegar desde Resultado hacia el Paciente asociado
            entity.HasOne(d => d.Paciente)
                .WithMany(p => p.Resultados)
                .HasForeignKey(d => d.PacienteId)
                .HasConstraintName("FK__Resultado__Pacie__114A936A");
        });

        // ==========================
        // Llamada parcial para permitir extensiones de configuración fuera de este archivo
        // ==========================
        OnModelCreatingPartial(modelBuilder);
    }

    // Método parcial definido para extender la configuración del modelo en otros archivos

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
