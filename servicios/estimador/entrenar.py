"""
entrenar.py — construye el modelo de fortaleza de contraseñas de principio a fin.

Sigue las 5 fases del plan:

    limpiar -> dividir -> contar -> probabilidad -> comprobar a mano ->
    muestrear -> comprobar muestras -> evaluar -> ajustar -> guardar

Uso típico
----------
    # entrenar el modelo por defecto (orden 3, alpha 0.01) y guardarlo:
    python entrenar.py

    # comparar configuraciones (fase 5, paso 18) sobre una muestra:
    python entrenar.py --comparar

    # ajustar parámetros / tamaño:
    python entrenar.py --orden 4 --alpha 0.001 --muestras 50000

Solo cuando esto funciona pasas a main.py con FastAPI.
"""

from __future__ import annotations

import argparse
import math
import os
import random
from typing import List, Tuple

from ngram import ALFABETO_SET, FIN, ModeloMarkov

RUTA_LISTA_POR_DEFECTO = os.path.join(
    os.path.dirname(__file__), "datos", "10_million_password_list_top_1000000.txt"
)
CARPETA_MODELO = os.path.join(os.path.dirname(__file__), "modelo")
SEMILLA_DIVISION = 42
MAX_LONGITUD = 32

def cargar_y_limpiar(ruta: str, limite: int | None = None) -> List[str]:
    """Lee el fichero con cuidado con la codificación y limpia cada línea."""
    contrasenas: List[str] = []
    with open(ruta, "r", encoding="utf-8", errors="replace") as f:
        for linea in f:
            pw = linea.rstrip("\n").rstrip("\r")
            if not pw:
                continue 
            if len(pw) > MAX_LONGITUD:
                continue  
            if not all(c in ALFABETO_SET for c in pw):
                continue
            contrasenas.append(pw)
            if limite is not None and len(contrasenas) >= limite:
                break
    return contrasenas


def dividir(contrasenas: List[str]) -> Tuple[List[str], List[str]]:
    """Baraja con semilla fija y divide 90/10. El 10% no se toca hasta la fase 5."""
    datos = list(contrasenas)
    random.Random(SEMILLA_DIVISION).shuffle(datos)
    corte = int(len(datos) * 0.9)
    return datos[:corte], datos[corte:]


def comprobacion_fase1(train: List[str], test: List[str]) -> None:
    print("\n=== Fase 1: datos ===")
    print(f"Contraseñas tras limpiar: {len(train) + len(test):,}")
    print(f"  entrenamiento (90%): {len(train):,}")
    print(f"  prueba        (10%): {len(test):,}")
    r = random.Random(0)
    print("  ejemplos train:", r.sample(train, min(5, len(train))))
    print("  ejemplos test :", r.sample(test, min(5, len(test))))

def comprobacion_fase2(modelo: ModeloMarkov) -> None:
    print("\n=== Fase 2: entrenamiento ===")
    print(f"Contextos distintos (orden {modelo.orden}): {modelo.num_contextos():,}")
    ctx = ("123")[-modelo.orden:] if modelo.orden <= 3 else "123"
    contador = modelo.conteos.get(ctx)
    if contador:
        top = contador.most_common(5)
        legible = [("<FIN>" if s == FIN else s, n) for s, n in top]
        print(f"  tras {ctx!r} lo más probable: {legible}")
    else:
        print(f"  (el contexto {ctx!r} no aparece con este orden)")

def comprobacion_fase3(modelo: ModeloMarkov) -> None:
    print("\n=== Fase 3: bits (dificultad) ===")
    ejemplos = ["123456", "password", "Barcelona2023!", "xK9#qL2!vT"]
    filas = sorted(((modelo.bits(pw), pw) for pw in ejemplos))
    for bits, pw in filas:
        print(f"  {bits:8.2f} bits  {pw}")
    print("  (esperado: 123456/password pocos bits, la aleatoria muchos)")

def comprobacion_fase4(modelo: ModeloMarkov, muestras: int) -> None:
    print(f"\n=== Fase 4: Monte Carlo ({muestras:,} muestras) ===")
    ejemplos = modelo.construir_tabla_montecarlo(muestras, semilla=1234)
    print("  20 muestras generadas (deberían parecer contraseñas reales):")
    for pw, p in ejemplos:
        print(f"    p={p:.3e}  {pw!r}")

    # Repetir con otra semilla: las estimaciones deben ser parecidas.
    prueba = "monkey123"
    est1 = modelo.estimar_intentos(prueba)
    modelo.construir_tabla_montecarlo(muestras, semilla=9999)
    est2 = modelo.estimar_intentos(prueba)
    # dejamos la tabla con la semilla estándar
    modelo.construir_tabla_montecarlo(muestras, semilla=1234)
    print(f"  estabilidad para {prueba!r}: {est1:.3e} vs {est2:.3e} intentos")

