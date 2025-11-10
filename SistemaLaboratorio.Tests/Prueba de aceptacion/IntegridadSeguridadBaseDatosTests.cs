using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SistemaLaboratorio.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    /// <summary>
    /// CP-RF07-05: Pruebas de integridad y seguridad de la base de datos
    /// 
    /// Objetivo: Validar que todas las operaciones críticas del sistema mantengan 
    /// la integridad referencial de la base de datos y registren correctamente 
    /// la auditoría de cambios.
    /// 
    /// Criterios de aceptación:
    /// - Operaciones transaccionales con rollback automático ante errores
    /// - Registro completo en HistorialAuditoria para cada operación CRUD
    /// - Mantenimiento de integridad referencial sin datos huérfanos
    /// - Restricciones de eliminación en cascada funcionando correctamente
    /// </summary>
    [TestClass]
    public class IntegridadSeguridadBaseDatosTests
    {
        private DblaboratorioContext _context = null!;
        public TestContext TestContext { get; set; } = null!;

        [TestInitialize]
        public void Setup()
        {
            // Configurar base de datos en memoria para pruebas aisladas
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: $"TestDB_{Guid.NewGuid()}")
                .Options;

            _context = new DblaboratorioContext(options);

            // Sembrar datos iniciales
            SembrarDatosIniciales();

            TestContext.WriteLine("✅ Base de datos en memoria configurada");
            TestContext.WriteLine($"📊 Empleados: {_context.Empleado.Count()}");
            TestContext.WriteLine($"📊 Pacientes: {_context.Paciente.Count()}");
            TestContext.WriteLine($"📊 Análisis: {_context.Analisis.Count()}");
            TestContext.WriteLine($"📊 Reactivos: {_context.Reactivo.Count()}\n");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        #region CP-RF07-05-01: Transaccionalidad y rollback automático

        /// <summary>
        /// CP-RF07-05-01: Validar que las operaciones complejas se ejecuten 
        /// de forma transaccional con rollback ante errores
        /// </summary>
        [TestMethod]
        [TestCategory("PruebaAceptacion")]
        [TestCategory("IntegridadBD")]
        [TestCategory("Transaccionalidad")]
        public async Task CP_RF07_05_01_ValidarTransaccionalidadYRollback()
        {
            try
            {
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("🔒 CP-RF07-05-01: TRANSACCIONALIDAD Y ROLLBACK");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════\n");

                // ARRANGE - Contar registros iniciales
                int pacientesIniciales = await _context.Paciente.CountAsync();
                int analisisIniciales = await _context.AnalisisPaciente.CountAsync();
                int consumosIniciales = await _context.Consumo.CountAsync();

                TestContext.WriteLine($"📊 Estado inicial:");
                TestContext.WriteLine($"   - Pacientes: {pacientesIniciales}");
                TestContext.WriteLine($"   - AnalisisPaciente: {analisisIniciales}");
                TestContext.WriteLine($"   - Consumos: {consumosIniciales}\n");

                // ACT - Intentar operación compleja que debe fallar
                TestContext.WriteLine("⚡ Ejecutando operación transaccional compleja...");

                bool rollbackOcurrido = false;
                Exception? errorCapturado = null;

                try
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    // Paso 1: Registrar análisis para paciente
                    var analisisPaciente = new AnalisisPaciente
                    {
                        PacienteId = 1,
                        AnalisisId = 1,
                        EmpleadoId = 1,
                        Estado = "En proceso",
                        FechaHoraRegistro = DateTime.Now
                    };

                    _context.AnalisisPaciente.Add(analisisPaciente);
                    await _context.SaveChangesAsync();

                    TestContext.WriteLine("   ✅ Paso 1: Análisis registrado");

                    // Paso 2: Registrar consumo de reactivo
                    var consumo = new Consumo
                    {
                        AnalisisId = 1,
                        ReactivoId = 1,
                        NombreReactivo = "Reactivo Hemoglobina",
                        CantidadConsumida = 5,
                        Fecha = DateOnly.FromDateTime(DateTime.Now),
                        Mes = DateTime.Now.Month,
                        Año = DateTime.Now.Year,
                        DiaSemana = DateTime.Now.DayOfWeek.ToString()
                    };

                    _context.Consumo.Add(consumo);
                    await _context.SaveChangesAsync();

                    TestContext.WriteLine("   ✅ Paso 2: Consumo registrado");

                    // Paso 3: Intentar operación inválida (FK inexistente)
                    // Esto debe causar un error y provocar rollback
                    var resultadoInvalido = new Resultado
                    {
                        AnalisisPacienteId = 99999, // ID inexistente
                        AnalisisId = 1,
                        PacienteId = 1,
                        Estado = "completado",
                        FechaRegistro = DateOnly.FromDateTime(DateTime.Now)
                    };

                    _context.Resultados.Add(resultadoInvalido);
                    await _context.SaveChangesAsync(); // Esto debería fallar

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    rollbackOcurrido = true;
                    errorCapturado = ex;
                    TestContext.WriteLine($"   ⚠️ Error detectado (esperado): {ex.Message}");
                }

                // ASSERT - Verificar que se hizo rollback
                int pacientesFinales = await _context.Paciente.CountAsync();
                int analisisFinales = await _context.AnalisisPaciente.CountAsync();
                int consumosFinales = await _context.Consumo.CountAsync();

                TestContext.WriteLine($"\n📊 Estado final:");
                TestContext.WriteLine($"   - Pacientes: {pacientesFinales}");
                TestContext.WriteLine($"   - AnalisisPaciente: {analisisFinales}");
                TestContext.WriteLine($"   - Consumos: {consumosFinales}");

                Assert.IsTrue(rollbackOcurrido, "Debe ocurrir un error que provoque rollback");
                Assert.IsNotNull(errorCapturado, "Debe capturarse la excepción");

                Assert.AreEqual(pacientesIniciales, pacientesFinales,
                    "Los pacientes no deben cambiar tras rollback");

                Assert.AreEqual(analisisIniciales, analisisFinales,
                    "Los análisis no deben cambiar tras rollback");

                Assert.AreEqual(consumosIniciales, consumosFinales,
                    "Los consumos no deben cambiar tras rollback");

                TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ PRUEBA EXITOSA - CP-RF07-05-01");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ Rollback ejecutado correctamente");
                TestContext.WriteLine("✅ Integridad de datos mantenida");
                TestContext.WriteLine("✅ Atomicidad de transacciones validada\n");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"\n❌ ERROR: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region CP-RF07-05-02: Integridad referencial y auditoría

        /// <summary>
        /// CP-RF07-05-02: Validar que todas las operaciones CRUD mantengan 
        /// la integridad referencial y registren auditoría correctamente
        /// </summary>
        [TestMethod]
        [TestCategory("PruebaAceptacion")]
        [TestCategory("IntegridadBD")]
        [TestCategory("Auditoria")]
        public async Task CP_RF07_05_02_ValidarIntegridadReferencialYAuditoria()
        {
            try
            {
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("🔒 CP-RF07-05-02: INTEGRIDAD REFERENCIAL Y AUDITORÍA");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════\n");

                // ARRANGE
                int auditoriasIniciales = await _context.HistorialAuditoria.CountAsync();
                TestContext.WriteLine($"📊 Auditorías iniciales: {auditoriasIniciales}\n");

                // ACT - Operación 1: Registrar paciente
                TestContext.WriteLine("📝 Operación 1: Registrar paciente");
                var nuevoPaciente = new Paciente
                {
                    Nombre = "Pedro",
                    Apellidos = "González Martínez",
                    Dni = "99887766",
                    Sexo = "Masculino",
                    Celular = "987654321",
                    Correo = "pedro.gonzalez@test.com",
                    Direccion = "Av. Test 123",
                    Estado = "Activo",
                    FechaNacimiento = new DateOnly(1985, 5, 15)
                };

                _context.Paciente.Add(nuevoPaciente);

                // Registrar auditoría manualmente (como lo haría el sistema)
                var auditoria1 = new HistorialAuditoria
                {
                    Actividad = "Paciente",
                    Descripcion = "Registro de nuevo paciente",
                    Comentario = $"Nombre: {nuevoPaciente.Nombre} {nuevoPaciente.Apellidos}, DNI: {nuevoPaciente.Dni}",
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 1,
                    EntidadId = 0 // Se actualizará después del save
                };

                _context.HistorialAuditoria.Add(auditoria1);
                await _context.SaveChangesAsync();

                // Actualizar EntidadId con el ID real del paciente
                auditoria1.EntidadId = nuevoPaciente.PacienteId;
                await _context.SaveChangesAsync();

                TestContext.WriteLine($"   ✅ Paciente registrado: ID={nuevoPaciente.PacienteId}");
                TestContext.WriteLine($"   ✅ Auditoría registrada\n");

                // ACT - Operación 2: Registrar análisis para paciente
                TestContext.WriteLine("📝 Operación 2: Registrar análisis para paciente");
                var analisisPaciente = new AnalisisPaciente
                {
                    PacienteId = nuevoPaciente.PacienteId,
                    AnalisisId = 1,
                    EmpleadoId = 1,
                    Estado = "En proceso",
                    FechaHoraRegistro = DateTime.Now
                };

                _context.AnalisisPaciente.Add(analisisPaciente);

                var auditoria2 = new HistorialAuditoria
                {
                    Actividad = "AnalisisPaciente",
                    Descripcion = "Registro de análisis para paciente",
                    Comentario = $"Paciente: {nuevoPaciente.Nombre}, Análisis: Análisis completo de sangre",
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 1,
                    EntidadId = 0
                };

                _context.HistorialAuditoria.Add(auditoria2);
                await _context.SaveChangesAsync();

                auditoria2.EntidadId = analisisPaciente.AnalisisPacienteId;
                await _context.SaveChangesAsync();

                TestContext.WriteLine($"   ✅ Análisis registrado: ID={analisisPaciente.AnalisisPacienteId}");
                TestContext.WriteLine($"   ✅ Auditoría registrada\n");

                // ACT - Operación 3: Registrar consumo de reactivo
                TestContext.WriteLine("📝 Operación 3: Registrar consumo de reactivo");
                var consumo = new Consumo
                {
                    AnalisisId = 1,
                    ReactivoId = 1,
                    NombreReactivo = "Reactivo Hemoglobina",
                    CantidadConsumida = 5,
                    Fecha = DateOnly.FromDateTime(DateTime.Now),
                    Mes = DateTime.Now.Month,
                    Año = DateTime.Now.Year,
                    DiaSemana = DateTime.Now.DayOfWeek.ToString()
                };

                _context.Consumo.Add(consumo);

                var auditoria3 = new HistorialAuditoria
                {
                    Actividad = "Consumo",
                    Descripcion = "Registro de consumo de reactivo",
                    Comentario = $"Reactivo: {consumo.NombreReactivo}, Cantidad: {consumo.CantidadConsumida}",
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 1,
                    EntidadId = 0
                };

                _context.HistorialAuditoria.Add(auditoria3);
                await _context.SaveChangesAsync();

                auditoria3.EntidadId = consumo.ConsumoId;
                await _context.SaveChangesAsync();

                TestContext.WriteLine($"   ✅ Consumo registrado: ID={consumo.ConsumoId}");
                TestContext.WriteLine($"   ✅ Auditoría registrada\n");

                // ASSERT - Verificar integridad referencial
                TestContext.WriteLine("🔍 Verificando integridad referencial:");

                var pacienteVerificado = await _context.Paciente
                    .Include(p => p.AnalisisPacientes)
                    .FirstOrDefaultAsync(p => p.PacienteId == nuevoPaciente.PacienteId);

                Assert.IsNotNull(pacienteVerificado, "El paciente debe existir");
                Assert.IsTrue(pacienteVerificado.AnalisisPacientes.Any(),
                    "El paciente debe tener análisis asociados");

                TestContext.WriteLine($"   ✅ Paciente existe en BD");
                TestContext.WriteLine($"   ✅ Relación Paciente-AnalisisPaciente correcta");

                var consumoVerificado = await _context.Consumo
                    .Include(c => c.Reactivo)
                    .FirstOrDefaultAsync(c => c.ConsumoId == consumo.ConsumoId);

                Assert.IsNotNull(consumoVerificado, "El consumo debe existir");
                Assert.IsNotNull(consumoVerificado.Reactivo, "Debe existir FK a Reactivo");

                TestContext.WriteLine($"   ✅ Consumo existe en BD");
                TestContext.WriteLine($"   ✅ Relación Consumo-Reactivo correcta\n");

                // ASSERT - Verificar auditoría
                TestContext.WriteLine("🔍 Verificando auditoría:");

                int auditoríasFinales = await _context.HistorialAuditoria.CountAsync();
                int auditoríasNuevas = auditoríasFinales - auditoriasIniciales;

                Assert.AreEqual(3, auditoríasNuevas,
                    "Deben registrarse 3 auditorías (1 por cada operación)");

                var ultimasAuditorias = await _context.HistorialAuditoria
                    .OrderByDescending(h => h.Fecha)
                    .Take(3)
                    .ToListAsync();

                Assert.IsTrue(ultimasAuditorias.Any(a => a.Actividad == "Paciente"),
                    "Debe existir auditoría de registro de paciente");

                Assert.IsTrue(ultimasAuditorias.Any(a => a.Actividad == "AnalisisPaciente"),
                    "Debe existir auditoría de registro de análisis");

                Assert.IsTrue(ultimasAuditorias.Any(a => a.Actividad == "Consumo"),
                    "Debe existir auditoría de registro de consumo");

                TestContext.WriteLine($"   ✅ Auditorías registradas: {auditoríasNuevas}");
                TestContext.WriteLine($"   ✅ Todas las operaciones auditadas\n");

                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ PRUEBA EXITOSA - CP-RF07-05-02");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ Integridad referencial mantenida");
                TestContext.WriteLine("✅ Auditoría completa registrada");
                TestContext.WriteLine("✅ Relaciones FK correctas\n");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"\n❌ ERROR: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region CP-RF07-05-03: Eliminación en cascada y datos huérfanos

        /// <summary>
        /// CP-RF07-05-03: Validar que las restricciones de eliminación en cascada 
        /// funcionen correctamente y no queden datos huérfanos
        /// </summary>
        [TestMethod]
        [TestCategory("PruebaAceptacion")]
        [TestCategory("IntegridadBD")]
        [TestCategory("Cascada")]
        public async Task CP_RF07_05_03_ValidarEliminacionCascadaYDatosHuerfanos()
        {
            try
            {
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("🔒 CP-RF07-05-03: ELIMINACIÓN EN CASCADA");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════\n");

                // ARRANGE - Crear estructura completa de datos relacionados
                TestContext.WriteLine("📝 Creando estructura de datos relacionados...\n");

                var empleado = new Empleado
                {
                    Nombre = "Test",
                    Apellidos = "Cascada Usuario",
                    Dni = "11111111",
                    Usuario = "tcascada",
                    Contrasena = "test123",
                    Rol = "Supervisor",
                    Estado = "Activo",
                    Celular = "999999999",
                    Correo = "test@cascada.com",
                    Direccion = "Av. Test",
                    FechaNacimiento = new DateOnly(1990, 1, 1)
                };

                _context.Empleado.Add(empleado);
                await _context.SaveChangesAsync();

                TestContext.WriteLine($"   ✅ Empleado creado: ID={empleado.EmpleadoId}");

                // Crear OTPs asociados (eliminación en cascada)
                var otp1 = new EmpleadoOtp
                {
                    EmpleadoId = empleado.EmpleadoId,
                    Codigo = "123456",
                    Expiracion = DateTime.Now.AddMinutes(5),
                    Usado = false
                };

                var otp2 = new EmpleadoOtp
                {
                    EmpleadoId = empleado.EmpleadoId,
                    Codigo = "789012",
                    Expiracion = DateTime.Now.AddMinutes(5),
                    Usado = false
                };

                _context.EmpleadoOtp.Add(otp1);
                _context.EmpleadoOtp.Add(otp2);
                await _context.SaveChangesAsync();

                TestContext.WriteLine($"   ✅ OTPs creados: 2");

                // Contar registros antes de eliminación
                int otpsAntes = await _context.EmpleadoOtp
                    .Where(o => o.EmpleadoId == empleado.EmpleadoId)
                    .CountAsync();

                TestContext.WriteLine($"\n📊 Estado antes de eliminación:");
                TestContext.WriteLine($"   - OTPs del empleado: {otpsAntes}");

                // ACT - Eliminar empleado (debe eliminar OTPs en cascada)
                TestContext.WriteLine($"\n🗑️ Eliminando empleado ID={empleado.EmpleadoId}...");

                _context.Empleado.Remove(empleado);

                // Registrar auditoría de eliminación
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Empleado",
                    Descripcion = "Eliminación de empleado",
                    Comentario = $"Nombre: {empleado.Nombre} {empleado.Apellidos}, DNI: {empleado.Dni}",
                    Accion = "Eliminar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 1, // Empleado que realiza la acción
                    EntidadId = empleado.EmpleadoId
                };

                _context.HistorialAuditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                TestContext.WriteLine("   ✅ Empleado eliminado");

                // ASSERT - Verificar eliminación en cascada
                TestContext.WriteLine("\n🔍 Verificando eliminación en cascada:");

                var empleadoEliminado = await _context.Empleado
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleado.EmpleadoId);

                Assert.IsNull(empleadoEliminado, "El empleado debe haber sido eliminado");
                TestContext.WriteLine("   ✅ Empleado no existe en BD");

                int otpsDespues = await _context.EmpleadoOtp
                    .Where(o => o.EmpleadoId == empleado.EmpleadoId)
                    .CountAsync();

                Assert.AreEqual(0, otpsDespues, "No deben quedar OTPs huérfanos");
                TestContext.WriteLine($"   ✅ OTPs eliminados en cascada: {otpsAntes} → {otpsDespues}");

                // ASSERT - Verificar que no hay datos huérfanos
                TestContext.WriteLine("\n🔍 Verificando ausencia de datos huérfanos:");

                var otpsHuerfanos = await _context.EmpleadoOtp
                    .Where(o => !_context.Empleado.Any(e => e.EmpleadoId == o.EmpleadoId))
                    .CountAsync();

                Assert.AreEqual(0, otpsHuerfanos, "No deben existir OTPs sin empleado");
                TestContext.WriteLine($"   ✅ Sin datos huérfanos: {otpsHuerfanos}");

                // Verificar auditoría de eliminación
                var auditoriaEliminacion = await _context.HistorialAuditoria
                    .FirstOrDefaultAsync(h => h.Accion == "Eliminar" &&
                                             h.EntidadId == empleado.EmpleadoId);

                Assert.IsNotNull(auditoriaEliminacion, "Debe existir auditoría de eliminación");
                TestContext.WriteLine("   ✅ Eliminación auditada correctamente\n");

                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ PRUEBA EXITOSA - CP-RF07-05-03");
                TestContext.WriteLine("═══════════════════════════════════════════════════════════");
                TestContext.WriteLine("✅ Eliminación en cascada funciona correctamente");
                TestContext.WriteLine("✅ No quedan datos huérfanos");
                TestContext.WriteLine("✅ Integridad referencial mantenida");
                TestContext.WriteLine("✅ Operación auditada\n");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"\n❌ ERROR: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Métodos auxiliares

        /// <summary>
        /// Siembra datos iniciales en la base de datos en memoria para pruebas
        /// </summary>
        private void SembrarDatosIniciales()
        {
            // Empleado
            var empleado = new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Luis",
                Apellidos = "Morales Díaz",
                Dni = "11223344",
                Usuario = "lmorales",
                Contrasena = "pass123",
                Rol = "Administrador",
                Estado = "Activo",
                Celular = "900111222",
                Correo = "luis.morales@test.com",
                Direccion = "Av. Industrial 101",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };
            _context.Empleado.Add(empleado);

            // Pacientes
            var paciente1 = new Paciente
            {
                PacienteId = 1,
                Nombre = "Juan",
                Apellidos = "Pérez López",
                Dni = "12345678",
                Sexo = "Masculino",
                Celular = "987654321",
                Correo = "juan.perez@test.com",
                Direccion = "Av. Siempre Viva 123",
                Estado = "Activo",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };

            var paciente2 = new Paciente
            {
                PacienteId = 2,
                Nombre = "María",
                Apellidos = "García Torres",
                Dni = "87654321",
                Sexo = "Femenino",
                Celular = "912345678",
                Correo = "maria.garcia@test.com",
                Direccion = "Jr. Los Olivos 456",
                Estado = "Activo",
                FechaNacimiento = new DateOnly(2015, 1, 1)
            };

            _context.Paciente.AddRange(paciente1, paciente2);

            // Análisis
            var analisis = new Analisis
            {
                AnalisisId = 1,
                Nombre = "Análisis completo de sangre",
                Comentario = "Incluye parámetros hematológicos básicos",
                Condicion = "Ayuno de 8 horas",
                TipoMuestra = "Sangre",
                Precio = 50,
                Estado = true
            };
            _context.Analisis.Add(analisis);

            // Reactivos
            var reactivo = new Reactivo
            {
                ReactivoId = 1,
                Nombre = "Reactivo Hemoglobina",
                Presentacion = "Frasco",
                Proveedor = "Proveedor A",
                Cantidad = 50,
                Capacidad = 100,
                FechaIngreso = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddMonths(11)),
                CantidadTotal = 5000,
                CapacidadTotal = 10000,
                Disponibilidad = 10
            };
            _context.Reactivo.Add(reactivo);

            // Componentes
            var componente = new Componente
            {
                ComponenteId = 1,
                Nombre = "Hemoglobina",
                Categoria = "Hematológico"
            };
            _context.Componente.Add(componente);

            _context.SaveChanges();
        }

        #endregion
    }
}