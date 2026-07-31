using System.ComponentModel;
using System.Text;
using DolarMcpServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DolarMcpServer.Tools;

/// <summary>
/// Herramientas MCP para consultar cotizaciones del dólar en Argentina y convertir
/// entre pesos argentinos (ARS) y dólares estadounidenses (USD).
/// Fuente de datos: https://dolarapi.com
/// </summary>
[McpServerToolType]
public class DolarTools
{
    private readonly IDolarService _dolarService;
    private readonly ILogger<DolarTools> _logger;

    public DolarTools(IDolarService dolarService, ILogger<DolarTools> logger)
    {
        _dolarService = dolarService;
        _logger = logger;
    }

    [McpServerTool(Name = "ObtenerCotizacionesDolar")]
    [Description("Obtiene el listado completo de cotizaciones del dólar en Argentina: oficial, blue, bolsa (MEP), " +
                 "cripto, tarjeta, mayorista y contado con liqui (CCL), con sus valores de compra y venta.")]
    public async Task<string> ObtenerCotizacionesDolar(CancellationToken cancellationToken)
    {
        try
        {
            var cotizaciones = await _dolarService.ObtenerCotizacionesAsync(cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("Cotizaciones del dólar en Argentina:");
            sb.AppendLine();

            foreach (var c in cotizaciones.OrderBy(c => c.Nombre))
            {
                sb.AppendLine($"- {c.Nombre} ({c.Casa}): Compra ${c.Compra:F2} / Venta ${c.Venta:F2} ARS " +
                              $"(actualizado: {c.FechaActualizacion:yyyy-MM-dd HH:mm})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las cotizaciones del dólar");
            return $"Error: no se pudieron obtener las cotizaciones del dólar. Detalle: {ex.Message}";
        }
    }

    [McpServerTool(Name = "ConvertirPesosADolares")]
    [Description("Convierte un monto en pesos argentinos (ARS) a dólares estadounidenses (USD), " +
                 "usando el valor de venta del tipo de dólar indicado.")]
    public async Task<string> ConvertirPesosADolares(
        [Description("Monto en pesos argentinos (ARS) a convertir. Debe ser un número positivo.")]
        decimal montoARS,
        [Description("Tipo de dólar a utilizar: oficial, blue, bolsa (o mep), cripto, tarjeta, mayorista, " +
                      "contadoconliqui (o ccl). Si se omite, se usa 'oficial'.")]
        string? tipoDolar,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _dolarService.ConvertirPesosADolaresAsync(montoARS, tipoDolar ?? "oficial", cancellationToken);

            return $"{resultado.MontoOriginal:F2} ARS equivalen a {resultado.MontoConvertido:F2} USD " +
                   $"usando el dólar {resultado.TipoDolarUtilizado}. {resultado.DetalleTasa}";
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al convertir pesos a dólares");
            return $"Error: no se pudo realizar la conversión. Detalle: {ex.Message}";
        }
    }

    [McpServerTool(Name = "ConvertirDolaresAPesos")]
    [Description("Convierte un monto en dólares estadounidenses (USD) a pesos argentinos (ARS), " +
                 "usando el valor de compra del tipo de dólar indicado.")]
    public async Task<string> ConvertirDolaresAPesos(
        [Description("Monto en dólares (USD) a convertir. Debe ser un número positivo.")]
        decimal montoUSD,
        [Description("Tipo de dólar a utilizar: oficial, blue, bolsa (o mep), cripto, tarjeta, mayorista, " +
                      "contadoconliqui (o ccl). Si se omite, se usa 'oficial'.")]
        string? tipoDolar,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _dolarService.ConvertirDolaresAPesosAsync(montoUSD, tipoDolar ?? "oficial", cancellationToken);

            return $"{resultado.MontoOriginal:F2} USD equivalen a {resultado.MontoConvertido:F2} ARS " +
                   $"usando el dólar {resultado.TipoDolarUtilizado}. {resultado.DetalleTasa}";
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al convertir dólares a pesos");
            return $"Error: no se pudo realizar la conversión. Detalle: {ex.Message}";
        }
    }

    [McpServerTool(Name = "ObtenerTasaConversion")]
    [Description("Devuelve la tasa exacta de cambio (compra y venta) entre el peso argentino y el dólar " +
                 "para una variante específica (oficial, blue, bolsa/mep, cripto, tarjeta, mayorista, contadoconliqui/ccl).")]
    public async Task<string> ObtenerTasaConversion(
        [Description("Tipo de dólar del cual se desea conocer la tasa: oficial, blue, bolsa (o mep), cripto, " +
                      "tarjeta, mayorista, contadoconliqui (o ccl). Si se omite, se usa 'oficial'.")]
        string? tipoDolar,
        CancellationToken cancellationToken)
    {
        try
        {
            var cotizacion = await _dolarService.ObtenerCotizacionAsync(tipoDolar ?? "oficial", cancellationToken);

            return $"Dólar {cotizacion.Nombre} ({cotizacion.Casa}): " +
                   $"Compra = ${cotizacion.Compra:F2} ARS | Venta = ${cotizacion.Venta:F2} ARS | " +
                   $"Última actualización: {cotizacion.FechaActualizacion:yyyy-MM-dd HH:mm}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la tasa de conversión");
            return $"Error: no se pudo obtener la tasa de conversión. Detalle: {ex.Message}";
        }
    }
}
