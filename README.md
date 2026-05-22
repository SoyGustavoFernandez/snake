# SnakeQuest.API 🐍

Un backend ligero desarrollado con **.NET 10 Minimal APIs** para gestionar la tabla de puntuaciones más altas (Top 10) del juego Snake.

## Características

- **Puntajes Altos (Top 10)**: Mantiene en memoria las 10 mejores puntuaciones ordenadas de mayor a menor.
- **CORS Habilitado**: Configurado para aceptar solicitudes de cualquier origen (`*`), lo que facilita la conexión con cualquier frontend.
- **Preparado para Producción**: Contiene configuración de contenedorización optimizada con Docker y archivo de infraestructura para Railway.

---

## Endpoints de la API

### 1. Obtener Puntuaciones (Top 10)
- **Método**: `GET`
- **Ruta**: `/api/scores`
- **Respuesta**: Lista de las mejores 10 puntuaciones.
- **Ejemplo de respuesta**:
  ```json
  [
    { "name": "Alice", "score": 250 },
    { "name": "Bob", "score": 180 }
  ]
  ```

### 2. Registrar Puntuación
- **Método**: `POST`
- **Ruta**: `/api/scores`
- **Cuerpo (JSON)**:
  ```json
  {
    "name": "NombreJugador",
    "score": 150
  }
  ```
- **Validación**:
  - `name` es requerido y no puede estar vacío.
  - `score` debe ser un número entero no negativo.

---

## Ejecución Local

1. Asegúrate de tener instalado el SDK de .NET 10.
2. Clona el repositorio y navega al directorio del proyecto.
3. Ejecuta la aplicación:
   ```bash
   dotnet run
   ```
4. La API estará disponible en `http://localhost:5000` (o el puerto configurado localmente).

---

## Despliegue en Producción (Docker / Railway)

Este repositorio está preconfigurado para desplegarse automáticamente en plataformas como **Railway** gracias a los siguientes archivos en la raíz:

- **Dockerfile**: Compilación multi-etapa optimizada utilizando las imágenes base oficiales de .NET 10.
- **railway.json**: Configuración básica para la detección del Dockerfile por parte de la plataforma de Railway.

### Construir Imagen Docker Localmente
```bash
docker build -t snakequest-api .
docker run -d -p 8080:8080 snakequest-api
```
