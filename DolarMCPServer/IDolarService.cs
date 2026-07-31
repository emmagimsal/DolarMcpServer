using DolarMcpServer.Models;

namespace DolarMcpServer.Services;

public interface IDolarService
{
    /// <summary>Obtiene todas las cotizaciones disponibles (oficial, blue, bolsa/MEP, tarjeta, etc.).</summary>
    Task<List<DolarCotizacion>> ObtenerCotizacionesAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene la cotización de un tipo de dólar específico.</summary>
    /// <param name="tipoDolar">oficial, blue, bolsa (o mep), cripto, tarjeta, mayorista, contadoconliqui (o ccl).</param>
    Task<DolarCotizacion> ObtenerCotizacionAsync(string tipoDolar, CancellationToken cancellationToken = default);

    /// <summary>Convierte un monto en pesos argentinos a dólares usando el valor de venta del tipo indicado.</summary>
    Task<ResultadoConversion> ConvertirPesosADolaresAsync(decimal montoArs, string tipoDolar, CancellationToken cancellationToken = default);

    /// <summary>Convierte un monto en dólares a pesos argentinos usando el valor de compra del tipo indicado.</summary>
    Task<ResultadoConversion> ConvertirDolaresAPesosAsync(decimal montoUsd, string tipoDolar, CancellationToken cancellationToken = default);
}
