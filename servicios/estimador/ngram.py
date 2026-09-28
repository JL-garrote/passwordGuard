"""
ngram.py — modelo de Markov de caracteres + estimación Monte Carlo.

Lo usan tanto entrenar.py (para construir y guardar el modelo) como main.py
(para cargarlo y responder). Aquí no se lee ni se escribe ninguna contraseña
de usuario en disco ni en logs.

Referencia de la estimación: Dell'Amico & Filippone (2015),
"Monte Carlo Strength Evaluation: Fast and Reliable Password Checking".
"""

from __future__ import annotations

import bisect
import gzip
import math
import pickle
import random
from collections import Counter, defaultdict
from typing import Dict, Iterable, List, Tuple

# Alfabeto: los 95 caracteres ASCII imprimibles (del espacio a la tilde).
ALFABETO = "".join(chr(i) for i in range(32, 127))
ALFABETO_SET = frozenset(ALFABETO)

# Marcadores de control: fuera del alfabeto para que no se confundan con
# caracteres reales. INICIO rellena el contexto inicial; FIN también se
# predice, así el modelo aprende cuándo suelen acabar las contraseñas.
INICIO = "\x02"
FIN = "\x03"
VOCABULARIO = ALFABETO + FIN
TAM_VOCABULARIO = len(VOCABULARIO)

# Tope de seguridad al muestrear, por si una cadena no llega nunca a FIN.
MAX_LONGITUD_MUESTRA = 64

# Límite de bits al convertir a número de intentos, para no desbordar el float.
MAX_BITS = 900.0


