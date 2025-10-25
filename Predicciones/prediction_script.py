# archivo: app.py

from flask import Flask
import pandas as pd
import pyodbc
from prophet import Prophet
from datetime import datetime

app = Flask(__name__)

@app.route('/')
def home():
    return "✅ API de predicciones funcionando correctamente."

@app.route('/ejecutar_prediccion', methods=['GET'])
def ejecutar_prediccion():
    # Conexión a SQL Server en Azure
    conn = pyodbc.connect(
        'DRIVER={ODBC Driver 17 for SQL Server};'
        'SERVER=proyectoweb1.database.windows.net,1433;'
        'DATABASE=dblaboratorio;'
        'UID=useradmin;'
        'PWD=Martinez0110;'
        'Encrypt=yes;'
        'TrustServerCertificate=no;'
        'Connection Timeout=30;'
    )
    cursor = conn.cursor()

    # Obtener el último número de predicción y aumentar en 1
    cursor.execute("SELECT ISNULL(MAX(NumeroPrediccion), 0) FROM PrediccionesReactivo")
    last_pred_num = cursor.fetchone()[0]
    current_pred_num = last_pred_num + 1

    # Obtener todos los reactivos distintos
    reactivos_query = "SELECT DISTINCT ReactivoId, NombreReactivo FROM Consumo"
    reactivos = pd.read_sql(reactivos_query, conn)

    hoy = pd.to_datetime(datetime.today().strftime('%Y-%m-%d'))

    for _, row in reactivos.iterrows():
        reactivo_id = row['ReactivoId']
        nombre_reactivo = row['NombreReactivo']

        consumo_query = f"""
            SELECT Fecha, SUM(CantidadConsumida) AS TotalConsumido
            FROM Consumo
            WHERE ReactivoId = {reactivo_id}
            GROUP BY Fecha
            ORDER BY Fecha
        """
        df = pd.read_sql(consumo_query, conn)
        df.rename(columns={'Fecha': 'ds', 'TotalConsumido': 'y'}, inplace=True)

        if df.empty or len(df) < 2:
            continue  # Saltar si no hay datos suficientes

        model = Prophet()
        model.fit(df)

        future = model.make_future_dataframe(periods=365, freq='D')
        forecast = model.predict(future)

        forecast['ds'] = pd.to_datetime(forecast['ds'])
        forecast = forecast[forecast['ds'] >= hoy]
        forecast['ds'] = forecast['ds'].dt.to_period('M').dt.to_timestamp()

        forecast_monthly = forecast.groupby('ds')['yhat'].mean().reset_index()
        forecast_monthly['yhat_pct_change'] = forecast_monthly['yhat'].pct_change().fillna(0) * 100

        for _, pred_row in forecast_monthly.iterrows():
            insert_pred = """
                INSERT INTO PrediccionesReactivo 
                (NumeroPrediccion, ReactivoId, NombreReactivo, Mes, ConsumoEsperado, PorcentajeCambio)
                VALUES (?, ?, ?, ?, ?, ?)
            """
            cursor.execute(insert_pred, (
                current_pred_num,
                reactivo_id,
                nombre_reactivo,
                pred_row['ds'],
                pred_row['yhat'],
                pred_row['yhat_pct_change']
            ))

        # Resumen
        top3 = forecast_monthly.nlargest(3, 'yhat').sort_values('ds')
        tendencia_promedio = forecast_monthly['yhat_pct_change'].mean()
        mes_mayor = top3.iloc[0]['ds']
        mes_menor = forecast_monthly.nsmallest(1, 'yhat').iloc[0]['ds']

        texto_conclusion = (
            f"El reactivo {nombre_reactivo} tiene un pico en {mes_mayor.strftime('%B %Y')}. "
            f"Tendencia promedio: {tendencia_promedio:.2f}% mensual. "
            f"Menor consumo proyectado en {mes_menor.strftime('%B %Y')}. "
            f"Recomendación: programar compras estratégicamente."
        )

        insert_resumen = """
            INSERT INTO PrediccionesReactivoResumen 
            (NumeroPrediccion, ReactivoId, NombreReactivo, TendenciaPromedio, MesMayorConsumo, MesMenorConsumo, TextoConclusion)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """
        cursor.execute(insert_resumen, (
            current_pred_num,
            reactivo_id,
            nombre_reactivo,
            tendencia_promedio,
            mes_mayor,
            mes_menor,
            texto_conclusion
        ))

    conn.commit()
    cursor.close()
    conn.close()

    return f"Predicción ejecutada correctamente. NumeroPrediccion: {current_pred_num}"

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8000)
