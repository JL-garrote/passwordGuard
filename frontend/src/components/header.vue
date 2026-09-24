<script setup lang="ts"></script>

<template>
  <header class="header">
    <div class="contenido">
      <!-- Marca -->
      <a href="/" class="marca" aria-label="Password Guard, inicio">
        <svg class="logo" viewBox="0 0 24 24" width="32" height="32" aria-hidden="true">
          <path
            fill="currentColor"
            d="M12 1 3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4Zm0 6a3 3 0 0 1 3 3v1h.5c.28 0 .5.22.5.5v4c0 .28-.22.5-.5.5h-7a.5.5 0 0 1-.5-.5v-4c0-.28.22-.5.5-.5H9v-1a3 3 0 0 1 3-3Zm0 1.5A1.5 1.5 0 0 0 10.5 10v1h3v-1A1.5 1.5 0 0 0 12 8.5Z"
          />
        </svg>
        <span class="textos">
          <span class="nombre">Password Guard</span>
          <span class="lema">Seguridad sin complicaciones</span>
        </span>
      </a>

      <!-- Navegación -->
      <nav class="nav" aria-label="Secciones">
        <a href="/" class="nav-item activo" aria-current="page">Generar</a>
        <span class="nav-item deshabilitado" aria-disabled="true" title="Próximamente">Analizar</span>
      </nav>

      <!-- Distintivo de privacidad -->
      <div class="distintivo">
        <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
          <path
            fill="currentColor"
            d="M18 8h-1V6A5 5 0 0 0 7 6v2H6a2 2 0 0 0-2 2v10c0 1.1.9 2 2 2h12a2 2 0 0 0 2-2V10a2 2 0 0 0-2-2Zm-6 9a2 2 0 1 1 0-4 2 2 0 0 1 0 4Zm3.1-9H8.9V6a3.1 3.1 0 0 1 6.2 0v2Z"
          />
        </svg>
        <span>100% local</span>
      </div>
    </div>
  </header>
</template>

<style scoped>
.header {
  position: fixed;
  top: 0;
  left: 0;
  z-index: 40;
  width: 100%;
  background: var(--card);
  border-bottom: 1px solid var(--muted);
  font-family: 'IBM Plex Sans', system-ui, sans-serif;
}

.contenido {
  max-width: 1200px;
  height: 80px;
  margin: 0 auto;
  padding: 0 2rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.25rem;
}

/* Marca */
.marca {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0;
  color: inherit;
  text-decoration: none;
}
.marca:hover {
  background: none; /* anula el hover verde de main.css */
}
.logo {
  color: var(--primary);
  flex-shrink: 0;
}
.textos {
  display: flex;
  flex-direction: column;
}
.nombre {
  font-size: 16px;
  line-height: 1.25;
  font-weight: 600;
  color: var(--ink);
}
.lema {
  font-size: 11px;
  line-height: 1;
  font-weight: 500;
  color: var(--ink-muted);
}

/* Navegación */
.nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem;
  border-radius: 0.75rem;
  border: 1px solid var(--muted);
  background: var(--subtle);
}
.nav-item {
  padding: 0.375rem 1rem;
  border-radius: 0.5rem;
  font-size: 14px;
  line-height: 20px;
  font-weight: 600;
  text-decoration: none;
  color: var(--ink-muted);
  transition: background 0.2s, color 0.2s;
}
.nav-item.activo {
  background: var(--primary);
  color: var(--on-primary);
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
}
.nav-item.deshabilitado {
  opacity: 0.55;
  cursor: not-allowed;
}
.nav-item:focus-visible,
.marca:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

/* Distintivo */
.distintivo {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  border-radius: 999px;
  border: 1px solid color-mix(in srgb, var(--secondary) 20%, transparent);
  background: color-mix(in srgb, var(--secondary-container) 40%, transparent);
  color: var(--secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

@media (max-width: 768px) {
  .contenido {
    padding: 0 1rem;
    height: 64px;
  }
  .lema,
  .distintivo {
    display: none;
  }
}
@media (max-width: 420px) {
  .nombre {
    display: none;
  }
}
</style>