class ModeloMarkov:
    def __init__(self, orden: int = 3, alpha: float = 0.01) -> None:
        if orden < 1:
            raise ValueError("El orden debe ser al menos 1.")
        if alpha <= 0:
            raise ValueError("alpha debe ser positivo (si no, habría probabilidades 0).")
        self.orden = orden
        self.alpha = alpha
        self.conteos: Dict[str, Counter] = {}
        self.totales: Dict[str, int] = {}
        # Tabla de Monte Carlo: bits de cada muestra (ordenados de más a menos
        # probable) y la suma acumulada de 1 / (n * p_i).
        self._bits_muestras: List[float] = []
        self._acumulado: List[float] = []
        # Caché para muestrear rápido: por contexto, caracteres y conteos acumulados.
        self._cache_muestreo: Dict[str, Tuple[str, List[int]]] = {}

    # ------------------------------------------------------------------
    # Entrenamiento
    # ------------------------------------------------------------------
    def _preparar(self, contrasena: str) -> str:
        return INICIO * self.orden + contrasena + FIN

    def entrenar(self, contrasenas: Iterable[str]) -> None:
        """Cuenta, para cada contexto de 'orden' caracteres, qué carácter le sigue."""
        conteos: Dict[str, Counter] = defaultdict(Counter)
        for pw in contrasenas:
            s = self._preparar(pw)
            for i in range(self.orden, len(s)):
                conteos[s[i - self.orden:i]][s[i]] += 1
        self.conteos = dict(conteos)
        self.totales = {ctx: sum(c.values()) for ctx, c in self.conteos.items()}
        self._bits_muestras = []
        self._acumulado = []
        self._cache_muestreo = {}

    def num_contextos(self) -> int:
        return len(self.conteos)

    # ------------------------------------------------------------------
    # Probabilidad
    # ------------------------------------------------------------------
    def _log2_prob_paso(self, contexto: str, caracter: str) -> float:
        """log2 P(caracter | contexto) con suavizado de Laplace."""
        contador = self.conteos.get(contexto)
        visto = contador.get(caracter, 0) if contador else 0
        total = self.totales.get(contexto, 0)
        return math.log2((visto + self.alpha) / (total + self.alpha * TAM_VOCABULARIO))

    def log2_prob(self, contrasena: str) -> float:
        """Suma de log2 de cada paso (incluido FIN). Nunca se multiplica: underflow."""
        s = self._preparar(contrasena)
        return sum(
            self._log2_prob_paso(s[i - self.orden:i], s[i])
            for i in range(self.orden, len(s))
        )

    def bits(self, contrasena: str) -> float:
        """Dificultad en bits: -log2 P. Cuantos más bits, más difícil de adivinar."""
        return -self.log2_prob(contrasena)

    # ------------------------------------------------------------------
    # Muestreo
    # ------------------------------------------------------------------
    def _siguiente(self, contexto: str, rnd: random.Random) -> str:
        """
        Elige el siguiente carácter según P(c | contexto) con Laplace.

        Truco para no recorrer los 96 símbolos en cada paso: la distribución
        suavizada es una mezcla exacta de dos:
          - con prob. N / (N + alpha·V): según los conteos observados;
          - con prob. alpha·V / (N + alpha·V): uniforme sobre el vocabulario.
        """
        total = self.totales.get(contexto, 0)
        peso_uniforme = self.alpha * TAM_VOCABULARIO
        if total == 0 or rnd.random() * (total + peso_uniforme) < peso_uniforme:
            return VOCABULARIO[rnd.randrange(TAM_VOCABULARIO)]

        cache = self._cache_muestreo.get(contexto)
        if cache is None:
            caracteres = "".join(self.conteos[contexto].keys())
            acumulados: List[int] = []
            suma = 0
            for c in caracteres:
                suma += self.conteos[contexto][c]
                acumulados.append(suma)
            cache = (caracteres, acumulados)
            self._cache_muestreo[contexto] = cache
        caracteres, acumulados = cache
        return caracteres[bisect.bisect_right(acumulados, rnd.randrange(total))]

    def generar(self, rnd: random.Random) -> Tuple[str, float]:
        """Genera una contraseña con el modelo. Devuelve (contraseña, bits)."""
        contexto = INICIO * self.orden
        generada: List[str] = []
        log2_p = 0.0
        while len(generada) < MAX_LONGITUD_MUESTRA:
            c = self._siguiente(contexto, rnd)
            log2_p += self._log2_prob_paso(contexto, c)
            if c == FIN:
                break
            generada.append(c)
            contexto = (contexto + c)[-self.orden:]
        return "".join(generada), -log2_p

    # ------------------------------------------------------------------
    # Monte Carlo
    # ------------------------------------------------------------------
    def construir_tabla_montecarlo(self, n: int, semilla: int = 1234) -> List[Tuple[str, float]]:
        """
        Genera n muestras y prepara la tabla de consulta.
        Devuelve 20 ejemplos (contraseña, probabilidad) para revisarlos a ojo.
        """
        if not self.conteos:
            raise RuntimeError("Entrena el modelo antes de construir la tabla.")
        rnd = random.Random(semilla)
        ejemplos: List[Tuple[str, float]] = []
        bits_muestras: List[float] = []
        for i in range(n):
            pw, b = self.generar(rnd)
            bits_muestras.append(b)
            if i < 20:
                ejemplos.append((pw, 2.0 ** -b))

        # Menos bits = más probable = el atacante la prueba antes.
        bits_muestras.sort()
        acumulado: List[float] = []
        suma = 0.0
        for b in bits_muestras:
            # 1 / (n · p_i) con p_i = 2^-b  ->  2^b / n. Se acota b para que una
            # muestra rarísima no desborde el float (2^1024 ya da OverflowError).
            suma += 2.0 ** min(b, MAX_BITS) / n
            acumulado.append(suma)
        self._bits_muestras = bits_muestras
        self._acumulado = acumulado
        return ejemplos

    def estimar_intentos(self, contrasena: str) -> float:
        """
        Número de intentos estimado: suma de 1 / (n · p_i) de las muestras
        más probables que la contraseña. Búsqueda binaria: milisegundos.
        """
        if not self._acumulado:
            raise RuntimeError("Falta la tabla de Monte Carlo (construir_tabla_montecarlo).")
        k = bisect.bisect_left(self._bits_muestras, self.bits(contrasena))
        # +1: aunque ninguna muestra sea más probable, hace falta al menos un intento.
        return 1.0 + (self._acumulado[k - 1] if k > 0 else 0.0)

    # ------------------------------------------------------------------
    # Persistencia
    # ------------------------------------------------------------------
    def guardar(self, ruta: str) -> None:
        datos = {
            "orden": self.orden,
            "alpha": self.alpha,
            "conteos": {ctx: dict(c) for ctx, c in self.conteos.items()},
            "bits_muestras": self._bits_muestras,
            "acumulado": self._acumulado,
        }
        with gzip.open(ruta, "wb") as f:
            pickle.dump(datos, f, protocol=pickle.HIGHEST_PROTOCOL)

    @classmethod
    def cargar(cls, ruta: str) -> "ModeloMarkov":
        # pickle solo es seguro con ficheros propios: este lo genera entrenar.py.
        with gzip.open(ruta, "rb") as f:
            datos = pickle.load(f)
        modelo = cls(orden=datos["orden"], alpha=datos["alpha"])
        modelo.conteos = {ctx: Counter(c) for ctx, c in datos["conteos"].items()}
        modelo.totales = {ctx: sum(c.values()) for ctx, c in modelo.conteos.items()}
        modelo._bits_muestras = datos["bits_muestras"]
        modelo._acumulado = datos["acumulado"]
        return modelo
