using System.Text;
using System.Text.RegularExpressions;

namespace passwordGuard.Api.models
{
    public class Contrasena
    {
        string[] tipo = { "palabra", "numero", "año", "repeticion", "simbolo" };


        // Devuelve solo la estructura anónima (tipo y longitud de cada trozo), nunca el texto de la contraseña.
        // Ejemplo: "Barcelona2023!" -> "palabra(9) numero(4) simbolo(1) longitud: 14"
        public string partirContrasena(string contrasena)
        {
            StringBuilder sb = new StringBuilder();
            MatchCollection trozos = Regex.Matches(contrasena, @"\p{L}+|\p{Nd}+|[^\p{L}\p{Nd}]+");
            foreach (Match trozo in trozos)
            {
                if (Regex.IsMatch(trozo.Value, @"\p{L}+"))
                {
                    sb.Append($"{tipo[0]}({trozo.Length}) ");
                }
                else if (Regex.IsMatch(trozo.Value, @"\p{Nd}+"))
                {
                    sb.Append($"{tipo[1]}({trozo.Length}) ");
                }
                else
                {
                    sb.Append($"{tipo[4]}({trozo.Length}) ");
                }
            }
            sb.Append("longitud: " + contrasena.Length);
            return sb.ToString();
        }
    }

}