import random

import pytest

from ngram import ModeloMarkov

BASES = ["password", "123456", "qwerty", "maria", "barcelona", "monkey", "dragon", "love"]
SUFIJOS = ["", "1", "12", "123", "2023", "2024", "!", "1!"]


@pytest.fixture(scope="session")
def modelo() -> ModeloMarkov:
    lista = [b + s for b in BASES for s in SUFIJOS]
    lista += [b.capitalize() + s for b in BASES for s in SUFIJOS]
    random.Random(0).shuffle(lista)
    m = ModeloMarkov(orden=3, alpha=0.01)
    m.entrenar(lista)
    m.construir_tabla_montecarlo(5000, semilla=1234)
    return m
