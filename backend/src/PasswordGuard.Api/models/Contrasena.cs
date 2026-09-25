using System.Text;
using System.Text.RegularExpressions;

namespace passwordGuard.Api.models
{
    public class Contrasena
    {
        string[] tipo = { "palabra", "numero", "año", "repeticion", "simbolo" };


        public string partirContrasena(string contrasena)
        {
            int longitud;
            StringBuilder sb = new StringBuilder();
            MatchCollection trozos = Regex.Matches(contrasena, @"\p{L}+|\p{Nd}+|[^\p{L}\p{Nd}]+");
            foreach (Match trozo in trozos)
            {
                if (Regex.IsMatch(trozo.Value, @"\p{L}+"))
                {
                    sb.Append(tipo[0] + " ");
                }
                else if (Regex.IsMatch(trozo.Value, @"\p{Nd}+"))
                {
                    sb.Append(tipo[1] + " " );
                }
                else
                {
                    sb.Append(tipo[4] + " ");
                }
            }
            longitud = sb.ToString().Length;
            sb.Append("longitud: " + longitud);
            return sb.ToString();
        }
    }

}