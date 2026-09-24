<script setup lang="ts">
import GeneradorContrasena from './components/generador.vue'
</script>

<template>
  <main>
    <GeneradorContrasena />
  </main>
</template>

<style scoped>
main {
  grid-column: 1 / -1; /* ocupa todo el ancho de la rejilla de #app */
  width: 100%;
}
</style>
