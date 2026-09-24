var longitud = document.getElementById("longitud");

var mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
var minusculas = "abcdefghijklmnopqrstuvwxyz";
var numeros = "0123456789";
var simbolos = "!@#$%^&*()-+";

var contrasenaGenerada;

function generarContrasena() {
    contrasenaGenerada = "";
    for (var i = 0; i < longitud.value; i++) {
        var tipoCaracter = Math.floor(Math.random() * 4);
        switch (tipoCaracter) {
            case 0:
                contrasenaGenerada += mayusculas[aleatorio(0,mayusculas.length)];
                break;
            case 1:
                contrasenaGenerada += minusculas[aleatorio(0,minusculas.length)];
                break;
            case 2:
                contrasenaGenerada += numeros[aleatorio(0,numeros.length)];
                break;
            case 3:
                contrasenaGenerada += simbolos[aleatorio(0,simbolos.length)];
                break;
        }
    }

    console.log("Contraseña generada: " + contrasenaGenerada);
    return contrasenaGenerada;
}