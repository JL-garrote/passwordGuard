import pytest
from fastapi.testclient import TestClient

import main


@pytest.fixture
def cliente(modelo, monkeypatch):
    monkeypatch.setattr(main, "modelo", modelo)
    with TestClient(main.app) as c:
        yield c


def test_health(cliente):
    r = cliente.get("/health")
    assert r.status_code == 200
    assert r.json() == {"ok": True, "orden": 3}


def test_estimar_devuelve_la_forma_esperada(cliente):
    r = cliente.post("/estimar", json={"contrasena": "Barcelona2023!"})
    assert r.status_code == 200
    datos = r.json()
    assert set(datos) == {"longitud", "bits", "intentos_estimados", "segundos", "categoria"}
    assert datos["longitud"] == 14
    assert datos["intentos_estimados"] >= 1


def test_tipica_mas_debil_que_aleatoria(cliente):
    tipica = cliente.post("/estimar", json={"contrasena": "password1"}).json()
    aleatoria = cliente.post("/estimar", json={"contrasena": "xK9#qL2!vT"}).json()
    assert tipica["intentos_estimados"] < aleatoria["intentos_estimados"]


@pytest.mark.parametrize("cuerpo", [{}, {"contrasena": ""}, {"contrasena": "a" * 129}])
def test_peticion_invalida_da_422(cliente, cuerpo):
    assert cliente.post("/estimar", json=cuerpo).status_code == 422


def test_sin_modelo_da_503(monkeypatch):
    monkeypatch.setattr(main, "modelo", None)
    r = TestClient(main.app).post("/estimar", json={"contrasena": "hola"})
    assert r.status_code == 503
