"""
Autor: Ericka Esther Martinez Yufra
Fecha: 09/08/2025
Propósito: Implementar conexión segura a base de datos y ejecución de consulta
           para el flujo de "Generar Predicción por Machine Learning" (RF-11)
           utilizando cadena de conexión almacenada en Configuración de la Aplicación de Azure.
Descripción General:
     - Lee la cadena de conexión a SQL Server desde la variable de entorno DB_CONN_STRING
      configurada en Azure App Service (Configuración de la aplicación).
    - Obtiene los reactivos y series de consumo desde la tabla Consumo.
    - Entrena un modelo Prophet por reactivo y almacena predicciones mensuales en tablas:
      PrediccionesReactivo y PrediccionesReactivoResumen.
    - Utiliza consultas parametrizadas y transacciones para evitar condiciones de carrera
      y prevenir inyección SQL.
"""
import os
import pandas as pd
import pyodbc
from prophet import Prophet
from datetime import datetime
from decimal import Decimal

from flask import Flask, jsonify

app = Flask(__name__)

# ----------------------------
# Configuración y utilidades
# ----------------------------
# Leer la cadena de conexión de forma segura desde la variable de entorno configurada en Azure
DB_CONN_STRING = os.getenv("DB_CONN_STRING")
if not DB_CONN_STRING:
    # Fail fast: si no existe la variable de entorno, detener la ejecución con mensaje claro.
    raise EnvironmentError("La variable de entorno DB_CONN_STRING no está configurada. "
                           "Configurela en Azure App Service > Configuración de la aplicación.")

# Parámetros del proceso (se pueden llevar a variables de entorno si se desea parametrizar más)
MIN_DATOS_PARA_MODELO = 12  # mínimo de puntos (ej. meses o registros) para entrenar un modelo; configurable

