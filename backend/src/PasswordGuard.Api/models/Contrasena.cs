using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace passwordGuard.Api.models
{
    public class Contrasena
    {
        string[] tipo = { "palabra", "numero", "año", "repeticion", "simbolo", "fecha" };

        public string partirContrasena(string contrasena)
        {
            StringBuilder sb = new StringBuilder();
            MatchCollection trozos = Regex.Matches(contrasena, @"\p{L}+|\p{Nd}+|[^\p{L}\p{Nd}]+");
            foreach (Match trozo in trozos)
            {
                if(trozo.Length >= 3 && trozo.Value.All(c => char.ToLower(c) == char.ToLower(trozo.Value[0])))
                {
                    sb.Append($"{tipo[3]}({trozo.Length}) ");
                }
                else if (Regex.IsMatch(trozo.Value, @"\p{L}+"))
                {
                    sb.Append($"{tipo[0]}({trozo.Length}) ");
                }
                else if (trozo.Length == 8 && EsFecha(trozo.Value))
                {
                    sb.Append($"{tipo[5]}({trozo.Length}) ");
                }
                else if (trozo.Length == 4 && int.TryParse(trozo.Value, out int valor) && valor >= 1900 && valor <= DateTime.Now.Year + 1)
                {
                    sb.Append($"{tipo[2]}({trozo.Length}) ");
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

        // Formatos de fecha más habituales en contraseñas: 15031995 (día-mes-año) y 19950315 (año-mes-día)
        static readonly string[] FormatosFecha = { "ddMMyyyy", "yyyyMMdd" };

        // TryParseExact descarta fechas imposibles (31 de febrero, mes 13...), así que basta con comprobar el año
        static bool EsFecha(string digitos)
        {
            return DateTime.TryParseExact(digitos, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha)
                && fecha.Year >= 1900
                && fecha.Year <= DateTime.Now.Year + 1;
        }
    }
}