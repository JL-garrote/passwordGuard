<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { comprobarFiltraciones } from '../filtraciones.js';

const emit = defineEmits<{ generar: [] }>();

const ICONOS = {
  candado:
    'M18 8h-1V6A5 5 0 0 0 7 6v2H6a2 2 0 0 0-2 2v10c0 1.1.9 2 2 2h12a2 2 0 0 0 2-2V10a2 2 0 0 0-2-2Zm-6 9a2 2 0 1 1 0-4 2 2 0 0 1 0 4Zm3.1-9H8.9V6a3.1 3.1 0 0 1 6.2 0v2Z',
  escudo: 'M12 1 3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4Zm-2 16-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8Z',
  ver: 'M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5ZM12 17a5 5 0 1 1 0-10 5 5 0 0 1 0 10Zm0-8a3 3 0 1 0 0 6 3 3 0 0 0 0-6Z',
  ocultar:
    'M12 7a5 5 0 0 1 4.64 6.83l2.92 2.92A11.8 11.8 0 0 0 23 12c-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7ZM2 4.27l2.74 2.74A11.8 11.8 0 0 0 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84L19.73 22 21 20.73 3.27 3 2 4.27Zm5.53 5.53 1.55 1.55A3 3 0 0 0 12.65 14.92l1.55 1.55A5 5 0 0 1 7.53 9.8Zm4.31-.78 3.15 3.15.02-.16a3 3 0 0 0-3-3l-.17.01Z',
  cerrar: 'M19 6.41 17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41Z',
  rayo: 'M11 21h-1l1-7H7.5c-.88 0-.33-.75-.31-.78C8.48 10.94 10.42 7.54 13 3h1l-1 7h3.5c.49 0 .56.33.47.51C12.96 17.55 11 21 11 21Z',
  ok: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm-2 15-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9Z',
  mal: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59Z',
  aviso: 'M1 21h22L12 2 1 21Zm12-3h-2v-2h2v2Zm0-4h-2v-4h2v4Z',
  info: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm1 15h-2v-6h2v6Zm0-8h-2V7h2v2Z',
  chispas:
    'm19 9 1.25-2.75L23 5l-2.75-1.25L19 1l-1.25 2.75L15 5l2.75 1.25L19 9Zm-7.5.5L9 4 6.5 9.5 1 12l5.5 2.5L9 20l2.5-5.5L17 12l-5.5-2.5ZM19 15l-1.25 2.75L15 19l2.75 1.25L19 23l1.25-2.75L23 19l-2.75-1.25L19 15Z',
  llave:
    'M12.65 10A6 6 0 0 0 1 12a6 6 0 0 0 11.65 2H17v4h4v-4h2v-4H12.65ZM7 14a2 2 0 1 1 0-4 2 2 0 0 1 0 4Z',
};

const PRUEBAS_RAPIDAS = ['Barcelona2023!', '123456', 'tr0ub4dor&3', 'Correct-Horse-Battery-Staple'];

const contrasena = ref('');
const visible = ref(false);
const campo = ref<HTMLInputElement | null>(null);

function limpiar() {
  contrasena.value = '';
  campo.value?.focus();
}

const longitud = computed(() => [...contrasena.value].length);
const tiene = computed(() => ({
  mayusculas: /\p{Lu}/u.test(contrasena.value),
  minusculas: /\p{Ll}/u.test(contrasena.value),
  numeros: /\p{Nd}/u.test(contrasena.value),
  simbolos: /[^\p{L}\p{Nd}]/u.test(contrasena.value),
}));

// Entropía por fuerza bruta: longitud × log2(tamaño del alfabeto que usa la contraseña)
const alfabeto = computed(
  () =>
    (tiene.value.minusculas ? 26 : 0) +
    (tiene.value.mayusculas ? 26 : 0) +
    (tiene.value.numeros ? 10 : 0) +
    (tiene.value.simbolos ? 32 : 0)
);
const entropia = computed(() => (alfabeto.value ? longitud.value * Math.log2(alfabeto.value) : 0));
const combinaciones = computed(() => 2 ** entropia.value);

// Una GPU doméstica prueba del orden de 10.000 millones de hashes rápidos por segundo
const INTENTOS_POR_SEGUNDO = 1e10;