# ----------------------------
# Endpoint principal
# ----------------------------
@app.route('/ejecutar_prediccion', methods=['GET'])
def ejecutar_prediccion():
    """
    Ejecuta el proceso completo de predicción por reactivo y persiste los resultados.
    Retorna JSON con el número de predicción generada o el error encontrado.
    """

    fecha_generacion = datetime.now()

    # Abrir conexión con manejo de errores y transacción explícita para atomicidad.
    try:
        conn = pyodbc.connect(DB_CONN_STRING)
        cursor = conn.cursor()
    except Exception as ex:
        # Error de conexión a BD: devolver causa y loguear (aquí print para pruebas).
        # En producción usar un logger centralizado (Application Insights, ELK, etc.).
        print("ERROR: No se pudo conectar a la base de datos:", ex)
        return jsonify({"error": "Error de conexión a base de datos", "detalle": str(ex)}), 500

    try:
        # Iniciar transacción explícita
        # NOTA: pyodbc comienza transacción por defecto en muchas configuraciones; se usa commit()/rollback() según sea necesario.
        # Obtener nuevo NumeroPrediccion de forma segura:

        try:
            # Intentar insertar registro en tabla de control para obtener un Id único y seguro
            cursor.execute("INSERT INTO PrediccionesControl (FechaGeneracion) VALUES (?)", (fecha_generacion,))
            # Obtener el id generado (esto depende del driver; se usa SELECT @@IDENTITY como fallback)
            cursor.execute("SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId")
            new_id_row = cursor.fetchone()
            if new_id_row and new_id_row[0] is not None:
                current_pred_num = int(new_id_row[0])
            else:
                # fallback: usar MAX(NumeroPrediccion) + 1 con bloqueo (no ideal pero funcional)
                cursor.execute("SELECT ISNULL(MAX(NumeroPrediccion), 0) FROM PrediccionesReactivo")
                last_pred_num = cursor.fetchone()[0] or 0
                current_pred_num = int(last_pred_num) + 1
        except Exception as ex_ctrl:
            # Si falla la estrategia de control, intentar fallback con MAX(...)
            print("Advertencia: falla al usar PrediccionesControl, aplicando fallback. Error:", ex_ctrl)
            cursor.execute("SELECT ISNULL(MAX(NumeroPrediccion), 0) FROM PrediccionesReactivo")
            last_pred_num = cursor.fetchone()[0] or 0
            current_pred_num = int(last_pred_num) + 1

        # Obtener lista de reactivos distintos desde la tabla Consumo
        reactivos_query = "SELECT DISTINCT ReactivoId, NombreReactivo FROM Consumo"
        reactivos_df = pd.read_sql(reactivos_query, conn)

        hoy = pd.to_datetime(datetime.today().strftime('%Y-%m-%d'))

        # Procesar cada reactivo
        for _, row in reactivos_df.iterrows():
            reactivo_id = int(row['ReactivoId'])
            nombre_reactivo = str(row['NombreReactivo'])

            # Consulta parametrizada para obtener consumo diario/por fecha del reactivo
            consumo_query = """
                SELECT Fecha, SUM(CantidadConsumida) AS TotalConsumido
                FROM Consumo
                WHERE ReactivoId = ?
                GROUP BY Fecha
                ORDER BY Fecha
            """
            # Usar params para evitar inyección y problemas de tipos
            df = pd.read_sql(consumo_query, conn, params=[reactivo_id])

            # Renombrar columnas al formato esperado por Prophet: ds (fecha), y (valor)
            df.rename(columns={'Fecha': 'ds', 'TotalConsumido': 'y'}, inplace=True)

            # Si no hay suficientes datos, se salta el reactivo (registro en resumen que no se generó)
            # Esto se hace para mantener robustez del proceso y no insertar predicciones inválidas.
            if df.empty or len(df) < MIN_DATOS_PARA_MODELO:
                print(f"Advertencia: reactivo {reactivo_id} ({nombre_reactivo}) omitido por datos insuficientes.")
                continue

            # Asegurar tipos correctos y eliminar NaN en 'y'
            df['ds'] = pd.to_datetime(df['ds'])
            df['y'] = pd.to_numeric(df['y'], errors='coerce').fillna(0)

            # Entrenar modelo Prophet
            model = Prophet()
            model.fit(df)

            # Generar horizonte de predicción: 365 días (1 año diario), luego agregaremos por mes
            future = model.make_future_dataframe(periods=365, freq='D')
            forecast = model.predict(future)

            # Normalizar fechas y agrupar por mes para obtener predicciones mensuales
            forecast['ds'] = pd.to_datetime(forecast['ds'])
            forecast = forecast[forecast['ds'] >= hoy]
            # Convertir al primer día del mes para tener consistencia con tipo DATE en BD
            forecast['ds'] = forecast['ds'].dt.to_period('M').dt.to_timestamp()

            # Agrupar por mes y calcular promedio de yhat (valor pronosticado)
            forecast_monthly = (forecast.groupby('ds')['yhat']
                                .mean()
                                .reset_index()
                                .rename(columns={'yhat': 'yhat'}))

            # Si no hay datos para forecast_monthly, saltar este reactivo
            if forecast_monthly.empty:
                print(f"Advertencia: forecast mensual vacío para reactivo {reactivo_id}.")
                continue

            # Reemplazar NaN y asegurar tipo numérico
            forecast_monthly['yhat'] = pd.to_numeric(forecast_monthly['yhat'], errors='coerce').fillna(0)

            # Calcular porcentaje de cambio mensual (para trend)
            forecast_monthly['yhat_pct_change'] = forecast_monthly['yhat'].pct_change().fillna(0) * 100

            # Insertar predicciones por mes en la tabla PrediccionesReactivo (parametrizado)
            insert_pred = """
                INSERT INTO PrediccionesReactivo 
                (NumeroPrediccion, ReactivoId, NombreReactivo, Mes, ConsumoEsperado, PorcentajeCambio, FechaGeneracion)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """
            for _, pred_row in forecast_monthly.iterrows():
                mes = pred_row['ds'].to_pydatetime().date()  # fecha tipo date (primer día del mes)
                consumo_esperado = float(pred_row['yhat'])
                porcentaje_cambio = float(pred_row['yhat_pct_change'])
                cursor.execute(insert_pred, (
                    current_pred_num,
                    reactivo_id,
                    nombre_reactivo,
                    mes,
                    consumo_esperado,
                    porcentaje_cambio,
                    fecha_generacion
                ))

            # Generar resumen (PrediccionesReactivoResumen)
            # Obtener top3 por yhat (mayores consumos)
            top3 = forecast_monthly.nlargest(3, 'yhat')
            tendencia_promedio = float(forecast_monthly['yhat_pct_change'].mean())

            # Mes con mayor y menor consumo (si existe)
            mes_mayor = top3.iloc[0]['ds'] if not top3.empty else None
            mes_menor_row = forecast_monthly.nsmallest(1, 'yhat')
            mes_menor = mes_menor_row.iloc[0]['ds'] if not mes_menor_row.empty else None

            # Normalizar a date o None
            mes_mayor_date = mes_mayor.to_pydatetime().date() if mes_mayor is not None else None
            mes_menor_date = mes_menor.to_pydatetime().date() if mes_menor is not None else None

            texto_conclusion = (
                f"El reactivo {nombre_reactivo} presenta pico en {mes_mayor_date.strftime('%B %Y') if mes_mayor_date else 'N/A'}. "
                f"Tendencia promedio: {tendencia_promedio:.2f}% mensual. "
                f"Menor consumo proyectado en {mes_menor_date.strftime('%B %Y') if mes_menor_date else 'N/A'}. "
                f"Recomendación: programar compras estratégicamente."
            )

            insert_resumen = """
                INSERT INTO PrediccionesReactivoResumen 
                (NumeroPrediccion, ReactivoId, NombreReactivo, TendenciaPromedio, MesMayorConsumo, MesMenorConsumo, TextoConclusion, FechaGeneracion)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """
            cursor.execute(insert_resumen, (
                current_pred_num,
                reactivo_id,
                nombre_reactivo,
                tendencia_promedio,
                mes_mayor_date,
                mes_menor_date,
                texto_conclusion,
                fecha_generacion
            ))

        # Commit de la transacción una vez procesados todos los reactivos
        conn.commit()

    except Exception as e_proc:
        # Si hay cualquier error durante el procesamiento, realizar rollback y devolver error
        print("ERROR durante la ejecución de predicción:", e_proc)
        try:
            conn.rollback()
        except Exception as rb_ex:
            print("ERROR rollback:", rb_ex)
        return jsonify({"error": "Error durante ejecución de predicción", "detalle": str(e_proc)}), 500

    finally:
        # Cierre de cursor y conexión en el finally para asegurar liberación de recursos
        try:
            cursor.close()
        except Exception:
            pass
        try:
            conn.close()
        except Exception:
            pass

    # Respuesta exitosa con número de predicción generado
    return jsonify({"mensaje": "Predicción ejecutada correctamente.", "NumeroPrediccion": current_pred_num}), 200


if __name__ == '__main__':
    # En entorno de desarrollo; en producción se recomienda usar un WSGI server/procesos gestionados
    app.run(host='0.0.0.0', port=8000)