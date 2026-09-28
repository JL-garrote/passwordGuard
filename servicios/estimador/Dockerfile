FROM python:3.14-slim

WORKDIR /app

# Primero las dependencias: si solo cambia el código, esta capa sale de caché
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY ngram.py main.py ./

# El modelo NO va en la imagen: se monta como volumen en /app/modelo (ver docker-compose.yml)

# 0.0.0.0 para que el backend lo alcance por la red interna de Compose.
# La privacidad la da no publicar el puerto, no la IP de escucha.
EXPOSE 8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
