using System;
using System.Globalization;
using System.Linq;

namespace NosAyudamos
{
    class TaxId : IEquatable<TaxId>
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

        public static TaxId Unknown { get; } = new TaxId(nameof(Unknown), TaxCategory.Unknown, TaxIdKind.Unknown);

        /// <summary>
        /// The person does not have a tax identification number.
        /// </summary>
        public static TaxId None { get; } = new TaxId(nameof(None), TaxCategory.NotApplicable, TaxIdKind.None);

        public TaxId(string id, TaxCategory? category = default, TaxIdKind? kind = TaxIdKind.None)
        {
            Id = id;
            Category = category ?? TaxCategory.Unknown;
            Kind = kind ?? TaxIdKind.None;
        }

        /// <summary>
        /// The tax identifier, either a <see cref="TaxIdKind.CUIT"/> or <see cref="TaxIdKind.CUIL"/>.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Category for the simplified tax regime for individuals with low-ish income.
        /// </summary>
        public TaxCategory Category { get; set; }

        public TaxIdKind Kind { get; set; } = TaxIdKind.None;

        /// <summary>
        /// Whether the tax registration includes income taxes.
        /// </summary>
        public bool? HasIncomeTax { get; set; }

        public bool Equals(TaxId other) =>
            Id == other.Id &&
            Category == other.Category &&
            Kind == other.Kind &&
            HasIncomeTax == other.HasIncomeTax;

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is TaxId other))
                return false;

            return Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(Id, Category, Kind, HasIncomeTax);
    }
}