function formatearTiempo(segundos: number): string {
  if (segundos < 1) return 'al instante';
  const unidades: [number, string, string][] = [
    [60, 'segundo', 'segundos'],
    [60, 'minuto', 'minutos'],
    [24, 'hora', 'horas'],
    [365, 'día', 'días'],
    [100, 'año', 'años'],
  ];
  let valor = segundos;
  for (const [factor, singular, plural] of unidades) {
    if (valor < factor) {
      const n = Math.floor(valor);
      return `en ~${n} ${n === 1 ? singular : plural}`;
    }
    valor /= factor;
  }
  return 'en siglos';
}

// Contraseña común (lista de las 10.000 más usadas del backend): true, false o null si todavía no hay dato
const esComun = ref<boolean | null>(null);
let ultimaConsultaComun = 0;

async function consultarComun(valor: string) {
  const consulta = ++ultimaConsultaComun;
  try {
    const respuesta = await fetch('/api/nivelSeguridad/evaluar', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contrasena: valor }),
    });
    if (!respuesta.ok) throw new Error(`Error ${respuesta.status}`);
    const datos = (await respuesta.json()) as { esComun: boolean };
    if (consulta === ultimaConsultaComun) esComun.value = datos.esComun;
  } catch {
    if (consulta === ultimaConsultaComun) esComun.value = null;
  }
}

// Una contraseña común o filtrada cae en un ataque de diccionario, sin necesidad de fuerza bruta
const tiempoDescifrado = computed(() =>
  esComun.value || filtrada.value ? 'al instante' : formatearTiempo(combinaciones.value / INTENTOS_POR_SEGUNDO)
);

// Nivel de seguridad: lo calcula el backend (incluye la lista de contraseñas comunes)
const NIVELES_SERVIDOR: Record<string, { n: number; etiqueta: string; clase: string }> = {
  'Débil': { n: 1, etiqueta: 'Débil', clase: 'debil' },
  'Media': { n: 2, etiqueta: 'Media', clase: 'aceptable' },
  'Fuerte': { n: 3, etiqueta: 'Fuerte', clase: 'fuerte' },
  'Muy fuerte': { n: 4, etiqueta: 'Muy fuerte', clase: 'muy-fuerte' },
};

const nivelServidor = ref<string | null>(null);
let evaluarTimer: ReturnType<typeof setTimeout> | undefined;
let ultimaPeticion = 0;

async function evaluarEnServidor(valor: string) {
  const peticion = ++ultimaPeticion;
  try {
    const respuesta = await fetch('/api/nivelSeguridad/comprobar', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contrasena: valor,
        usarMayusculas: true,
        usarMinusculas: true,
        usarNumeros: true,
        usarSimbolos: true,
      }),
    });
    if (!respuesta.ok) throw new Error(`Error ${respuesta.status}`);
    const datos = (await respuesta.json()) as { valida: boolean; nivelSeguridad: string };
    // Ignora respuestas de contraseñas antiguas que lleguen tarde
    if (peticion === ultimaPeticion) nivelServidor.value = datos.nivelSeguridad;
  } catch {
    // Si la API no responde, se usa el cálculo local por entropía
    if (peticion === ultimaPeticion) nivelServidor.value = null;
  }
}

// Filtraciones (Have I Been Pwned): veces que aparece la contraseña, o null si todavía no hay dato
const filtraciones = ref<number | null>(null);
const estadoFiltraciones = ref<'cargando' | 'error' | 'listo'>('listo');
let ultimaConsultaFiltraciones = 0;

// Filtrada: aparece al menos una vez en Have I Been Pwned
const filtrada = computed(() => (filtraciones.value ?? 0) > 0);

async function consultarFiltraciones(valor: string) {
  const consulta = ++ultimaConsultaFiltraciones;
  estadoFiltraciones.value = 'cargando';
  try {
    const veces = await comprobarFiltraciones(valor);
    // Ignora respuestas de contraseñas antiguas que lleguen tarde
    if (consulta !== ultimaConsultaFiltraciones) return;
    filtraciones.value = veces;
    estadoFiltraciones.value = 'listo';
  } catch {
    if (consulta !== ultimaConsultaFiltraciones) return;
    filtraciones.value = null;
    estadoFiltraciones.value = 'error';
  }
}

