"""
main.py — microservicio FastAPI que sirve el modelo ya entrenado.

La API NO entrena nada: carga el modelo guardado por entrenar.py y responde.
Regla de oro: NUNCA se registran (log/print) las contraseñas que llegan.

Solo lo llama el backend .NET, por eso escucha únicamente en 127.0.0.1 y no
tiene CORS (el navegador nunca habla con este servicio).

Arrancar en local:
    uvicorn main:app --host 127.0.0.1 --port 8000
"""

from __future__ import annotations

import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from ngram import ModeloMarkov

RUTA_MODELO = os.path.join(os.path.dirname(__file__), "modelo", "modelo.pkl.gz")

# Misma velocidad de ataque que usa el analizador del frontend.
INTENTOS_POR_SEGUNDO = 1e10

modelo: ModeloMarkov | None = None


@asynccontextmanager
async def lifespan(_: FastAPI):
    global modelo
    if modelo is None:
        if not os.path.exists(RUTA_MODELO):
            raise RuntimeError(
                f"No existe el modelo en {RUTA_MODELO}. Ejecuta antes: python entrenar.py"
            )
        modelo = ModeloMarkov.cargar(RUTA_MODELO)
    yield


app = FastAPI(
    title="Estimador de fortaleza de contraseñas",
    description="Modelo de Markov de caracteres + Monte Carlo (Dell'Amico & Filippone).",
    version="1.0.0",
    lifespan=lifespan,
)


class Peticion(BaseModel):
    contrasena: str = Field(..., min_length=1, max_length=128)


class Respuesta(BaseModel):
    longitud: int
    bits: float                 
    intentos_estimados: float   
    segundos: float             
    categoria: str


def _categoria(intentos: float) -> str:
    """Etiqueta legible a partir del número de intentos estimado."""
    if intentos < 1e6:
        return "muy debil"
    if intentos < 1e9:
        return "debil"
    if intentos < 1e12:
        return "aceptable"
    if intentos < 1e15:
        return "fuerte"
    return "muy fuerte"


@app.get("/health")
def health() -> dict:
    return {"ok": modelo is not None, "orden": modelo.orden if modelo else None}


@app.post("/estimar", response_model=Respuesta)
def estimar(pet: Peticion) -> Respuesta:
    if modelo is None:
        raise HTTPException(status_code=503, detail="Modelo no cargado.")
    pw = pet.contrasena
    intentos = modelo.estimar_intentos(pw)
    return Respuesta(
        longitud=len(pw),
        bits=round(modelo.bits(pw), 2),
        intentos_estimados=intentos,
        segundos=intentos / INTENTOS_POR_SEGUNDO,
        categoria=_categoria(intentos),
    )
