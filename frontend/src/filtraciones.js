// Comprueba si una contraseña aparece en filtraciones públicas con la API Pwned Passwords de Have I Been Pwned.
// Usa k-anonymity: solo se envían los 5 primeros caracteres del hash SHA-1, nunca la contraseña.

const URL_API = 'https://api.pwnedpasswords.com/range/';

// Calcula el SHA-1 de un texto en hexadecimal y mayúsculas (el formato que usa la API)
async function calcularSha1(texto) {
  const bytes = new TextEncoder().encode(texto);
  const hash = await crypto.subtle.digest('SHA-1', bytes);
  return [...new Uint8Array(hash)]
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('')
    .toUpperCase();
}

/**
 * Devuelve cuántas veces aparece la contraseña en filtraciones conocidas (0 si no aparece).
 * Lanza un error si la API no responde.
 * @param {string} contrasena
 * @returns {Promise<number>}
 */
export async function comprobarFiltraciones(contrasena) {
  const hash = await calcularSha1(contrasena);
  const prefijo = hash.slice(0, 5);
  const sufijo = hash.slice(5);

  // Add-Padding: la API añade entradas de relleno para que el tamaño de la respuesta no revele nada
  const respuesta = await fetch(URL_API + prefijo, { headers: { 'Add-Padding': 'true' } });
  if (!respuesta.ok) throw new Error(`Error ${respuesta.status} al consultar Have I Been Pwned`);

  // Cada línea tiene el formato "SUFIJO:VECES"; la coincidencia se busca aquí, en local
  const lineas = (await respuesta.text()).split('\n');
  for (const linea of lineas) {
    const [sufijoLinea, veces] = linea.trim().split(':');
    if (sufijoLinea === sufijo) {
      return parseInt(veces, 10);
    }
  }

  return 0;
}
