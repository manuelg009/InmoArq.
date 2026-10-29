using System.Globalization;

namespace InmoArq.Web;

public static class Formato
{
    private static readonly CultureInfo Numeros = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Convierte una fecha UTC a la hora de República Dominicana (UTC-4, sin horario de verano).</summary>
    public static string FechaRd(DateTime? utc) =>
        utc is null ? "—" : utc.Value.AddHours(-4).ToString("dd/MM/yyyy hh:mm tt", Numeros);

    public static string Pesos(decimal valor) => "RD$ " + valor.ToString("N0", Numeros);

    public static bool IntentarLeerMonto(string? texto, out decimal monto) =>
        decimal.TryParse((texto ?? "").Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out monto);
}
