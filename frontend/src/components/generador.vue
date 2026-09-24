<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

// Mismos conjuntos que generadorContraseña.js
const mayusculas = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
const minusculas = 'abcdefghijklmnopqrstuvwxyz';
const numeros = '0123456789';
const simbolos = '!@#$%^&*()-+';

const MIN_LONGITUD = 8;
const MAX_LONGITUD = 128;

const longitud = ref(16);
const contrasenaGenerada = ref('');

// Filtros: qué tipos de carácter entran en la contraseña
type TipoConjunto = 'minusculas' | 'mayusculas' | 'numeros' | 'simbolos';

const CONJUNTOS: { tipo: TipoConjunto; nombre: string; caracteres: string; muestra: string }[] = [
  { tipo: 'minusculas', nombre: 'Minúsculas', caracteres: minusculas, muestra: 'a b c d e f g …' },
  { tipo: 'mayusculas', nombre: 'Mayúsculas', caracteres: mayusculas, muestra: 'A B C D E F G …' },
  { tipo: 'numeros', nombre: 'Números', caracteres: numeros, muestra: '0 1 2 3 4 5 6 7 8 9' },
  { tipo: 'simbolos', nombre: 'Símbolos', caracteres: simbolos, muestra: '! @ # $ % ^ & * ( ) - +' },
];

const usar = ref<Record<TipoConjunto, boolean>>({
  minusculas: true,
  mayusculas: true,
  numeros: true,
  simbolos: true,
});

const conjuntosActivos = computed(() => CONJUNTOS.filter((c) => usar.value[c.tipo]).map((c) => c.caracteres));

// Siempre debe quedar al menos un tipo activo
function alternarConjunto(tipo: TipoConjunto) {
  if (usar.value[tipo] && conjuntosActivos.value.length === 1) return;
  usar.value[tipo] = !usar.value[tipo];
  generarContrasena();
}

// Para cada carácter se elige primero un tipo activo y luego un carácter de ese tipo
function generarContrasena() {
  const activos = conjuntosActivos.value;
  let resultado = '';
  for (let i = 0; i < longitud.value; i++) {
    const conjunto = activos[Math.floor(Math.random() * activos.length)]!;
    resultado += conjunto[Math.floor(Math.random() * conjunto.length)];
  }
  contrasenaGenerada.value = resultado;
  return resultado;
}

function setLongitud(valor: string | number) {
  const n = parseInt(String(valor), 10);
  longitud.value = Math.max(MIN_LONGITUD, Math.min(MAX_LONGITUD, Number.isNaN(n) ? 16 : n));
  generarContrasena();
}

// Corrige en el campo los valores fuera de rango
function onLongitudNumero(event: Event) {
  const input = event.target as HTMLInputElement;
  setLongitud(input.value);
  input.value = String(longitud.value);
}

// Colorea cada carácter según su tipo
const caracteres = computed(() =>
  [...contrasenaGenerada.value].map((char) => {
    let tipo = 'minuscula';
    if (numeros.includes(char)) tipo = 'numero';
    else if (simbolos.includes(char)) tipo = 'simbolo';
    else if (mayusculas.includes(char)) tipo = 'mayuscula';
    return { char, tipo };
  })
);

// Entropía por carácter: primero se elige el tipo (1/n) y luego un carácter del conjunto
const bitsPorCaracter = computed(() => {
  const activos = conjuntosActivos.value;
  return Math.log2(activos.length) + activos.reduce((acc, c) => acc + Math.log2(c.length) / activos.length, 0);
});

const entropia = computed(() => Math.round(longitud.value * bitsPorCaracter.value));

// Niveles que devuelve el backend (obtenerNivel) y cómo se pintan
const NIVELES_SERVIDOR: Record<string, { n: number; etiqueta: string; clase: string; tiempo: string }> = {
  'Débil': { n: 1, etiqueta: 'Débil', clase: 'debil', tiempo: 'Minutos u horas' },
  'Media': { n: 2, etiqueta: 'Media', clase: 'aceptable', tiempo: 'Días o semanas' },
  'Fuerte': { n: 3, etiqueta: 'Fuerte', clase: 'fuerte', tiempo: 'Varios años' },
  'Muy fuerte': { n: 4, etiqueta: 'Muy fuerte', clase: 'muy-fuerte', tiempo: 'Siglos mediante fuerza bruta' },
};

const nivelServidor = ref<string | null>(null);
let evaluarTimer: ReturnType<typeof setTimeout> | undefined;
let ultimaPeticion = 0;

