# 🔐 Password Guard

**Password Guard** es una aplicación web para **generar contraseñas seguras** y **analizar la seguridad de las que ya usas**. Combina cálculos en el navegador, una API propia en ASP.NET Core, la comprobación de filtraciones de [Have I Been Pwned](https://haveibeenpwned.com/Passwords), consejos personalizados generados con IA (Google Gemini) y un microservicio en Python que estima **cuántos intentos necesitaría un atacante real** para adivinarla.

Todo el proyecto se levanta con un solo comando gracias a **Docker Compose**.

> 🚧 **Proyecto en desarrollo.** Es un proyecto personal de aprendizaje de desarrollo *full stack* con Vue 3, .NET y Python. Consulta [Limitaciones conocidas](#-limitaciones-conocidas) y la [Hoja de ruta](#-hoja-de-ruta).

---

## 📑 Índice

- [Funcionalidades](#-funcionalidades)
- [Privacidad: qué datos salen de tu equipo](#-privacidad-qué-datos-salen-de-tu-equipo)
- [Tecnologías](#-tecnologías)
- [Arquitectura](#-arquitectura)
- [Estructura del repositorio](#-estructura-del-repositorio)
- [Requisitos](#-requisitos)
- [Instalación y puesta en marcha](#-instalación-y-puesta-en-marcha)
- [Configuración](#-configuración)
- [API](#-api)
- [Cómo se evalúa una contraseña](#-cómo-se-evalúa-una-contraseña)
- [Microservicio de estimación en Python](#-microservicio-de-estimación-en-python)
- [Pruebas](#-pruebas)
- [Flujo de trabajo con Git](#-flujo-de-trabajo-con-git)
- [Limitaciones conocidas](#-limitaciones-conocidas)
- [Hoja de ruta](#-hoja-de-ruta)
- [Autor](#-autor)

---

## ✨ Funcionalidades

### Generador de contraseñas

- Longitud configurable de **8 a 128 caracteres** (16 por defecto), con control deslizante, botones `−`/`+` y campo numérico.
- **Filtros por tipo de carácter**: minúsculas, mayúsculas, números y símbolos (`! @ # $ % ^ & * ( ) - +`). Siempre debe quedar al menos uno activo.
- Coloreado de cada carácter según su tipo para leer la contraseña con más facilidad.
- Cálculo de la **entropía** en bits según los tipos activos.
- **Nivel de seguridad** calculado por la API (con el cálculo local como respaldo si la API no responde).
- Botón para **copiar al portapapeles** y regeneración rápida con la **barra espaciadora**.

### Analizador de contraseñas

Escribe o pega una contraseña y obtendrás:

- **Fortaleza**: nivel de 1 a 4 (*Débil*, *Media*, *Fuerte*, *Muy fuerte* o *Insegura*), entropía, número de combinaciones posibles y **tiempo estimado para descifrarla**.
- **Comparativa de ataques**: diccionario, patrones habituales y fuerza bruta, ordenados del más rápido al más lento, con barras en escala logarítmica y el ataque ganador destacado. Los patrones habituales los estima un **modelo de Markov entrenado con un millón de contraseñas filtradas**.
- **Desglose de la composición**: longitud, mayúsculas y minúsculas combinadas, números y símbolos.
- **Contraseñas comunes**: comprobación contra una lista de las **10 000 contraseñas más usadas**.
- **Filtraciones**: cuántas veces aparece la contraseña en filtraciones públicas según **Have I Been Pwned**, usando *k-anonymity*.
- **Patrones y consejos con IA**: la estructura anónima de la contraseña, que detecta **palabras, números, símbolos, años, fechas y repeticiones** (por ejemplo, `Palabra · 9 + Año · 4 + Símbolo · 1`), y tres consejos personalizados generados por **Gemini**. Si la IA no está disponible, se muestran consejos generales.
- **Pruebas rápidas** con contraseñas de ejemplo y botón para mostrar u ocultar la contraseña.

El tiempo mostrado es el del **ataque que antes funcionaría**:

- Si la contraseña es **común** o aparece en **alguna filtración**, se marca como **Insegura** y se descifraría **al instante** con un ataque de diccionario, aunque cumpla el resto de requisitos.
- Si no, se muestra el **menor** de dos tiempos: el de fuerza bruta y el del modelo de Markov.

---

## 🛡️ Privacidad: qué datos salen de tu equipo

Password Guard **no guarda ninguna contraseña**: no hay base de datos, ni cookies, ni almacenamiento local. Aun así, conviene saber exactamente qué viaja por la red:

| Destino | Qué se envía | Para qué |
|---|---|---|
| **API de Password Guard** (tu propio servidor) | La contraseña completa | Calcular su nivel, comprobar si es común y extraer su estructura anónima |
| **Estimador en Python** (mismo equipo o red interna de Docker) | La contraseña completa, **solo desde el backend** | Estimar el número de intentos de un ataque realista |
| **Have I Been Pwned** | Solo los **5 primeros caracteres del hash SHA-1**, calculado en el navegador | Comprobar filtraciones sin revelar la contraseña (*k-anonymity*) |
| **Google Gemini** | Solo la **estructura anónima**, por ejemplo `palabra(9) año(4) simbolo(1) longitud: 14` | Generar consejos personalizados |

- La comprobación de filtraciones funciona así: el navegador calcula el SHA-1 de la contraseña, envía solo sus 5 primeros caracteres y recibe cientos de hashes que empiezan igual. La coincidencia se busca **en el navegador**, así que Have I Been Pwned nunca conoce la contraseña.
- A Gemini **nunca** le llega la contraseña ni ningún fragmento de ella: solo los tipos de cada bloque y sus longitudes.
- El estimador **no es accesible desde fuera**:
  - en local escucha solo en `127.0.0.1`;
  - con Docker no publica ningún puerto, así que solo lo alcanza el backend por la red interna.
- Los registros (*logs*) del backend y del estimador no incluyen la contraseña ni fragmentos de ella.

> ⚠️ Al usar la capa gratuita de Gemini, Google puede utilizar el contenido enviado para mejorar sus productos. Por eso solo se envía la estructura anónima. Revisa las condiciones actuales en [Google AI Studio](https://aistudio.google.com/).

---

## 🧰 Tecnologías

**Frontend**

- [Vue 3](https://vuejs.org/) (Composition API con `<script setup>`) + [TypeScript](https://www.typescriptlang.org/)
- [Vite](https://vite.dev/) como servidor de desarrollo y empaquetador (incluye un *proxy* hacia la API)
- CSS con ámbito por componente (`scoped`) y variables de tema con soporte de **modo oscuro**
- [Web Crypto API](https://developer.mozilla.org/es/docs/Web/API/Web_Crypto_API) para el hash SHA-1

**Backend**

- [.NET 10](https://dotnet.microsoft.com/) y **ASP.NET Core Web API** con controladores
- Inyección de dependencias (`AddScoped`, `AddSingleton`, `AddHttpClient` con clientes tipados)
- [DotNetEnv](https://www.nuget.org/packages/DotNetEnv) para cargar la configuración desde un archivo `.env`
- `HttpClient` para llamar a la API REST de **Google Gemini** (salida JSON estructurada) y al **estimador en Python**
- OpenAPI (documento en `/openapi/v1.json` en desarrollo)

**Microservicio de estimación**

- [Python 3.14](https://www.python.org/) con entorno virtual (`venv`)
- [FastAPI](https://fastapi.tiangolo.com/) y [Uvicorn](https://www.uvicorn.org/) para exponer el servicio
- Modelo de Markov de caracteres y estimación Monte Carlo del número de intentos, implementados solo con la biblioteca estándar

**Contenedores**

- [Docker](https://www.docker.com/) con *builds* multietapa para el backend y el frontend
- **Docker Compose** para levantar los tres servicios
- [nginx](https://nginx.org/) para servir el frontend compilado y reenviar `/api` al backend

**Pruebas**

- [pytest](https://docs.pytest.org/) y el `TestClient` de FastAPI para el microservicio
- [xUnit](https://xunit.net/), [NSubstitute](https://nsubstitute.github.io/) y `Microsoft.AspNetCore.Mvc.Testing` para el backend, con pruebas unitarias y de integración

**Servicios externos**

- [Have I Been Pwned — Pwned Passwords](https://haveibeenpwned.com/API/v3#PwnedPasswords) (gratuito, sin clave)
- [Google Gemini API](https://ai.google.dev/) (requiere clave)

---

## 🏗️ Arquitectura

```
┌──────────────────────────── Navegador ────────────────────────────┐
│  Vue 3                                                            │
│  · Generador y analizador                                         │
│  · Entropía y tiempo por fuerza bruta (cálculo local)             │
│  · SHA-1 de la contraseña ──────────────► Have I Been Pwned       │
│                                          (solo 5 caracteres)      │
└───────────────┬───────────────────────────────────────────────────┘
                │  /api/*  (proxy de Vite en desarrollo · nginx en Docker)
                ▼
┌──────────────────────── API ASP.NET Core ─────────────────────────┐
│  nivelSeguridadController                                         │
│   ├─ comprobarContrasenaService  → requisitos y puntuación        │
│   ├─ contrasenasComunesService   → lista de 10 000 contraseñas    │
│   ├─ Contrasena                  → estructura anónima             │
│   ├─ IConsejosIAService (GeminiService) ────────► Google Gemini   │
│   │                               (solo la estructura anónima)    │
│   └─ IEstimadorService (EstimadorService)                         │
└───────────────┬───────────────────────────────────────────────────┘
                │  HTTP interno (timeout de 3 s; si falla, se ignora)
                ▼
┌──────────────────── Estimador en Python · FastAPI ────────────────┐
│  · Modelo de Markov entrenado con contraseñas filtradas           │
│  · Estimación Monte Carlo del número de intentos                  │
│  · Nunca accesible desde fuera                                    │
└───────────────────────────────────────────────────────────────────┘
```

| Servicio | En desarrollo local | Con Docker Compose |
|---|---|---|
| Frontend | `http://localhost:5173` (Vite) | `http://localhost:8080` (nginx) |
| API | `http://localhost:5225` | `http://backend:8080` (solo red interna) |
| Estimador | `http://127.0.0.1:8000` | `http://estimador:8000` (solo red interna) |

Tanto la IA como el estimador son **opcionales**: el backend los usa a través de interfaces y, si no responden, devuelve `null` en ese campo y el resto de la aplicación sigue funcionando.

---

## 📂 Estructura del repositorio

```
passwordGuard/
├── docker-compose.yml                        # Levanta los tres servicios
├── backend/
│   ├── PasswordGuard.slnx                    # Solución de .NET
│   ├── src/PasswordGuard.Api/
│   │   ├── Controllers/
│   │   │   └── nivelSeguridadController.cs   # Endpoints de la API
│   │   ├── Service/
│   │   │   ├── comprobarContrasenaService.cs # Requisitos, puntuación y nivel
│   │   │   ├── contrasenasComunesService.cs  # Lista de contraseñas comunes
│   │   │   ├── IConsejosIAService.cs         # Contrato del servicio de IA
│   │   │   ├── GeminiService.cs              # Implementación con Gemini
│   │   │   ├── IEstimadorService.cs          # Contrato del estimador
│   │   │   └── EstimadorService.cs           # Cliente HTTP del microservicio de Python
│   │   ├── models/
│   │   │   ├── Contrasena.cs                 # Estructura anónima (palabras, números, años, repeticiones…)
│   │   │   ├── ConsejosIA.cs                 # Consejos devueltos por la IA
│   │   │   └── EstimacionAtaque.cs           # Resultado del estimador
│   │   ├── data/
│   │   │   └── 10k_most_common.txt           # 10 000 contraseñas más usadas
│   │   ├── Properties/launchSettings.json    # Puertos de desarrollo
│   │   ├── appsettings.json                  # Configuración (URL del estimador)
│   │   ├── Program.cs                        # Arranque y registro de servicios
│   │   ├── Dockerfile
│   │   └── .env                              # Clave de Gemini (no se sube a Git)
│   └── tests/PasswordGuard.Api.Tests/        # Pruebas con xUnit
│       ├── Helpers/ManejadorHttpFalso.cs     # Sustituye a la red en las pruebas de clientes HTTP
│       ├── ContrasenaTests.cs                # Estructura anónima
│       ├── ContrasenasComunesServiceTests.cs
│       ├── ComprobarContrasenaServiceTests.cs
│       ├── GeminiServiceTests.cs
│       ├── EstimadorServiceTests.cs
│       └── NivelSeguridadControllerTests.cs  # Integración: la API completa en memoria
├── servicios/
│   └── estimador/                            # Microservicio en Python
│       ├── ngram.py                          # Modelo de Markov y estimación Monte Carlo
│       ├── entrenar.py                       # Entrenamiento y evaluación offline del modelo
│       ├── main.py                           # API con FastAPI
│       ├── requirements.txt                  # Dependencias de Python
│       ├── Dockerfile
│       ├── tests/                            # Pruebas con pytest
│       ├── datos/                            # Listas de entrenamiento (no se suben a Git)
│       └── modelo/                           # Modelo entrenado (no se sube a Git)
└── frontend/
    ├── src/
    │   ├── components/
    │   │   ├── header.vue                    # Cabecera y navegación
    │   │   ├── generador.vue                 # Generador de contraseñas
    │   │   └── analizador.vue                # Analizador de contraseñas
    │   ├── assets/tema.css                   # Paleta de colores (claro y oscuro)
    │   ├── filtraciones.js                   # Consulta a Have I Been Pwned
    │   ├── App.vue                           # Cambio entre Generar y Analizar
    │   └── main.ts
    ├── vite.config.ts                        # Proxy /api → backend (desarrollo)
    ├── nginx.conf                            # Proxy /api → backend (Docker)
    └── Dockerfile
```

---

## ✅ Requisitos

**Con Docker (recomendado para probar el proyecto)**

| Herramienta | Notas |
|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | En Windows necesita **WSL 2** (`wsl --install`) y la virtualización activada |
| [Python](https://www.python.org/downloads/) 3.14 | Solo para entrenar el modelo una vez |
| Clave de API de Gemini | Opcional, solo para los consejos con IA ([Google AI Studio](https://aistudio.google.com/)) |

**Sin Docker (desarrollo)**

| Herramienta | Versión |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10 |
| [Node.js](https://nodejs.org/) | 22.18 o superior (o 24.12 o superior) |
| [Python](https://www.python.org/downloads/) | 3.14 |
| Clave de API de Gemini | Opcional |

---

## 🚀 Instalación y puesta en marcha

### 1. Clonar el repositorio

```bash
git clone https://github.com/JL-garrote/passwordGuard.git
cd passwordGuard
```

### 2. Configurar la clave de Gemini (opcional)

Crea el archivo `backend/src/PasswordGuard.Api/.env` (consulta [Configuración](#-configuración)):

```env
Gemini__ApiKey=tu-clave-de-google-ai-studio
```

> Sin clave, la aplicación funciona igualmente: el endpoint de consejos devolverá un error y el analizador mostrará consejos generales. Con Docker Compose el archivo `.env` **debe existir** (aunque esté vacío), porque se carga con `env_file`.

### 3. Entrenar el modelo del estimador (una sola vez)

1. Descarga [`10-million-password-list-top-1000000.txt`](https://github.com/danielmiessler/SecLists/tree/master/Passwords/Common-Credentials) de SecLists.
2. Guárdalo en `servicios/estimador/datos/` con el nombre `10_million_password_list_top_1000000.txt`.
3. Ejecuta:

```powershell
cd servicios/estimador
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe entrenar.py
```

El entrenamiento tarda unos minutos y guarda el modelo en `servicios/estimador/modelo/modelo.pkl.gz`. Consulta las opciones en [Entrenamiento](#entrenamiento).

### 4A. Arrancar todo con Docker Compose

Desde la raíz del repositorio:

```bash
docker compose up --build
```

Abre **http://localhost:8080**. La primera vez tarda unos minutos en descargar las imágenes; después aprovecha la caché.

| Comando | Qué hace |
|---|---|
| `docker compose up -d --build` | Arranca en segundo plano |
| `docker compose ps` | Muestra el estado (el estimador debe aparecer como `healthy`) |
| `docker compose logs -f backend` | Muestra los registros de un servicio |
| `docker compose up --build frontend` | Reconstruye un solo servicio tras cambiar su código |
| `docker compose down` | Para y elimina los contenedores |

El modelo **no se incluye en la imagen**: se monta como volumen de solo lectura desde `servicios/estimador/modelo/`.

### 4B. Arrancar cada servicio por separado (desarrollo)

Cada servicio en **su propia terminal**:

```powershell
# 1. Estimador
cd servicios/estimador
.\.venv\Scripts\python.exe -m uvicorn main:app --host 127.0.0.1 --port 8000

# 2. Backend
cd backend/src/PasswordGuard.Api
dotnet run

# 3. Frontend
cd frontend
npm install        # solo la primera vez
npm run dev
```

Abre la dirección que indique Vite, normalmente `http://localhost:5173`. Para detener un servicio, pulsa `Ctrl+C` en su terminal.

Así tienes recarga en caliente del frontend y no hace falta reconstruir imágenes con cada cambio.

### Otros comandos útiles

| Comando | Dónde | Qué hace |
|---|---|---|
| `dotnet build` | `backend/` | Compila la solución |
| `dotnet test` | `backend/` | Ejecuta las pruebas del backend |
| `npm run build` | `frontend/` | Comprueba los tipos y genera la versión de producción en `dist/` |
| `python -m pytest -q` | `servicios/estimador/` | Ejecuta las pruebas del estimador |

---

## ⚙️ Configuración

El backend lee la configuración con el sistema estándar de ASP.NET Core. Las variables del archivo `.env` se cargan al arrancar gracias a **DotNetEnv**, y en Docker llegan como variables de entorno. El doble guion bajo (`__`) equivale a `:` en la configuración, así que `Gemini__ApiKey` se lee como `Gemini:ApiKey`.

| Variable | Obligatoria | Valor por defecto | Descripción |
|---|---|---|---|
| `Gemini__ApiKey` | Solo para los consejos con IA | — | Clave de la API de Gemini |
| `Gemini__Model` | No | `gemini-3.8-flash` | Modelo de Gemini (por ejemplo, `gemini-3.5-flash-lite` para reducir costes) |
| `Estimador__Url` | No | `http://127.0.0.1:8000` (en `appsettings.json`) | Dirección del microservicio de Python. Docker Compose la cambia a `http://estimador:8000` |

> 🔒 **El archivo `.env` nunca debe subirse a Git** ni entrar en una imagen de Docker. Ya está incluido en `backend/.gitignore` y en el `.dockerignore` del backend. Si una clave llega a publicarse por error, **revócala en Google AI Studio y genera otra**: borrarla en un commit posterior no la elimina del historial.

En desarrollo, el frontend llama a rutas relativas (`/api/...`) y Vite las reenvía a `http://localhost:5225` mediante el *proxy* de `vite.config.ts`. En Docker hace lo mismo nginx con `nginx.conf`. En ambos casos no hace falta configurar CORS.

---

## 📡 API

Ruta base: `/api/nivelSeguridad`. Todas las peticiones usan `POST` para que la contraseña viaje en el **cuerpo** de la petición y no en la URL, que acaba guardada en registros e historiales.

Si falta el campo `contrasena`, la API responde automáticamente con un **400 Bad Request**.

### `POST /api/nivelSeguridad/comprobar`

Comprueba si la contraseña cumple los requisitos elegidos y calcula su nivel de seguridad.

**Petición**

```json
{
  "contrasena": "Barcelona2023!",
  "usarMayusculas": true,
  "usarMinusculas": true,
  "usarNumeros": true,
  "usarSimbolos": true
}
```

Cada campo `usarX` indica si ese tipo de carácter es **obligatorio**. La longitud mínima de 8 caracteres se exige siempre.

**Respuesta**

```json
{
  "valida": true,
  "nivelSeguridad": "Fuerte"
}
```

### `POST /api/nivelSeguridad/evaluar`

Indica si la contraseña está entre las 10 000 más usadas (sin distinguir mayúsculas y minúsculas).

**Petición**

```json
{ "contrasena": "Primetime21" }
```

**Respuesta**

```json
{ "esComun": true }
```

### `POST /api/nivelSeguridad/consejos`

Extrae la estructura anónima de la contraseña y pide a Gemini tres consejos para mejorarla.

**Petición**

```json
{ "contrasena": "Barcelona2023!" }
```

**Respuesta**

```json
{
  "patrones": "palabra(9) año(4) simbolo(1) longitud: 14",
  "consejos": [
    { "titulo": "…", "texto": "…" },
    { "titulo": "…", "texto": "…" },
    { "titulo": "…", "texto": "…" }
  ]
}
```

Si Gemini no responde (error de red, límite de peticiones, clave incorrecta, más de 15 segundos de espera…), `consejos` llega como `null` y el backend deja un aviso en el registro con el código de error. Si la clave no está configurada, este endpoint devuelve un error 500, pero **el resto de endpoints siguen funcionando**.

### `POST /api/nivelSeguridad/estimar`

Pide al microservicio de Python la estimación de un ataque realista.

**Petición**

```json
{ "contrasena": "maria1234" }
```

**Respuesta**

```json
{
  "estimacion": {
    "longitud": 9,
    "bits": 21.3,
    "intentosEstimados": 2600000,
    "segundos": 0.00026,
    "categoria": "debil"
  }
}
```

Si el estimador no está arrancado, tarda más de 3 segundos o responde con error, `estimacion` llega como `null` y el backend deja un aviso en el registro. *Los valores del ejemplo son ilustrativos.*

---

## 🧮 Cómo se evalúa una contraseña

### Puntuación del backend (0 a 7 puntos)

| Criterio | Puntos |
|---|---|
| Longitud ≥ 8 | +1 |
| Longitud ≥ 12 | +1 |
| Longitud ≥ 16 | +1 |
| Contiene mayúsculas | +1 |
| Contiene minúsculas | +1 |
| Contiene números | +1 |
| Contiene símbolos | +1 |
| **Está entre las 10 000 más usadas** | **puntuación = 0** |

| Puntos | Nivel |
|---|---|
| 0–2 | Débil |
| 3–4 | Media |
| 5–6 | Fuerte |
| 7 | Muy fuerte |

En el analizador, una contraseña **común** o que aparezca en **alguna filtración** se muestra como **Insegura**, independientemente de su puntuación.

### Estructura anónima (backend)

La contraseña se divide en bloques de caracteres seguidos del mismo tipo (letras, dígitos o símbolos). Cada bloque se clasifica con la **primera** regla que cumpla, en este orden:

| Orden | Tipo | Regla | Ejemplo |
|---|---|---|---|
| 1 | `repeticion` | 3 o más veces el mismo carácter, sin distinguir mayúsculas | `aaaa`, `1111`, `!!!` |
| 2 | `palabra` | Letras | `Barcelona` |
| 3 | `fecha` | 8 dígitos que forman una fecha real (`ddMMyyyy` o `yyyyMMdd`) con el año entre 1900 y el siguiente al actual | `15031995`, `19950315` |
| 4 | `año` | 4 dígitos entre 1900 y el año siguiente al actual | `2023` |
| 5 | `numero` | El resto de bloques de dígitos | `7391`, `32011995` |
| 6 | `simbolo` | Todo lo demás | `!`, `@#` |

Las fechas se validan de verdad: `32011995` (día 32), `15131995` (mes 13) o `29022023` (29 de febrero en año no bisiesto) se quedan en `numero`.

El orden importa: `1111` es una **repetición** aunque esté dentro del rango de años, y `11111111` es una repetición antes que una fecha. Por ejemplo, `Barcelona2023!` se convierte en `palabra(9) año(4) simbolo(1) longitud: 14`. El resultado solo contiene **tipos y longitudes**, nunca el texto de la contraseña, y es lo único que recibe Gemini.

### Tiempo estimado para descifrarla (frontend)

El analizador compara tres ataques y muestra **el más rápido**, porque es el que usaría un atacante:

| Ataque | Cuándo gana | Tiempo |
|---|---|---|
| **Diccionario** | La contraseña es común o está filtrada | Al instante |
| **Modelo de Markov** (estimador en Python) | La contraseña sigue patrones habituales | `intentos estimados ÷ 10¹⁰ por segundo` |
| **Fuerza bruta** | La contraseña es realmente aleatoria | `2^entropía ÷ 10¹⁰ por segundo` |

La entropía por fuerza bruta se calcula así:

```
entropía = longitud × log₂(tamaño del alfabeto)
```

El alfabeto suma 26 (minúsculas), 26 (mayúsculas), 10 (números) y 32 (símbolos) según los tipos que contenga la contraseña. La velocidad de **10 000 millones de intentos por segundo** corresponde a una GPU doméstica contra *hashes* rápidos.

El tiempo principal es siempre el del ataque más rápido. Debajo, la **comparativa de ataques** muestra cada uno con su tiempo y una barra en escala logarítmica (de 1 milisegundo a unos 30 000 años), ordenados del más rápido al más lento, con el ganador marcado como «Más rápido». Las barras son rojas por debajo de una hora, ámbar por debajo de un año y verdes a partir de ahí.

La comparativa solo aparece si hay más de un ataque posible: si el estimador no está arrancado y la contraseña no es común ni está filtrada, solo queda la fuerza bruta, que ya muestra el tiempo principal.

---

## 🐍 Microservicio de estimación en Python

### Objetivo

El cálculo de entropía supone que el atacante prueba combinaciones al azar, pero los atacantes reales prueban primero lo que la gente suele usar. Este microservicio estima **en qué intento adivinaría la contraseña un atacante** que conoce esos hábitos. Por ejemplo, «~2 millones de intentos» frente a «siglos por fuerza bruta».

### Cómo funciona

1. **Datos:** la lista `10-million-password-list-top-1000000.txt` de [SecLists](https://github.com/danielmiessler/SecLists).
   - Se limpia: se descartan las líneas vacías, las de más de 32 caracteres y las que no son ASCII imprimible.
   - Se baraja con una semilla fija y se divide en un 90 % para entrenar y un 10 % para evaluar.
2. **Modelo de Markov de caracteres** (`ngram.py`): cuenta qué carácter sigue a cada contexto de *n* caracteres (orden 3 por defecto).
   - Usa marcas de inicio y fin, así también aprende cómo suelen empezar y acabar las contraseñas.
   - Aplica suavizado de Laplace, para que nada tenga probabilidad cero.
   - La probabilidad se calcula sumando logaritmos, para no perder precisión, y se expresa en **bits** (`−log₂ p`).
3. **Estimación Monte Carlo** (Dell'Amico y Filippone, 2015):
   - Al entrenar, se generan 50 000 contraseñas con el propio modelo y se guardan sus probabilidades ordenadas.
   - El número de intentos de una contraseña con probabilidad `p` es `Σ 1 / (n · pᵢ)`, sumando solo las muestras más probables que ella.
   - Con sumas acumuladas y búsqueda binaria, cada consulta tarda milisegundos.
4. **Evaluación:** porcentaje de contraseñas del 10 % de prueba que se adivinarían antes de 10⁶, 10⁹ y 10¹² intentos, comparando distintos órdenes y valores de suavizado.

| Archivo | Función | Cuándo se ejecuta |
|---|---|---|
| `ngram.py` | Clase `ModeloMarkov`: entrenamiento, probabilidad, muestreo, estimación y guardado | La usan los otros dos |
| `entrenar.py` | Limpia los datos, entrena, comprueba cada fase, evalúa y guarda el modelo en `modelo/` | A mano, cuando se quiere reentrenar |
| `main.py` | Carga el modelo al arrancar y responde a `POST /estimar` | Mientras el servicio está en marcha |

### Entrenamiento

```powershell
cd servicios/estimador
.\.venv\Scripts\python.exe entrenar.py                              # orden 3, alpha 0.01, 50 000 muestras
.\.venv\Scripts\python.exe entrenar.py --limite 100000              # prueba rápida con menos datos
.\.venv\Scripts\python.exe entrenar.py --comparar                   # compara orden 2/3/4 y alpha 0.001/0.01/0.1
.\.venv\Scripts\python.exe entrenar.py --orden 4 --alpha 0.001 --muestras 100000
```

Durante el entrenamiento se imprimen varias comprobaciones: el tamaño de los datos, los caracteres más probables tras `123`, el orden de dificultad de contraseñas conocidas, 20 contraseñas generadas por el modelo (deberían parecer reales) y la curva de adivinación.

### API del microservicio

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/health` | Indica si el modelo está cargado y su orden |
| `POST` | `/estimar` | Recibe `{ "contrasena": "..." }` (de 1 a 128 caracteres) y devuelve `longitud`, `bits`, `intentos_estimados`, `segundos` y `categoria` |

Las categorías se asignan por número de intentos:

| Intentos | Categoría |
|---|---|
| < 10⁶ | muy débil |
| < 10⁹ | débil |
| < 10¹² | aceptable |
| < 10¹⁵ | fuerte |
| ≥ 10¹⁵ | muy fuerte |

Con el servicio en marcha en local, la documentación interactiva está en `http://127.0.0.1:8000/docs`.

### Privacidad

- El servicio recibe la contraseña completa, así que **nunca es accesible desde fuera**: en local escucha en `127.0.0.1` y en Docker no publica ningún puerto.
- No tiene CORS, porque el navegador nunca habla con él.
- No registra ni guarda la contraseña en ningún caso.
- El backend lo usa a través de la interfaz `IEstimadorService` con un tiempo máximo de 3 segundos. Si no responde, el analizador solo muestra el cálculo por fuerza bruta.

---

## 🧪 Pruebas

### Microservicio de estimación

```powershell
cd servicios/estimador
.\.venv\Scripts\python.exe -m pytest -q
```

Las pruebas entrenan un modelo pequeño en memoria, así que no necesitan la lista de un millón de contraseñas. Comprueban que:

- una contraseña típica tiene menos bits y menos intentos que una aleatoria;
- un carácter nunca visto no da probabilidad cero;
- el muestreo es reproducible con la misma semilla;
- el modelo se guarda y se carga sin cambiar los resultados;
- la API responde con la forma esperada, devuelve 422 ante peticiones inválidas y 503 si el modelo no está cargado.

### Backend

```bash
cd backend
dotnet test
```

El proyecto `backend/tests/PasswordGuard.Api.Tests` usa **xUnit**, **NSubstitute** y **Microsoft.AspNetCore.Mvc.Testing**. Ninguna prueba sale a la red: Gemini y el estimador se sustituyen por un manejador HTTP falso o por dobles de NSubstitute.

| Archivo | Qué comprueba |
|---|---|
| `ContrasenaTests` | Cada tipo de bloque, los límites de años y fechas, fechas imposibles, el orden de las reglas y que la estructura **nunca incluye el texto de la contraseña** |
| `ContrasenasComunesServiceTests` | La lista de 10 000 contraseñas, sin distinguir mayúsculas |
| `ComprobarContrasenaServiceTests` | Cada requisito, los tipos que elige el usuario, la puntuación de 0 a 7 y los límites de cada nivel |
| `GeminiServiceTests` | La clave **en la cabecera y no en la URL**, que solo se envía la estructura, el modelo configurado y `null` ante errores (400, 403, 404, 429), respuestas vacías, texto que no es JSON, falta de red o timeout |
| `EstimadorServiceTests` | La lectura de la respuesta de Python en *snake_case*, `null` si el servicio falla, no está arrancado, tarda demasiado o devuelve un JSON roto, y que propaga la cancelación del cliente |
| `NivelSeguridadControllerTests` | **Integración** con `WebApplicationFactory`: los cuatro endpoints, que a la IA **solo le llega la estructura anónima**, las respuestas `null` cuando fallan los servicios y el 400 sin contraseña |

Para que `WebApplicationFactory<Program>` pueda arrancar la API, `Program.cs` termina con `public partial class Program { }`.

---

## 🌿 Flujo de trabajo con Git

El repositorio sigue un flujo basado en **Git Flow**:

| Rama | Uso |
|---|---|
| `main` | Versiones estables |
| `release` | Preparación de versiones |
| `develop` | Integración de las funcionalidades terminadas |
| `feature/*` | Una rama por funcionalidad: `feature/generadorContraseña`, `feature/comprobacionFiltraciones`, `feature/consejosIA`, `feature/reconocimientoPatrones` y `feature/estimacionPython` |

Las funcionalidades se integran en `develop` mediante *Pull Requests*.

---

## ⚠️ Limitaciones conocidas

- **El modelo de Markov no entiende estructuras**: ve la contraseña carácter a carácter, con solo 3 caracteres de memoria, así que no sabe que `Barcelona` es una palabra ni que `2023` es un año. Por eso subestima lo predecibles que son contraseñas como `Barcelona2023!` cuando no están filtradas. Una herramienta como hashcat, con diccionario y reglas, las sacaría mucho antes.
- **Datos de entrenamiento**: la lista de SecLists es sobre todo de usuarios angloparlantes y no incluye frecuencias; cada contraseña cuenta una sola vez. Puede subestimar contraseñas en español.
- **El modelo se guarda con `pickle`**, que solo es seguro con archivos propios: nunca cargues un `modelo.pkl.gz` de origen desconocido.
- **Aleatoriedad del generador**: el generador usa `Math.random()`, que **no es criptográficamente seguro**. Para contraseñas reales debería usar `crypto.getRandomValues()`.
- **Tipos de carácter no garantizados**: con longitudes cortas, la contraseña generada puede no incluir todos los tipos activos, porque cada carácter se elige al azar.
- **Textos de privacidad pendientes de revisar**: el distintivo «100% local» de la cabecera y algunos textos del generador indican que la contraseña no sale del navegador, pero el nivel de seguridad se calcula en la API.
- **Estructura incompleta**: la estructura anónima detecta palabras, números, símbolos, años, fechas de 8 dígitos y repeticiones de un mismo carácter, pero todavía no detecta fechas cortas o con separadores (`150395`, `15-03-1995`), secuencias (`1234`), grupos repetidos (`abcabc`), patrones de teclado (`qwerty`) ni *leetspeak* (`P@ssw0rd`).
- **Contraseñas cortas en el modelo**: el modelo de Markov puede estimar más tiempo que la propia fuerza bruta en contraseñas cortas (por ejemplo, 6 minúsculas). La comparativa lo deja a la vista, porque en ese caso gana la fuerza bruta.
- El archivo `PasswordGuard.Api.http` conserva la petición de ejemplo de la plantilla (`/weatherforecast`), que ya no existe.

---

## 🗺️ Hoja de ruta

- [x] Detectar años y repeticiones en la estructura anónima
- [x] Detectar fechas de 8 dígitos (`ddMMyyyy` y `yyyyMMdd`)
- [x] Comparativa de ataques en el analizador
- [x] Pruebas unitarias de los servicios y pruebas de integración de la API
- [x] Microservicio de estimación: entrenar y evaluar el modelo de Markov
- [x] Microservicio de estimación: API con FastAPI, pruebas con pytest e integración con el backend y el frontend
- [x] Docker Compose para arrancar frontend, backend y microservicio con un solo comando
- [ ] Añadir un modelo **PCFG** al estimador que aproveche la estructura anónima (`palabra + año + símbolo`) y quedarse con la estimación más baja
- [ ] Mejorar el modelo de Markov con *backoff* y entrenarlo también con contraseñas en español
- [ ] Detectar fechas cortas o con separadores, secuencias, grupos repetidos, patrones de teclado y *leetspeak*
- [ ] Usar `crypto.getRandomValues()` en el generador y garantizar al menos un carácter de cada tipo elegido
- [ ] Pruebas del frontend (componentes de Vue)
- [ ] Guardar en caché los consejos de la IA por estructura para reducir llamadas
- [ ] Revisar y unificar los textos de privacidad
- [ ] Configurar CORS para desplegar frontend y backend por separado
- [ ] Actualizar el paquete `Microsoft.OpenApi` (aviso de seguridad `NU1903`)

---

## 👤 Autor

[@JL-garrote](https://github.com/JL-garrote)