UMBRALES = [1e6, 1e9, 1e12]

def curva_de_adivinacion(modelo: ModeloMarkov, test: List[str],
                         limite_test: int | None = None) -> List[float]:
    """% del test que se adivinaría en menos de 10^6, 10^9 y 10^12 intentos."""
    datos = test if limite_test is None else test[:limite_test]
    contadores = [0] * len(UMBRALES)
    for pw in datos:
        intentos = modelo.estimar_intentos(pw)
        for i, u in enumerate(UMBRALES):
            if intentos < u:
                contadores[i] += 1
    return [100.0 * c / len(datos) for c in contadores]


def comprobacion_fase5(modelo: ModeloMarkov, test: List[str],
                       limite_test: int | None) -> None:
    print("\n=== Fase 5: curva de adivinación (config actual) ===")
    pct = curva_de_adivinacion(modelo, test, limite_test)
    for u, p in zip(UMBRALES, pct):
        print(f"  < 10^{int(round(math.log10(u))):>2}: {p:6.2f}% adivinado")


def comparar_configuraciones(train: List[str], test: List[str],
                             muestras: int, limite_test: int) -> None:
    """Paso 18: repite con orden 2/3/4 y alpha 0.001/0.01/0.1."""
    print("\n=== Fase 5: comparación de configuraciones ===")
    print(f"(muestras MC={muestras:,}, test evaluado={limite_test:,})\n")
    cabecera = f"{'orden':>5} {'alpha':>7} | " + " ".join(f"<10^{int(round(math.log10(u))):>2}" for u in UMBRALES)
    print(cabecera)
    print("-" * len(cabecera))
    for orden in (2, 3, 4):
        for alpha in (0.001, 0.01, 0.1):
            m = ModeloMarkov(orden=orden, alpha=alpha)
            m.entrenar(train)
            m.construir_tabla_montecarlo(muestras, semilla=1234)
            pct = curva_de_adivinacion(m, test, limite_test)
            fila = f"{orden:>5} {alpha:>7} | " + " ".join(f"{p:6.2f}%" for p in pct)
            print(fila)
    print("\nGana la que más porcentaje adivina con menos intentos.")


def main() -> None:
    ap = argparse.ArgumentParser(description="Entrena el modelo de fortaleza de contraseñas.")
    ap.add_argument("--lista", default=RUTA_LISTA_POR_DEFECTO, help="ruta a la lista de contraseñas")
    ap.add_argument("--orden", type=int, default=3)
    ap.add_argument("--alpha", type=float, default=0.01)
    ap.add_argument("--muestras", type=int, default=50000, help="muestras de Monte Carlo")
    ap.add_argument("--limite", type=int, default=None, help="usar solo N contraseñas (más rápido)")
    ap.add_argument("--limite-test", type=int, default=5000, help="cuántas de test evaluar en las comprobaciones")
    ap.add_argument("--comparar", action="store_true", help="ejecuta la comparación de configuraciones (fase 5)")
    args = ap.parse_args()

    contrasenas = cargar_y_limpiar(args.lista, args.limite)
    train, test = dividir(contrasenas)
    comprobacion_fase1(train, test)

    if args.comparar:
        comparar_configuraciones(train, test, args.muestras, args.limite_test)
        return

    modelo = ModeloMarkov(orden=args.orden, alpha=args.alpha)
    modelo.entrenar(train)
    comprobacion_fase2(modelo)

    comprobacion_fase3(modelo)

    comprobacion_fase4(modelo, args.muestras)

    comprobacion_fase5(modelo, test, args.limite_test)

    os.makedirs(CARPETA_MODELO, exist_ok=True)
    ruta = os.path.join(CARPETA_MODELO, "modelo.pkl.gz")
    modelo.guardar(ruta)
    print(f"\n✅ Modelo guardado en {ruta}")
    print("   Ya puedes arrancar la API con:  uvicorn main:app --reload")


if __name__ == "__main__":
    main()