// Espera 300 ms sin cambios para no lanzar una petición por cada tecla
watch(contrasena, (valor) => {
  clearTimeout(evaluarTimer);
  if (!valor) {
    ultimaPeticion++;
    ultimaConsultaFiltraciones++;
    ultimaConsultaComun++;
    nivelServidor.value = null;
    filtraciones.value = null;
    estadoFiltraciones.value = 'listo';
    esComun.value = null;
    return;
  }
  evaluarTimer = setTimeout(() => {
    evaluarEnServidor(valor);
    consultarFiltraciones(valor);
    consultarComun(valor);
  }, 300);
});

const nivel = computed(() => {
  // Una contraseña común o filtrada es insegura aunque cumpla todo lo demás
  if (esComun.value || filtrada.value) return { n: 1, etiqueta: 'Insegura', clase: 'debil' };

  const servidor = nivelServidor.value ? NIVELES_SERVIDOR[nivelServidor.value] : undefined;
  if (servidor) return servidor;

  const e = entropia.value;
  if (e >= 80) return { n: 4, etiqueta: 'Muy fuerte', clase: 'muy-fuerte' };
  if (e >= 60) return { n: 3, etiqueta: 'Fuerte', clase: 'fuerte' };
  if (e >= 40) return { n: 2, etiqueta: 'Aceptable', clase: 'aceptable' };
  return { n: 1, etiqueta: 'Débil', clase: 'debil' };
});

// Desglose de la estructura en chips
const chips = computed(() => {
  const l = longitud.value;
  const t = tiene.value;
  const comun = esComun.value
    ? [{ estado: 'mal', texto: 'Contraseña muy común', detalle: 'Está entre las 10.000 más usadas' }]
    : [];
  const filtrado = filtrada.value
    ? [
        {
          estado: 'mal',
          texto: 'Aparece en filtraciones',
          detalle: `Vista ${(filtraciones.value ?? 0).toLocaleString('es-ES')} ${filtraciones.value === 1 ? 'vez' : 'veces'}`,
        },
      ]
    : [];
  return [
    ...comun,
    ...filtrado,
    l >= 12
      ? { estado: 'ok', texto: `${l} caracteres`, detalle: 'Longitud adecuada' }
      : l >= 8
        ? { estado: 'aviso', texto: `${l} caracteres`, detalle: 'Mejor 12 o más' }
        : { estado: 'mal', texto: `${l} caracteres`, detalle: 'Demasiado corta' },
    t.mayusculas && t.minusculas
      ? { estado: 'ok', texto: 'Mayúsculas y minúsculas combinadas' }
      : { estado: 'mal', texto: 'Sin combinar mayúsculas y minúsculas' },
    t.numeros ? { estado: 'ok', texto: 'Contiene números' } : { estado: 'mal', texto: 'Sin números' },
    t.simbolos ? { estado: 'ok', texto: 'Contiene símbolos' } : { estado: 'mal', texto: 'Sin símbolos' },
  ] as { estado: 'ok' | 'aviso' | 'mal'; texto: string; detalle?: string }[];
});

onBeforeUnmount(() => clearTimeout(evaluarTimer));
</script>

