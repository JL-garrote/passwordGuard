namespace passwordGuard.Api.Service
{
    public class comprobarContrasenaService
    {
        bool longitudValida = false;


        public bool comprobarLongitud(string contrasena)
        {
            if (contrasena.Length < 8)
            {
                Console.WriteLine("La contraseña es demasiado corta.");
                longitudValida = false;
            }
            else
            {
                Console.WriteLine("La contraseña tiene una longitud adecuada.");
                longitudValida = true;
            }

            return longitudValida;
        }

        public bool comprobarMayusculas(string contrasena)
        {
            bool mayusculasValidas = false;
            foreach (char c in contrasena)
            {
                if (char.IsUpper(c))
                {
                    Console.WriteLine("La contraseña contiene al menos una letra mayúscula.");
                    mayusculasValidas = true;
                    break;
                }
                else
                {
                    mayusculasValidas = false;
                }
            }

            return mayusculasValidas;
        }

        public bool comprobarMinusculas(string contrasena)
        {
            bool minusculasValidas = false;

            foreach (char c in contrasena)
            {
                if (char.IsLower(c))
                {
                    Console.WriteLine("La contraseña contiene al menos una letra minúscula.");
                    minusculasValidas = true;
                    break;
                }
                else
                {
                    minusculasValidas = false;
                }
            }

            return minusculasValidas;
        }

        public bool comprobarNumeros(string contrasena)
        {
            bool numerosValidos = false;

            foreach (char c in contrasena)
            {
                if (char.IsDigit(c))
                {
                    Console.WriteLine("La contraseña contiene al menos un número.");
                    numerosValidos = true;
                    break;
                }
                else
                {
                    numerosValidos = false;
                }
            }

            return numerosValidos;
        }

        public bool comprobarSimbolos(string contrasena)
        {
            bool simbolosValidos = false;

            foreach (char c in contrasena)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    Console.WriteLine("La contraseña contiene al menos un símbolo.");
                    simbolosValidos = true;
                    break;
                }
                else
                {
                    simbolosValidos = false;
                }
            }

            return simbolosValidos;
        }

        public bool comprobarContrasena(string contrasena, bool usarMayusculas, bool usarMinusculas, bool usarNumeros, bool usarSimbolos)
        {
            bool valida = comprobarLongitud(contrasena);

            if (usarMayusculas && !comprobarMayusculas(contrasena))
            {
                valida = false;
            }

            if (usarMinusculas && !comprobarMinusculas(contrasena))
            {
                valida = false;
            }

            if (usarNumeros && !comprobarNumeros(contrasena))
            {
                valida = false;
            }

            if (usarSimbolos && !comprobarSimbolos(contrasena))
            {
                valida = false;
            }

            if (valida)
            {
                Console.WriteLine("La contraseña es válida.");
            }
            else
            {
                Console.WriteLine("La contraseña no cumple con los requisitos.");
            }

            return valida;
        }

        public bool comprobarContrasenaSinSimbolos(string contrasena)
        {
            return comprobarContrasena(contrasena, true, true, true, false);
        }

        public bool comprobarContrasenaSinNumeros(string contrasena)
        {
            return comprobarContrasena(contrasena, true, true, false, true);
        }

        public bool comprobarContrasena(string contrasena)
        {
            return comprobarContrasena(contrasena, true, true, true, true);
        }

       
        public int calcularPuntuacion(string contrasena)
        {
            int puntuacion = 0;

            if (contrasena.Length >= 8)
            {
                puntuacion++;
            }

            if (contrasena.Length >= 12)
            {
                puntuacion++;
            }

            if (contrasena.Length >= 16)
            {
                puntuacion++;
            }

            if (comprobarMayusculas(contrasena))
            {
                puntuacion++;
            }

            if (comprobarMinusculas(contrasena))
            {
                puntuacion++;
            }

            if (comprobarNumeros(contrasena))
            {
                puntuacion++;
            }

            if (comprobarSimbolos(contrasena))
            {
                puntuacion++;
            }

            return puntuacion;
        }

        public string obtenerNivel(int puntuacion)
        {
            if (puntuacion <= 2)
            {
                return "Débil";
            }
            else if (puntuacion <= 4)
            {
                return "Media";
            }
            else if (puntuacion <= 6)
            {
                return "Fuerte";
            }
            else
            {
                return "Muy fuerte";
            }
        }

        public string evaluarContrasena(string contrasena)
        {
            int puntuacion = calcularPuntuacion(contrasena);
            string nivel = obtenerNivel(puntuacion);
            return nivel;
        }
    }
}
