using System.Net;
using System.Text.Json;
using DolarMcpServer.Models;
using Microsoft.Extensions.Logging;

namespace DolarMcpServer.Services;

/// <summary>
/// Servicio que consume la API pública https://dolarapi.com para obtener cotizaciones
/// del dólar en Argentina y realizar conversiones de moneda.
/// </summary>
public class DolarService : IDolarService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DolarService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Alias comunes que un usuario o un LLM podría usar, mapeados a la "casa" real de dolarapi.com.
    private static readonly Dictionary<string, string> AliasCasa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["oficial"] = "oficial",
        ["blue"] = "blue",
        ["informal"] = "blue",
        ["bolsa"] = "bolsa",
        ["mep"] = "bolsa",
        ["cripto"] = "cripto",
        ["crypto"] = "cripto",
        ["tarjeta"] = "tarjeta",
        ["turista"] = "tarjeta",
        ["mayorista"] = "mayorista",
        ["contadoconliqui"] = "contadoconliqui",
        ["ccl"] = "contadoconliqui",
        ["contado con liqui"] = "contadoconliqui"
    };

    public DolarService(HttpClient httpClient, ILogger<DolarService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://dolarapi.com/v1/");
        }
    }

    public async Task<List<DolarCotizacion>> ObtenerCotizacionesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consultando todas las cotizaciones en dolarapi.com");

        using var response = await _httpClient.GetAsync("dolares", cancellationToken);
        await EnsureSuccessAsync(response, "No se pudo obtener el listado de cotizaciones", cancellationToken);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var cotizaciones = await JsonSerializer.DeserializeAsync<List<DolarCotizacion>>(stream, JsonOptions, cancellationToken);

        if (cotizaciones is null || cotizaciones.Count == 0)
        {
            throw new InvalidOperationException("La API de dolarapi.com devolvió una respuesta vacía o inválida.");
        }

        return cotizaciones;
    }

    public async Task<DolarCotizacion> ObtenerCotizacionAsync(string tipoDolar, CancellationToken cancellationToken = default)
    {
        var casa = NormalizarTipoDolar(tipoDolar);

        _logger.LogInformation("Consultando cotización para la casa '{Casa}'", casa);

        using var response = await _httpClient.GetAsync($"dolares/{casa}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ArgumentException(
                $"El tipo de dólar '{tipoDolar}' no es válido. Valores aceptados: " +
                "oficial, blue, bolsa (mep), cripto, tarjeta, mayorista, contadoconliqui (ccl).");
        }

        await EnsureSuccessAsync(response, $"No se pudo obtener la cotización para '{casa}'", cancellationToken);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var cotizacion = await JsonSerializer.DeserializeAsync<DolarCotizacion>(stream, JsonOptions, cancellationToken);

        if (cotizacion is null)
        {
            throw new InvalidOperationException($"No se pudo interpretar la cotización devuelta para '{casa}'.");
        }

        return cotizacion;
    }

    public async Task<ResultadoConversion> ConvertirPesosADolaresAsync(decimal montoArs, string tipoDolar, CancellationToken cancellationToken = default)
    {
        if (montoArs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(montoArs), "El monto en pesos debe ser mayor a cero.");
        }

        var cotizacion = await ObtenerCotizacionAsync(tipoDolar, cancellationToken);

        if (cotizacion.Venta <= 0)
        {
            throw new InvalidOperationException($"La cotización de venta para '{cotizacion.Casa}' no es válida.");
        }

        // Para comprar dólares con pesos se usa el valor de VENTA (lo que el mercado te cobra).
        var montoUsd = Math.Round(montoArs / cotizacion.Venta, 2, MidpointRounding.AwayFromZero);

        return new ResultadoConversion
        {
            MontoOriginal = montoArs,
            MonedaOrigen = "ARS",
            MontoConvertido = montoUsd,
            MonedaDestino = "USD",
            TipoDolarUtilizado = cotizacion.Nombre,
            TasaUtilizada = cotizacion.Venta,
            DetalleTasa = $"Se usó el valor de venta (1 USD = {cotizacion.Venta:F2} ARS) de '{cotizacion.Nombre}'.",
            FechaCotizacion = cotizacion.FechaActualizacion
        };
    }

    public async Task<ResultadoConversion> ConvertirDolaresAPesosAsync(decimal montoUsd, string tipoDolar, CancellationToken cancellationToken = default)
    {
        if (montoUsd <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(montoUsd), "El monto en dólares debe ser mayor a cero.");
        }

        var cotizacion = await ObtenerCotizacionAsync(tipoDolar, cancellationToken);

        if (cotizacion.Compra <= 0)
        {
            throw new InvalidOperationException($"La cotización de compra para '{cotizacion.Casa}' no es válida.");
        }

        // Para vender dólares y recibir pesos se usa el valor de COMPRA (lo que el mercado te paga).
        var montoArs = Math.Round(montoUsd * cotizacion.Compra, 2, MidpointRounding.AwayFromZero);

        return new ResultadoConversion
        {
            MontoOriginal = montoUsd,
            MonedaOrigen = "USD",
            MontoConvertido = montoArs,
            MonedaDestino = "ARS",
            TipoDolarUtilizado = cotizacion.Nombre,
            TasaUtilizada = cotizacion.Compra,
            DetalleTasa = $"Se usó el valor de compra (1 USD = {cotizacion.Compra:F2} ARS) de '{cotizacion.Nombre}'.",
            FechaCotizacion = cotizacion.FechaActualizacion
        };
    }

    private static string NormalizarTipoDolar(string? tipoDolar)
    {
        var valor = string.IsNullOrWhiteSpace(tipoDolar) ? "oficial" : tipoDolar.Trim();

        return AliasCasa.TryGetValue(valor, out var casa)
            ? casa
            : valor.ToLowerInvariant();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string mensajeError, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var cuerpo = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"{mensajeError}. Código HTTP: {(int)response.StatusCode} ({response.StatusCode}). Detalle: {cuerpo}");
    }
}
