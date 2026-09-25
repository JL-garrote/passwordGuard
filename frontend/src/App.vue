<script setup lang="ts">
import { ref } from 'vue'
import AppHeader, { type Seccion } from './components/header.vue'
import GeneradorContrasena from './components/generador.vue'
import AnalizadorContrasena from './components/analizador.vue'

const seccion = ref<Seccion>('generar')
</script>

<template>
  <AppHeader :seccion="seccion" @cambiar="seccion = $event" />
  <main>
    <GeneradorContrasena v-if="seccion === 'generar'" />
    <AnalizadorContrasena v-else @generar="seccion = 'generar'" />
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