<template>
  <div class="analizador">
    <!-- Introducción -->
    <header class="cabecera">
      <span class="distintivo">
        <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true"><path fill="currentColor" :d="ICONOS.escudo" /></svg>
        No guardamos ninguna contraseña
      </span>
      <h1>Analiza tu contraseña</h1>
      <p>Calculamos la entropía en tu navegador y el nivel de seguridad en la API de Password Guard.</p>
    </header>

    <!-- Campo de entrada -->
    <section class="tarjeta">
      <div class="campo">
        <svg viewBox="0 0 24 24" width="24" height="24" class="campo-icono" aria-hidden="true"><path fill="currentColor" :d="ICONOS.candado" /></svg>
        <input
          ref="campo"
          v-model="contrasena"
          :type="visible ? 'text' : 'password'"
          autocomplete="off"
          spellcheck="false"
          aria-label="Contraseña para analizar"
          placeholder="Escribe o pega una contraseña para analizar…"
        />
        <button
          type="button"
          class="btn-icono"
          :aria-label="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'"
          @click="visible = !visible"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path fill="currentColor" :d="visible ? ICONOS.ocultar : ICONOS.ver" />
          </svg>
        </button>
        <button type="button" class="btn-icono btn-limpiar" aria-label="Limpiar campo" @click="limpiar">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" :d="ICONOS.cerrar" /></svg>
        </button>
      </div>

      <div class="pruebas">
        <span class="texto-suave">Pruebas rápidas:</span>
        <button v-for="p in PRUEBAS_RAPIDAS" :key="p" type="button" class="pastilla" @click="contrasena = p">
          {{ p }}
        </button>
      </div>

      <div class="nota">
        <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true"><path fill="currentColor" :d="ICONOS.candado" /></svg>
        <span>La contraseña se envía a nuestra API solo para calcular su nivel. No se guarda en ningún sitio.</span>
      </div>
    </section>

    <!-- Estado vacío -->
    <section v-if="!contrasena" class="tarjeta vacio">
      <div class="vacio-icono">
        <svg viewBox="0 0 24 24" width="32" height="32" aria-hidden="true"><path fill="currentColor" :d="ICONOS.escudo" /></svg>
      </div>
      <h3>Escribe una contraseña para empezar el análisis</h3>
      <p class="texto-suave">
        Evaluaremos su entropía, su composición, si está entre las contraseñas más usadas y si aparece en filtraciones.
      </p>
    </section>

    <!-- Resultados -->
    <div v-else class="resultados">
      <!-- Fortaleza y entropía -->
      <section class="tarjeta">
        <div class="tarjeta-cabecera">
          <div class="titulo-con-icono">
            <span class="icono-caja">
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path fill="currentColor" :d="ICONOS.rayo" /></svg>
            </span>
            <div>
              <h2>Fortaleza de la contraseña</h2>
              <p class="texto-suave">Estimación de su resistencia ante ataques de fuerza bruta</p>
            </div>
          </div>
          <span class="badge" :class="nivel.clase">{{ nivel.etiqueta }} (Nivel {{ nivel.n }})</span>
        </div>

        <div
          class="medidor"
          role="progressbar"
          aria-valuemin="1"
          aria-valuemax="4"
          :aria-valuenow="nivel.n"
          :aria-valuetext="nivel.etiqueta"
        >
          <span v-for="s in 4" :key="s" :class="s <= nivel.n ? nivel.clase : ''"></span>
        </div>

        <div class="tiempo">
          <div>
            <span class="etiqueta-mayus">Tiempo estimado para descifrarla</span>
            <div class="tiempo-valor">Se descifraría {{ tiempoDescifrado }}</div>
            <span v-if="esComun" class="texto-suave">
              con un ataque de diccionario: está entre las contraseñas más usadas, que son lo primero que se prueba.
            </span>
            <span v-else-if="filtrada" class="texto-suave">
              con un ataque de diccionario: aparece en filtraciones públicas, que los atacantes prueban antes que nada.
            </span>
            <span v-else class="texto-suave">probando 10.000 millones de combinaciones por segundo (una GPU doméstica).</span>
          </div>
          <div class="metricas">
            <span class="mono">{{ entropia.toFixed(1) }} bits de entropía</span>
            <span class="mono texto-suave">~{{ combinaciones.toExponential(1) }} combinaciones</span>
          </div>
        </div>

        <div class="chips">
          <div v-for="chip in chips" :key="chip.texto" class="chip">
            <svg viewBox="0 0 24 24" width="16" height="16" :class="chip.estado" aria-hidden="true">
              <path fill="currentColor" :d="ICONOS[chip.estado]" />
            </svg>
            <span>
              {{ chip.texto }}
              <strong v-if="chip.detalle">({{ chip.detalle }})</strong>
            </span>
          </div>
        </div>
      </section>

      <!-- Filtraciones -->
      <section class="tarjeta">
        <div class="tarjeta-cabecera">
          <div class="titulo-con-icono">
            <span class="icono-caja error">
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path fill="currentColor" :d="ICONOS.aviso" /></svg>
            </span>
            <div>
              <h2>Filtraciones y brechas de seguridad</h2>
              <p class="texto-suave">Comprobación contra colecciones públicas conocidas (Have I Been Pwned)</p>
            </div>
          </div>
        </div>

        <div v-if="estadoFiltraciones === 'error'" class="alerta pendiente">
          <span class="alerta-icono">
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path fill="currentColor" :d="ICONOS.info" /></svg>
          </span>
          <div>
            <div class="alerta-titulo">No se ha podido comprobar</div>
            <p class="texto-suave">Have I Been Pwned no responde ahora mismo. Vuelve a intentarlo en unos segundos.</p>
          </div>
        </div>

        <div v-else-if="filtraciones === null" class="alerta pendiente" aria-live="polite">
          <span class="alerta-icono">
            <span class="spinner" aria-hidden="true"></span>
          </span>
          <div>
            <div class="alerta-titulo">Comprobando filtraciones…</div>
            <p class="texto-suave">Consultando Have I Been Pwned con los 5 primeros caracteres del hash.</p>
          </div>
        </div>

        <div v-else-if="filtraciones > 0" class="alerta comprometida">
          <span class="alerta-icono">
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path fill="currentColor" :d="ICONOS.aviso" /></svg>
          </span>
          <div>
            <div class="alerta-titulo">
              Vista {{ filtraciones.toLocaleString('es-ES') }} {{ filtraciones === 1 ? 'vez' : 'veces' }} en filtraciones conocidas
              <span class="etiqueta-estado">Comprometida</span>
            </div>
            <p class="texto-suave">
              Esta contraseña ha sido expuesta públicamente en filtraciones de credenciales. Los atacantes la prueban
              automáticamente en cuestión de segundos.
            </p>
          </div>
        </div>

        <div v-else class="alerta limpia">
          <span class="alerta-icono">
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path fill="currentColor" :d="ICONOS.ok" /></svg>
          </span>
          <div>
            <div class="alerta-titulo">
              No aparece en ninguna filtración conocida
              <span class="etiqueta-estado">Limpia</span>
            </div>
            <p class="texto-suave">No hay coincidencias en la base de datos de Have I Been Pwned.</p>
          </div>
        </div>

        <div class="explicacion">
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true"><path fill="currentColor" :d="ICONOS.candado" /></svg>
          <div>
            <span class="titulo-seccion">Privacidad con k-anonymity</span>
            <p class="texto-suave">
              Tu navegador calcula el hash SHA-1 de la contraseña y solo envía a Have I Been Pwned sus 5 primeros
              caracteres. El servicio devuelve cientos de hashes que empiezan igual y la coincidencia se busca aquí, en
              tu navegador, así que Have I Been Pwned nunca recibe tu contraseña.
            </p>
          </div>
        </div>
      </section>

      <!-- Patrones y consejos -->
      <section class="tarjeta">
        <div class="tarjeta-cabecera">
          <div class="titulo-con-icono">
            <span class="icono-caja primario">
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path fill="currentColor" :d="ICONOS.chispas" /></svg>
            </span>
            <div>
              <h2>Patrones y consejos</h2>
              <p class="texto-suave">Detección de palabras, fechas y sustituciones leet</p>
            </div>
          </div>
        </div>

        <div class="alerta pendiente">
          <span class="alerta-icono">
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path fill="currentColor" :d="ICONOS.info" /></svg>
          </span>
          <div>
            <div class="alerta-titulo">Próximamente</div>
            <p class="texto-suave">
              Pronto detectaremos estructuras predecibles como <em>Palabra + Año + Símbolo</em>, el patrón que más
              prueban herramientas como Hashcat o John the Ripper.
            </p>
          </div>
        </div>

        <div class="consejos">
          <div class="consejo">
            <span class="numero">1</span>
            <h3>Crea una frase de contraseña</h3>
            <p class="texto-suave">Usa 4 o más palabras sin relación entre sí (ej. <em>piano-reloj-salto-arena</em>).</p>
          </div>
          <div class="consejo">
            <span class="numero">2</span>
            <h3>Evita años y fechas</h3>
            <p class="texto-suave">Los años recientes y los cumpleaños son lo primero que prueba el software de ataque.</p>
          </div>
          <div class="consejo">
            <span class="numero">3</span>
            <h3>Intercala símbolos</h3>
            <p class="texto-suave">Un símbolo en medio de la contraseña es mucho más difícil de adivinar que uno al final.</p>
          </div>
        </div>

        <div class="llamada">
          <span class="texto-suave">¿Prefieres no pensarla? Genera una aleatoria al instante.</span>
          <button type="button" class="btn-primario" @click="emit('generar')">
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path fill="currentColor" :d="ICONOS.llave" /></svg>
            Generar una contraseña segura
          </button>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.analizador {
  grid-column: 1 / -1; 
  width: 100%;
  max-width: 768px;
  margin: 0 auto;
  padding: 1rem 0;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  color: var(--ink);
  font-family: 'IBM Plex Sans', system-ui, sans-serif;
  font-size: 14px;
  line-height: 20px;
}

