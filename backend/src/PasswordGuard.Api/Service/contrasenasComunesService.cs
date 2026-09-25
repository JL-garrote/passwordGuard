namespace passwordGuard.Api.Service
{
    public class contrasenasComunesService
    {
        
        private readonly HashSet<string> contrasenasComunes;

        public contrasenasComunesService()
        {
            string rutaArchivo = Path.Combine(AppContext.BaseDirectory, "data", "10k_most_common.txt");

            contrasenasComunes = new HashSet<string>(File.ReadLines(rutaArchivo), StringComparer.OrdinalIgnoreCase);
        }

        public bool esContrasenaComun(string contrasena)
        {
            if (contrasenasComunes.Contains(contrasena))
            {
                Console.WriteLine("La contraseña es demasiado común.");
                return true;
            }

            Console.WriteLine("La contraseña no es común.");
            return false;
        }
    }
}