async function evaluarEnServidor(contrasena: string) {
  const peticion = ++ultimaPeticion;
  try {
    const respuesta = await fetch('/api/nivelSeguridad/comprobar', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contrasena,
        usarMayusculas: usar.value.mayusculas,
        usarMinusculas: usar.value.minusculas,
        usarNumeros: usar.value.numeros,
        usarSimbolos: usar.value.simbolos,
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

// Espera 300 ms sin cambios para no lanzar una petición por cada movimiento del slider
watch(contrasenaGenerada, (contrasena) => {
  clearTimeout(evaluarTimer);
  evaluarTimer = setTimeout(() => evaluarEnServidor(contrasena), 300);
});

const nivel = computed(() => {
  const servidor = nivelServidor.value ? NIVELES_SERVIDOR[nivelServidor.value] : undefined;
  if (servidor) return servidor;

  const e = entropia.value;
  if (e >= 80) return { n: 4, etiqueta: 'Muy fuerte', clase: 'muy-fuerte', tiempo: 'Siglos mediante fuerza bruta' };
  if (e >= 60) return { n: 3, etiqueta: 'Fuerte', clase: 'fuerte', tiempo: 'Varios años' };
  if (e >= 40) return { n: 2, etiqueta: 'Aceptable', clase: 'aceptable', tiempo: 'Días o semanas' };
  return { n: 1, etiqueta: 'Débil', clase: 'debil', tiempo: 'Minutos u horas' };
});

// Copiar al portapapeles
const toastVisible = ref(false);
let toastTimer: ReturnType<typeof setTimeout> | undefined;

async function copiar() {
  if (!contrasenaGenerada.value) return;
  try {
    await navigator.clipboard.writeText(contrasenaGenerada.value);
  } catch {
    const textarea = document.createElement('textarea');
    textarea.value = contrasenaGenerada.value;
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
  }
  toastVisible.value = true;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (toastVisible.value = false), 2500);
}

// Giro del icono al regenerar
const girando = ref(false);

function regenerar() {
  girando.value = true;
  setTimeout(() => (girando.value = false), 300);
  generarContrasena();
}

// Barra espaciadora para regenerar, solo si no hay ningún control enfocado
function onKeydown(event: KeyboardEvent) {
  if (event.code === 'Space' && event.target === document.body) {
    event.preventDefault();
    regenerar();
  }
}

onMounted(() => {
  generarContrasena();
  window.addEventListener('keydown', onKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeydown);
  clearTimeout(toastTimer);
  clearTimeout(evaluarTimer);
});
</script>

<template>
  <div class="generador">
    <!-- Aviso de copiado -->
    <div class="toast" :class="{ visible: toastVisible }" role="status" aria-live="polite">
      <svg viewBox="0 0 24 24" width="20" height="20" class="toast-icono" aria-hidden="true">
        <path fill="currentColor" d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm-1.2 14.2-4-4 1.4-1.4 2.6 2.6 5.6-5.6 1.4 1.4-7 7Z" />
      </svg>
      <div>
        <strong>¡Copiada!</strong>
        <span>Contraseña en el portapapeles</span>
      </div>
    </div>

    <!-- Cabecera -->
    <header class="cabecera">
      <h1>Genera una contraseña</h1>
      <p>Se crea en tu navegador y no se envía a ningún servidor.</p>
    </header>

    <!-- Tarjeta principal -->
    <section class="tarjeta" aria-label="Generador de contraseñas">
      <!-- Resultado -->
      <div class="bloque">
        <span class="titulo-seccion">Tu contraseña</span>
        <div class="resultado">
          <output class="contrasena" aria-label="Contraseña generada">
            <span v-for="(item, i) in caracteres" :key="i" :class="item.tipo">{{ item.char }}</span>
          </output>
          <div class="acciones">
            <button
              type="button"
              class="btn-icono"
              aria-label="Generar otra contraseña"
              title="Generar otra (barra espaciadora)"
              @click="regenerar"
            >
              <svg viewBox="0 0 24 24" width="22" height="22" :class="{ girando }" aria-hidden="true">
                <path fill="currentColor" d="M12 6V3L8 7l4 4V8a4 4 0 0 1 3.9 4.9l1.5 1.5A6 6 0 0 0 12 6Zm-3.9 4.1L6.6 8.6A6 6 0 0 0 12 18v3l4-4-4-4v3a4 4 0 0 1-3.9-4.9Z" />
              </svg>
            </button>
            <button type="button" class="btn-primario" :disabled="!contrasenaGenerada" @click="copiar">
              <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                <path fill="currentColor" d="M16 1H4a2 2 0 0 0-2 2v14h2V3h12V1Zm3 4H8a2 2 0 0 0-2 2v14c0 1.1.9 2 2 2h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Zm0 16H8V7h11v14Z" />
              </svg>
              Copiar
            </button>
          </div>
        </div>
      </div>

      <!-- Nivel de seguridad -->
      <div class="panel fortaleza">
        <div class="fila">
          <div class="fila-izq">
            <span class="titulo-seccion">Nivel de seguridad:</span>
            <span class="badge" :class="nivel.clase">{{ nivel.etiqueta }}</span>
          </div>
          <span class="mono">{{ entropia }} bits de entropía</span>
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
        <div class="fila">
          <span class="texto-suave">Tiempo estimado para descifrar:</span>
          <strong :class="['tiempo', nivel.clase]">{{ nivel.tiempo }}</strong>
        </div>
      </div>

      <!-- Opciones -->
      <div class="opciones">
        <!-- Longitud -->
        <div class="panel">
          <div class="fila">
            <label for="longitud" class="titulo-seccion">Longitud</label>
            <div class="campo-longitud">
              <input
                type="number"
                aria-label="Número de caracteres"
                :min="MIN_LONGITUD"
                :max="MAX_LONGITUD"
                :value="longitud"
                @change="onLongitudNumero"
              />
              <span class="texto-suave">caracteres</span>
            </div>
          </div>
          <div class="slider">
            <button type="button" class="btn-paso" aria-label="Reducir longitud" @click="setLongitud(longitud - 1)">−</button>
            <input
              id="longitud"
              type="range"
              :min="MIN_LONGITUD"
              :max="MAX_LONGITUD"
              :value="longitud"
              @input="setLongitud(($event.target as HTMLInputElement).value)"
            />
            <button type="button" class="btn-paso" aria-label="Aumentar longitud" @click="setLongitud(longitud + 1)">+</button>
          </div>
          <div class="marcas">
            <span>{{ MIN_LONGITUD }} mínimo</span>
            <span>16 recomendado</span>
            <span>{{ MAX_LONGITUD }} máximo</span>
          </div>
        </div>

        <!-- Conjuntos usados -->
        <div class="conjuntos">
          <span class="titulo-seccion">Caracteres que se usan</span>
          <ul>
            <li v-for="c in CONJUNTOS" :key="c.tipo">
              <button
                type="button"
                class="filtro"
                :class="{ activo: usar[c.tipo] }"
                :aria-pressed="usar[c.tipo]"
                :disabled="usar[c.tipo] && conjuntosActivos.length === 1"
                :title="usar[c.tipo] && conjuntosActivos.length === 1 ? 'Debe quedar al menos un tipo' : undefined"
                @click="alternarConjunto(c.tipo)"
              >
                <span class="filtro-cabecera">
                  <span>{{ c.nombre }}</span>
                  <span class="check" aria-hidden="true">
                    <svg v-if="usar[c.tipo]" viewBox="0 0 24 24" width="14" height="14">
                      <path fill="currentColor" d="M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2Z" />
                    </svg>
                  </span>
                </span>
                <code>{{ c.muestra }}</code>
              </button>
            </li>
          </ul>
        </div>
      </div>

      <!-- Nota de privacidad -->
      <div class="panel nota">
        <strong>Tu contraseña no sale de este dispositivo</strong>
        <p class="texto-suave">
          Se genera en tu navegador. No viaja por internet ni se guarda en cookies ni en almacenamiento local.
        </p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.generador {
  grid-column: 1 / -1; /* ocupa todo el ancho aunque #app sea una rejilla */
  width: 100%;
  max-width: 1040px;
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
code,
.contrasena,
.campo-longitud input {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
}

.cabecera h1 {
  margin: 0;
  font-size: 32px;
  line-height: 40px;
  font-weight: 600;
  letter-spacing: -0.01em;
  color: var(--ink);
}
.cabecera p {
  margin: 0.25rem 0 0;
  font-size: 16px;
  line-height: 24px;
  color: var(--ink-muted);
}

.tarjeta {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  padding: 2.5rem;
  border-radius: 0.75rem;
  background: var(--card);
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
}

.bloque {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.titulo-seccion {
  font-weight: 600;
  color: var(--ink);
}
.texto-suave {
  color: var(--ink-muted);
  font-size: 12px;
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 1rem;
  border-radius: 0.75rem;
  background: var(--subtle);
}

.fila {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
.fila-izq {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Resultado */
.resultado {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.5rem;
  border-radius: 0.75rem;
  background: var(--subtle);
}
.contrasena {
  flex: 1;
  min-width: 0;
  font-size: 28px;
  line-height: 1.6;
  font-weight: 500;
  letter-spacing: 0.05em;
  word-break: break-all;
  user-select: all;
}
.contrasena .minuscula { color: var(--ink-muted); }
.contrasena .mayuscula { color: var(--ink); font-weight: 600; }
.contrasena .numero { color: var(--primary); font-weight: 600; }
.contrasena .simbolo { color: var(--secondary); font-weight: 700; }

.acciones {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  flex-shrink: 0;
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

.btn-icono {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border-radius: 0.5rem;
  background: var(--surface);
  color: var(--ink);
  transition: background 0.2s;
}
.btn-icono:hover { background: var(--muted); }
.btn-icono svg { transition: transform 0.3s; }
.btn-icono svg.girando { transform: rotate(180deg); }

.btn-primario {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  height: 44px;
  padding: 0 1.5rem;
  border-radius: 0.5rem;
  background: var(--primary);
  color: var(--on-primary);
  font-weight: 600;
  transition: background 0.2s, transform 0.1s;
}
.btn-primario:hover { background: var(--primary-strong); }
.btn-primario:active { transform: scale(0.95); }
.btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }

/* Fortaleza */
.badge {
  padding: 2px 0.5rem;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}
.badge.muy-fuerte, .badge.fuerte { background: var(--secondary-container); color: var(--on-secondary-container); }
.badge.aceptable { background: var(--tertiary-container); color: var(--tertiary); }
.badge.debil { background: var(--error-container); color: var(--on-error-container); }

.mono { font-size: 13px; color: var(--ink); }

.medidor {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
  height: 8px;
}
.medidor span {
  border-radius: 999px;
  background: var(--muted);
  transition: background 0.3s;
}
.medidor span.muy-fuerte, .medidor span.fuerte { background: var(--secondary); }
.medidor span.aceptable { background: var(--tertiary); }
.medidor span.debil { background: var(--error); }

.tiempo.muy-fuerte, .tiempo.fuerte { color: var(--secondary); }
.tiempo.aceptable { color: var(--tertiary); }
.tiempo.debil { color: var(--error); }

/* Opciones */
.opciones {
  display: grid;
  grid-template-columns: 5fr 7fr;
  gap: 1.5rem;
  align-items: start;
}

.campo-longitud {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  border-radius: 0.5rem;
  background: var(--card);
}
.campo-longitud input {
  width: 3rem;
  border: none;
  background: transparent;
  text-align: center;
  font-size: 18px;
  font-weight: 600;
  color: var(--primary);
}

.slider {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.slider input {
  flex: 1;
  accent-color: var(--primary);
  cursor: pointer;
}
.btn-paso {
  width: 32px;
  height: 32px;
  border-radius: 0.5rem;
  background: var(--surface);
  color: var(--ink);
  font-size: 18px;
  font-weight: 700;
}
.btn-paso:hover { background: var(--muted); }

.marcas {
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: var(--ink-muted);
}

.conjuntos {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.conjuntos ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.25rem;
}
.conjuntos li {
  display: flex;
}
.conjuntos code {
  font-size: 13px;
  color: var(--ink-muted);
}

/* Filtros de tipo de carácter */
.filtro {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.5rem;
  border-radius: 0.5rem;
  border: 2px solid transparent;
  background: var(--subtle);
  color: var(--ink);
  text-align: left;
  opacity: 0.55;
  transition: background 0.2s, border-color 0.2s, opacity 0.2s;
}
.filtro:hover:not(:disabled) { background: var(--muted); }
.filtro.activo {
  border-color: var(--primary);
  opacity: 1;
}
.filtro:not(.activo) code { text-decoration: line-through; }
.filtro:disabled { cursor: not-allowed; }

.filtro-cabecera {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-weight: 500;
}
.check {
  display: grid;
  place-items: center;
  width: 18px;
  height: 18px;
  border-radius: 4px;
  border: 1.5px solid var(--ink-muted);
}
.filtro.activo .check {
  background: var(--primary);
  border-color: var(--primary);
  color: var(--on-primary);
}

.nota p { margin: 0; line-height: 1.6; }

/* Toast */
.toast {
  position: fixed;
  top: 1.5rem;
  right: 1.5rem;
  z-index: 50;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 0.75rem;
  background: var(--card);
  color: var(--ink);
  border: 1px solid var(--muted);
  box-shadow: 0 8px 24px rgb(0 0 0 / 0.12);
  opacity: 0;
  transform: translateY(-20px);
  pointer-events: none;
  transition: opacity 0.3s, transform 0.3s;
}
.toast.visible {
  opacity: 1;
  transform: translateY(0);
}
.toast div { display: flex; flex-direction: column; }
.toast span { font-size: 12px; color: var(--ink-muted); }
.toast-icono { color: var(--secondary); }

@media (max-width: 768px) {
  .tarjeta { padding: 1rem; }
  .resultado { flex-direction: column; align-items: stretch; padding: 1rem; }
  .acciones { align-self: flex-end; }
  .contrasena { font-size: 22px; }
  .opciones { grid-template-columns: 1fr; }
  .cabecera h1 { font-size: 26px; line-height: 34px; }
}
@media (max-width: 480px) {
  .conjuntos ul { grid-template-columns: 1fr; }
}
@media (prefers-reduced-motion: reduce) {
  .btn-icono svg, .toast, .medidor span, .filtro { transition: none; }
}
</style>
