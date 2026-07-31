using System.Text.Json.Serialization;

namespace DolarMcpServer.Models;

/// <summary>
/// Representa una cotización individual devuelta por dolarapi.com
/// (endpoint https://dolarapi.com/v1/dolares y https://dolarapi.com/v1/dolares/{casa}).
/// </summary>
public class DolarCotizacion
{
    /// <summary>Código de la moneda, por ejemplo "USD".</summary>
    [JsonPropertyName("moneda")]
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Identificador interno del tipo de cotización: oficial, blue, bolsa, cripto, tarjeta, mayorista, contadoconliqui.</summary>
    [JsonPropertyName("casa")]
    public string Casa { get; set; } = string.Empty;

    /// <summary>Nombre legible del tipo de cotización, por ejemplo "Dólar Blue".</summary>
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Precio de compra (lo que te pagan por vender dólares).</summary>
    [JsonPropertyName("compra")]
    public decimal Compra { get; set; }

    /// <summary>Precio de venta (lo que pagás para comprar dólares).</summary>
    [JsonPropertyName("venta")]
    public decimal Venta { get; set; }

    /// <summary>Fecha y hora (ISO 8601) de la última actualización de la cotización.</summary>
    [JsonPropertyName("fechaActualizacion")]
    public DateTimeOffset FechaActualizacion { get; set; }
}

/// <summary>
/// Resultado de una conversión de moneda (ARS -> USD o USD -> ARS),
/// incluyendo la tasa utilizada para que el LLM pueda explicar el cálculo.
/// </summary>
public class ResultadoConversion
{
    public decimal MontoOriginal { get; set; }
    public string MonedaOrigen { get; set; } = string.Empty;
    public decimal MontoConvertido { get; set; }
    public string MonedaDestino { get; set; } = string.Empty;
    public string TipoDolarUtilizado { get; set; } = string.Empty;
    public decimal TasaUtilizada { get; set; }
    public string DetalleTasa { get; set; } = string.Empty;
    public DateTimeOffset FechaCotizacion { get; set; }
}
