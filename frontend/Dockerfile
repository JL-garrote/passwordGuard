# Etapa 1: compilar la aplicación de Vue
FROM node:24-alpine AS build
WORKDIR /app

# Primero las dependencias: si solo cambia el código, npm ci sale de caché
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build-only

# Etapa 2: servir los estáticos con nginx, que además hace de proxy de /api
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
