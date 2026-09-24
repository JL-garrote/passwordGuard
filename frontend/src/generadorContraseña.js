var longitud = document.getElementById("longitud");

var mayusculas[] = {"A","B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
var minusculas[] = {"a","b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"};
var numeros[] = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
var simbolos[] = {"!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "=", "+", "[", "]", "{", "}", ";", ":", "'", '"', ",", ".", "<", ">", "/", "?", "|", "\\"};

var contrasenaGenerada = "";

for (var i = 0; i < longitud.value; i++) {
    math.random() < 0.5 ? contrasenaGenerada += mayusculas[Math.floor(Math.random() * mayusculas.length)] : contrasenaGenerada += minusculas[Math.floor(Math.random() * minusculas.length)];
}

console.log("Contraseña generada: " + contrasenaGenerada);