.mono,
.pastilla,
.campo input {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
}

button {
  font: inherit;
  cursor: pointer;
  border: none;
}
button:focus-visible,
input:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

h1,
h2,
h3,
p {
  margin: 0;
}

.texto-suave {
  color: var(--ink-muted);
  font-size: 12px;
  line-height: 16px;
}
.titulo-seccion {
  font-weight: 600;
  color: var(--ink);
}
.etiqueta-mayus {
  display: block;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--ink-muted);
}

/* Cabecera */
.cabecera {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  text-align: center;
}
.cabecera h1 {
  font-size: 32px;
  line-height: 40px;
  font-weight: 600;
  letter-spacing: -0.01em;
}
.cabecera p {
  font-size: 16px;
  line-height: 24px;
  color: var(--ink-muted);
}
.distintivo {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 1rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--secondary-container) 30%, transparent);
  color: var(--secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

/* Tarjetas */
.tarjeta {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.5rem;
  border-radius: 0.75rem;
  background: var(--card);
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
}
.resultados {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}
.tarjeta-cabecera {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
.titulo-con-icono {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.titulo-con-icono h2 {
  font-size: 20px;
  line-height: 28px;
  font-weight: 600;
}
.icono-caja {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  border-radius: 0.5rem;
  background: var(--subtle);
  color: var(--primary);
}
.icono-caja.error {
  background: color-mix(in srgb, var(--error-container) 40%, transparent);
  color: var(--error);
}
.icono-caja.primario {
  background: color-mix(in srgb, var(--primary) 15%, transparent);
}

/* Campo de entrada */
.campo {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  height: 64px;
  padding: 0 1rem;
  border-radius: 0.75rem;
  background: var(--subtle);
  transition: background 0.2s, box-shadow 0.2s;
}
.campo:focus-within {
  background: var(--card);
  box-shadow: 0 0 0 2px var(--primary);
}
.campo-icono {
  flex-shrink: 0;
  color: var(--ink-muted);
}
.campo input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  color: var(--ink);
  font-size: 18px;
  line-height: 28px;
  font-weight: 500;
  letter-spacing: 0.03em;
}
.campo input:focus-visible {
  outline: none; /* el foco se muestra en todo el campo */
}
.campo input::placeholder {
  color: var(--ink-muted);
  font-size: 14px;
  font-weight: 400;
  letter-spacing: 0;
}

.btn-icono {
  display: grid;
  place-items: center;
  padding: 0.5rem;
  border-radius: 0.5rem;
  background: transparent;
  color: var(--ink-muted);
  transition: background 0.2s, color 0.2s;
}
.btn-icono:hover {
  background: var(--muted);
  color: var(--ink);
}
.btn-limpiar:hover {
  color: var(--error);
}

.pruebas {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem;
}
.pastilla {
  padding: 2px 0.5rem;
  border-radius: 999px;
  background: var(--surface);
  color: var(--ink);
  font-size: 13px;
  line-height: 18px;
  transition: background 0.2s;
}
.pastilla:hover {
  background: var(--muted);
}

.nota {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.375rem 0.5rem;
  border-radius: 0.5rem;
  background: var(--subtle);
  color: var(--ink-muted);
  font-size: 12px;
  line-height: 16px;
}
.nota svg {
  flex-shrink: 0;
  color: var(--secondary);
}

/* Estado vacío */
.vacio {
  align-items: center;
  padding: 2.5rem;
  text-align: center;
}
.vacio-icono {
  display: grid;
  place-items: center;
  width: 64px;
  height: 64px;
  border-radius: 999px;
  background: var(--subtle);
  color: var(--ink-muted);
}
.vacio h3 {
  font-size: 20px;
  line-height: 28px;
  font-weight: 600;
}
.vacio p {
  max-width: 28rem;
  font-size: 14px;
  line-height: 20px;
}

/* Fortaleza */
.badge {
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}
.badge.muy-fuerte,
.badge.fuerte {
  background: var(--secondary-container);
  color: var(--on-secondary-container);
}
.badge.aceptable {
  background: var(--tertiary-container);
  color: var(--tertiary);
}
.badge.debil {
  background: var(--error-container);
  color: var(--on-error-container);
}

.medidor {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
  height: 10px;
}
.medidor span {
  border-radius: 999px;
  background: var(--muted);
  transition: background 0.3s;
}
.medidor span.muy-fuerte,
.medidor span.fuerte {
  background: var(--secondary);
}
.medidor span.aceptable {
  background: var(--tertiary);
}
.medidor span.debil {
  background: var(--error);
}

.tiempo {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem;
  border-radius: 0.75rem;
  background: var(--subtle);
}
.tiempo-valor {
  margin: 2px 0;
  font-size: 28px;
  line-height: 36px;
  font-weight: 700;
  color: var(--ink);
}
.metricas {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.25rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  background: var(--card);
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
  font-size: 13px;
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.chip {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.25rem 0.5rem;
  border-radius: 0.5rem;
  background: var(--subtle);
  color: var(--ink-muted);
  font-size: 12px;
  line-height: 16px;
}
.chip strong {
  font-weight: 500;
  color: var(--ink);
}
.chip svg {
  flex-shrink: 0;
}
.chip svg.ok {
  color: var(--secondary);
}
.chip svg.aviso {
  color: var(--tertiary);
}
.chip svg.mal {
  color: var(--error);
}

/* Alertas (filtraciones y patrones) */
.alerta {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 1rem;
  border-radius: 0.75rem;
}
.alerta-icono {
  display: grid;
  place-items: center;
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  border-radius: 999px;
}
.alerta-titulo {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 2px;
  font-size: 16px;
  line-height: 24px;
  font-weight: 600;
}
.alerta .texto-suave {
  font-size: 14px;
  line-height: 20px;
}
.etiqueta-estado {
  padding: 2px 0.5rem;
  border-radius: 999px;
  font-size: 11px;
  line-height: 14px;
  font-weight: 500;
}

.alerta.pendiente {
  background: var(--subtle);
}
.alerta.pendiente .alerta-icono {
  background: var(--muted);
  color: var(--ink-muted);
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 999px;
  animation: girar 0.8s linear infinite;
}
@keyframes girar {
  to {
    transform: rotate(360deg);
  }
}

.alerta.comprometida {
  background: color-mix(in srgb, var(--error-container) 25%, transparent);
}
.alerta.comprometida .alerta-icono {
  background: var(--error);
  color: var(--card);
}
.alerta.comprometida .alerta-titulo {
  color: var(--error);
}
.alerta.comprometida .etiqueta-estado {
  background: var(--error);
  color: var(--card);
}

.alerta.limpia {
  background: color-mix(in srgb, var(--secondary-container) 30%, transparent);
}
.alerta.limpia .alerta-icono {
  background: var(--secondary);
  color: var(--card);
}
.alerta.limpia .alerta-titulo {
  color: var(--secondary);
}
.alerta.limpia .etiqueta-estado {
  background: var(--secondary-container);
  color: var(--on-secondary-container);
}

.explicacion {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 1rem;
  border-radius: 0.75rem;
  background: var(--subtle);
}
.explicacion svg {
  flex-shrink: 0;
  margin-top: 2px;
  color: var(--primary);
}
.explicacion p {
  margin-top: 0.25rem;
  line-height: 1.6;
}

/* Consejos */
.consejos {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.5rem;
}
.consejo {
  padding: 1rem;
  border-radius: 0.75rem;
  background: var(--subtle);
}
.consejo h3 {
  margin: 0.5rem 0 0.25rem;
  font-size: 14px;
  line-height: 20px;
  font-weight: 600;
}
.numero {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary) 15%, transparent);
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
}

.llamada {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 0.5rem;
}
.btn-primario {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  height: 44px;
  padding: 0 1.5rem;
  border-radius: 0.5rem;
  background: var(--primary);
  color: var(--on-primary);
  font-weight: 600;
  transition: background 0.2s, transform 0.1s;
}
.btn-primario:hover {
  background: var(--primary-strong);
}
.btn-primario:active {
  transform: scale(0.95);
}

@media (max-width: 768px) {
  .tarjeta {
    padding: 1rem;
  }
  .cabecera h1 {
    font-size: 26px;
    line-height: 34px;
  }
  .consejos {
    grid-template-columns: 1fr;
  }
  .metricas {
    align-items: flex-start;
  }
  .tiempo-valor {
    font-size: 22px;
    line-height: 30px;
  }
  .btn-primario {
    width: 100%;
    justify-content: center;
  }
}
@media (prefers-reduced-motion: reduce) {
  .campo,
  .btn-icono,
  .pastilla,
  .medidor span,
  .btn-primario {
    transition: none;
  }
  .spinner {
    animation-duration: 2.4s;
  }
}
</style>
