# 🔐 Password Guard

**Password Guard** es una aplicación web para **generar contraseñas seguras** y **analizar la seguridad de las que ya usas**. Combina cálculos en el navegador, una API propia en ASP.NET Core, la comprobación de filtraciones de [Have I Been Pwned](https://haveibeenpwned.com/Passwords) y consejos personalizados generados con IA (Google Gemini).

> 🚧 **Proyecto en desarrollo.** Es un proyecto personal de aprendizaje de desarrollo *full stack* con Vue 3 y .NET. Algunas funciones están en construcción; consulta [Limitaciones conocidas](#-limitaciones-conocidas) y la [Hoja de ruta](#-hoja-de-ruta).

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

- **Fortaleza**: nivel de 1 a 4 (*Débil*, *Media*, *Fuerte*, *Muy fuerte* o *Insegura*), entropía, número de combinaciones posibles y **tiempo estimado para descifrarla** por fuerza bruta.
- **Desglose de la composición**: longitud, mayúsculas y minúsculas combinadas, números y símbolos.
- **Contraseñas comunes**: comprobación contra una lista de las **10 000 contraseñas más usadas**.
- **Filtraciones**: cuántas veces aparece la contraseña en filtraciones públicas según **Have I Been Pwned**, usando *k-anonymity*.
- **Patrones y consejos con IA**: la estructura anónima de la contraseña (por ejemplo, `Palabra · 9 + Número · 4 + Símbolo · 1`) y tres consejos personalizados generados por **Gemini**.
- **Pruebas rápidas** con contraseñas de ejemplo y botón para mostrar u ocultar la contraseña.

Si una contraseña es **común** o aparece en **alguna filtración**, se marca como **Insegura** y se estima que se descifraría **al instante**, aunque cumpla el resto de requisitos.

---

## 🛡️ Privacidad: qué datos salen de tu equipo

Password Guard **no guarda ninguna contraseña**: no hay base de datos, ni cookies, ni almacenamiento local. Aun así, conviene saber exactamente qué viaja por la red:

| Destino | Qué se envía | Para qué |
|---|---|---|
| **API de Password Guard** (tu propio servidor) | La contraseña completa | Calcular su nivel, comprobar si es común y extraer su estructura anónima |
| **Have I Been Pwned** | Solo los **5 primeros caracteres del hash SHA-1**, calculado en el navegador | Comprobar filtraciones sin revelar la contraseña (*k-anonymity*) |
| **Google Gemini** | Solo la **estructura anónima**, por ejemplo `palabra(9) numero(4) simbolo(1) longitud: 14` | Generar consejos personalizados |

- La comprobación de filtraciones funciona así: el navegador calcula el SHA-1 de la contraseña, envía solo sus 5 primeros caracteres y recibe cientos de hashes que empiezan igual. La coincidencia se busca **en el navegador**, así que Have I Been Pwned nunca conoce la contraseña.
- A Gemini **nunca** le llega la contraseña ni ningún fragmento de ella: solo los tipos de cada bloque y sus longitudes.
- Los registros (*logs*) del backend no incluyen la contraseña ni fragmentos de ella.

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
- Inyección de dependencias (`AddScoped`, `AddSingleton`, `AddHttpClient`)
- [DotNetEnv](https://www.nuget.org/packages/DotNetEnv) para cargar la configuración desde un archivo `.env`
- `HttpClient` para llamar a la API REST de **Google Gemini** con salida JSON estructurada
- OpenAPI (documento en `/openapi/v1.json` en desarrollo)

**Pruebas**

- [xUnit](https://xunit.net/), [NSubstitute](https://nsubstitute.github.io/) y `Microsoft.AspNetCore.Mvc.Testing` (proyecto preparado)

**Servicios externos**

- [Have I Been Pwned — Pwned Passwords](https://haveibeenpwned.com/API/v3#PwnedPasswords) (gratuito, sin clave)
- [Google Gemini API](https://ai.google.dev/) (requiere clave)

---

## 🏗️ Arquitectura

```
┌──────────────────────────── Navegador ────────────────────────────┐
│  Vue 3 + Vite  (http://localhost:5173)                            │
│                                                                   │
│  · Generador y analizador                                         │
│  · Entropía y tiempo estimado (cálculo local)                     │
│  · SHA-1 de la contraseña ──────────────► Have I Been Pwned       │
│                                          (solo 5 caracteres)      │
└───────────────┬───────────────────────────────────────────────────┘
                │  /api/*  (proxy de Vite)
                ▼
┌──────────────── API ASP.NET Core (http://localhost:5225) ─────────┐
│  nivelSeguridadController                                         │
│   ├─ comprobarContrasenaService  → requisitos y puntuación        │
│   ├─ contrasenasComunesService   → lista de 10 000 contraseñas    │
│   ├─ Contrasena                  → estructura anónima             │
│   └─ IConsejosIAService (GeminiService) ────────► Google Gemini   │
│                                   (solo la estructura anónima)    │
└───────────────────────────────────────────────────────────────────┘
```

---

## 📂 Estructura del repositorio

```
passwordGuard/
├── backend/
│   ├── PasswordGuard.slnx                    # Solución de .NET
│   ├── src/PasswordGuard.Api/
│   │   ├── Controllers/
│   │   │   └── nivelSeguridadController.cs   # Endpoints de la API
│   │   ├── Service/
│   │   │   ├── comprobarContrasenaService.cs # Requisitos, puntuación y nivel
│   │   │   ├── contrasenasComunesService.cs  # Lista de contraseñas comunes
│   │   │   ├── IConsejosIAService.cs         # Contrato del servicio de IA
│   │   │   └── GeminiService.cs              # Implementación con Gemini
│   │   ├── models/
│   │   │   ├── Contrasena.cs                 # Estructura anónima de la contraseña
│   │   │   └── ConsejosIA.cs                 # Consejos devueltos por la IA
│   │   ├── data/
│   │   │   └── 10k_most_common.txt           # 10 000 contraseñas más usadas
│   │   ├── Properties/launchSettings.json    # Puertos de desarrollo
│   │   ├── Program.cs                        # Arranque y registro de servicios
│   │   └── .env                              # Clave de Gemini (no se sube a Git)
│   └── tests/PasswordGuard.Api.Tests/        # Proyecto de pruebas (xUnit)
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
    └── vite.config.ts                        # Proxy /api → backend
```

---

## ✅ Requisitos

| Herramienta | Versión |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10 |
| [Node.js](https://nodejs.org/) | 22.18 o superior (o 24.12 o superior) |
| npm | incluido con Node.js |
| Clave de API de Gemini | opcional, solo para los consejos con IA ([Google AI Studio](https://aistudio.google.com/)) |

---

## 🚀 Instalación y puesta en marcha

### 1. Clonar el repositorio

```bash
git clone https://github.com/JL-garrote/passwordGuard.git
cd passwordGuard
```

### 2. Configurar el backend

Crea el archivo `backend/src/PasswordGuard.Api/.env` con tu clave de Gemini (consulta [Configuración](#-configuración)):

```env
Gemini__ApiKey=tu-clave-de-google-ai-studio
```

> Sin clave, la aplicación funciona igualmente: solo el endpoint de consejos devolverá un error y el analizador mostrará consejos generales.

### 3. Arrancar el backend

```bash
cd backend/src/PasswordGuard.Api
dotnet run
```

La API queda disponible en `http://localhost:5225` cuando aparece el mensaje `Now listening on: http://localhost:5225`.

### 4. Arrancar el frontend

En **otra terminal**:

```bash
cd frontend
npm install        # solo la primera vez
npm run dev
```

Abre en el navegador la dirección que indique Vite, normalmente `http://localhost:5173`.

Para detener cualquiera de los dos servicios, pulsa `Ctrl+C` en su terminal.

### Otros comandos útiles

| Comando | Dónde | Qué hace |
|---|---|---|
| `dotnet build` | `backend/` | Compila la solución |
| `dotnet test` | `backend/` | Ejecuta las pruebas |
| `npm run build` | `frontend/` | Comprueba los tipos y genera la versión de producción en `dist/` |
| `npm run preview` | `frontend/` | Sirve la versión de producción generada |

---

## ⚙️ Configuración

El backend lee la configuración con el sistema estándar de ASP.NET Core. Las variables del archivo `.env` se cargan al arrancar gracias a **DotNetEnv**. El doble guion bajo (`__`) equivale a `:` en la configuración, así que `Gemini__ApiKey` se lee como `Gemini:ApiKey`.

| Variable | Obligatoria | Valor por defecto | Descripción |
|---|---|---|---|
| `Gemini__ApiKey` | Solo para los consejos con IA | — | Clave de la API de Gemini |
| `Gemini__Model` | No | `gemini-3.8-flash` | Modelo de Gemini (por ejemplo, `gemini-3.5-flash-lite` para reducir costes) |

> 🔒 **El archivo `.env` nunca debe subirse a Git.** Ya está incluido en `backend/.gitignore`. Si una clave llega a publicarse por error, **revócala en Google AI Studio y genera otra**: borrarla en un commit posterior no la elimina del historial.

**Puertos de desarrollo**

| Servicio | URL |
|---|---|
| API (HTTP) | `http://localhost:5225` |
| API (HTTPS, perfil `https`) | `https://localhost:7109` |
| Frontend | `http://localhost:5173` |

El frontend llama a rutas relativas (`/api/...`) y Vite las reenvía a `http://localhost:5225` mediante el *proxy* definido en `vite.config.ts`, por lo que en desarrollo no hace falta configurar CORS.

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
  "patrones": "palabra(9) numero(4) simbolo(1) longitud: 14",
  "consejos": [
    { "titulo": "…", "texto": "…" },
    { "titulo": "…", "texto": "…" },
    { "titulo": "…", "texto": "…" }
  ]
}
```

Si Gemini no responde (error de red, límite de peticiones, clave incorrecta, más de 15 segundos de espera…), `consejos` llega como `null` y el backend deja un aviso en el registro con el código de error. Si la clave no está configurada, este endpoint devuelve un error 500, pero **el resto de endpoints siguen funcionando**.

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

### Entropía y tiempo estimado (frontend)

```
entropía = longitud × log₂(tamaño del alfabeto)
```

El alfabeto suma 26 (minúsculas), 26 (mayúsculas), 10 (números) y 32 (símbolos) según los tipos que contenga la contraseña. El tiempo estimado supone un ataque de **fuerza bruta a 10 000 millones de intentos por segundo** (una GPU doméstica contra *hashes* rápidos).

> Esta estimación solo contempla la fuerza bruta pura. Los ataques de diccionario descifran mucho antes las contraseñas basadas en palabras, fechas o patrones conocidos; por eso las contraseñas comunes o filtradas se marcan como descifrables **al instante**.

---

## 🧪 Pruebas

El proyecto `backend/tests/PasswordGuard.Api.Tests` está preparado con **xUnit**, **NSubstitute** y **Microsoft.AspNetCore.Mvc.Testing**, pero **todavía no contiene pruebas**.

```bash
cd backend
dotnet test
```

El servicio de IA se usa a través de la interfaz `IConsejosIAService`, así que en las pruebas puede sustituirse por un doble con NSubstitute sin llamar a Gemini.

---

## 🌿 Flujo de trabajo con Git

El repositorio sigue un flujo basado en **Git Flow**:

| Rama | Uso |
|---|---|
| `main` | Versiones estables |
| `release` | Preparación de versiones |
| `develop` | Integración de las funcionalidades terminadas |
| `feature/*` | Una rama por funcionalidad (por ejemplo, `feature/generadorContraseña`, `feature/comprobacionFiltraciones`, `feature/consejosIA`) |

Las funcionalidades se integran en `develop` mediante *Pull Requests*.

---

## ⚠️ Limitaciones conocidas

- **Aleatoriedad del generador**: el generador usa `Math.random()`, que **no es criptográficamente seguro**. Para contraseñas reales debería usar `crypto.getRandomValues()`.
- **Tipos de carácter no garantizados**: con longitudes cortas, la contraseña generada puede no incluir todos los tipos activos, porque cada carácter se elige al azar.
- **Textos de privacidad pendientes de revisar**: el distintivo «100% local» de la cabecera y algunos textos del generador indican que la contraseña no sale del navegador, pero el nivel de seguridad se calcula en la API.
- **Estructura básica**: la estructura anónima distingue palabras, números y símbolos, pero todavía no detecta años, fechas, secuencias (`1234`), repeticiones (`aaaa`), patrones de teclado (`qwerty`) ni *leetspeak* (`P@ssw0rd`).
- **Sin pruebas automáticas** todavía.
- El archivo `PasswordGuard.Api.http` conserva la petición de ejemplo de la plantilla (`/weatherforecast`), que ya no existe.

---

## 🗺️ Hoja de ruta

- [ ] Detectar años, fechas, secuencias, repeticiones, patrones de teclado y *leetspeak* en la estructura anónima
- [ ] Usar `crypto.getRandomValues()` en el generador y garantizar al menos un carácter de cada tipo elegido
- [ ] Añadir pruebas unitarias de los servicios y pruebas de integración de la API
- [ ] Guardar en caché los consejos de la IA por estructura para reducir llamadas
- [ ] Revisar y unificar los textos de privacidad
- [ ] Configurar CORS para desplegar frontend y backend por separado
- [ ] Actualizar el paquete `Microsoft.OpenApi` (aviso de seguridad `NU1903`)

---

## 👤 Autor

[@JL-garrote](https://github.com/JL-garrote)
