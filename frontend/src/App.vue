<script setup lang="ts">
import AppHeader from './components/header.vue'
import GeneradorContrasena from './components/generador.vue'
</script>

<template>
  <AppHeader />
  <main>
    <GeneradorContrasena />
  </main>
</template>

<style scoped>
main {
  grid-column: 1 / -1; /* ocupa todo el ancho de la rejilla de #app */
  width: 100%;
  padding-top: 80px; /* hueco para el header fijo */
}

@media (max-width: 768px) {
  main {
    padding-top: 64px;
  }
}
</style>
