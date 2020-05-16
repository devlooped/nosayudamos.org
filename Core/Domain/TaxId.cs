using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NosAyudamos
{
    partial class TaxId
    {
        static readonly int[] multiplier = new[] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

        /// <summary>
        /// From https://maurobernal.com.ar/cuil/calcular-el-cuil/
        /// </summary>
        public static string FromNationalId(string nationalId, Sex? sex)
        {
            // CUIL/T: Son 11 números en total:
            // XY – 12345678 – Z
            // XY: Indican el tipo
            // 12345678: Número de DNI
            // Z: Código Verificador

            // XY = Masculino:20, Femenino:27, Empresa:30
            var taxId = (sex == Sex.Male ? "20" : "27") + nationalId;

            // Se multiplica XY 12345678 por un número de forma separada:
            var sum = taxId.Select((c, i) => 
                int.Parse(c.ToString(), CultureInfo.InvariantCulture) * multiplier[i]).Sum();

            // Se suman dichos resultados. El resultado obtenido se divide por 11. 
            // De esa división se obtiene un Resto que determina Z
            var mod = sum % 11;

            if (mod == 0)
                return taxId + "0";

            return taxId + (mod == 0 ? "0" : mod == 1 ? "9" : (11 - mod).ToString(CultureInfo.InvariantCulture));
        }
    }
}
