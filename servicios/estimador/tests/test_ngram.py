import math
import os

import pytest

from ngram import ModeloMarkov


def test_contrasena_tipica_menos_bits_que_aleatoria(modelo):
    assert modelo.bits("password1") < modelo.bits("xK9#qL2!vT")


def test_contrasena_tipica_menos_intentos_que_aleatoria(modelo):
    assert modelo.estimar_intentos("password1") < modelo.estimar_intentos("xK9#qL2!vT")


def test_caracter_nunca_visto_no_da_probabilidad_cero(modelo):
    bits = modelo.bits("~~~~")
    assert math.isfinite(bits) and bits > 0


def test_intentos_al_menos_uno(modelo):
    assert modelo.estimar_intentos("password") >= 1


def test_muestras_reproducibles_con_misma_semilla(modelo):
    a = modelo.construir_tabla_montecarlo(200, semilla=7)
    b = modelo.construir_tabla_montecarlo(200, semilla=7)
    assert a == b
    modelo.construir_tabla_montecarlo(5000, semilla=1234)  # restaura la tabla del fixture


def test_estimar_sin_tabla_falla():
    m = ModeloMarkov()
    m.entrenar(["hola"])
    with pytest.raises(RuntimeError):
        m.estimar_intentos("hola")


def test_guardar_y_cargar_mantiene_resultados(modelo, tmp_path):
    ruta = os.path.join(tmp_path, "modelo.pkl.gz")
    modelo.guardar(ruta)
    cargado = ModeloMarkov.cargar(ruta)
    assert cargado.orden == modelo.orden
    assert cargado.bits("Barcelona2023!") == pytest.approx(modelo.bits("Barcelona2023!"))
    assert cargado.estimar_intentos("maria123") == pytest.approx(modelo.estimar_intentos("maria123"))
