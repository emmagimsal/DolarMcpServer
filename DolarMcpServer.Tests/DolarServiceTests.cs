using System.Net;
using System.Text;
using DolarMcpServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DolarMcpServer.Tests;

public class DolarServiceTests
{
    private const string OficialJson =
        """
        {
          "moneda": "USD",
          "casa": "oficial",
          "nombre": "Oficial",
          "compra": 980.00,
          "venta": 1000.00,
          "fechaActualizacion": "2026-07-30T10:00:00.000Z"
        }
        """;

    private const string BlueJson =
        """
        {
          "moneda": "USD",
          "casa": "blue",
          "nombre": "Blue",
          "compra": 1180.00,
          "venta": 1200.00,
          "fechaActualizacion": "2026-07-30T10:00:00.000Z"
        }
        """;

    private const string ListadoJson =
        """
        [
          {
            "moneda": "USD",
            "casa": "oficial",
            "nombre": "Oficial",
            "compra": 980.00,
            "venta": 1000.00,
            "fechaActualizacion": "2026-07-30T10:00:00.000Z"
          },
          {
            "moneda": "USD",
            "casa": "blue",
            "nombre": "Blue",
            "compra": 1180.00,
            "venta": 1200.00,
            "fechaActualizacion": "2026-07-30T10:00:00.000Z"
          }
        ]
        """;

    /// <summary>
    /// Crea un HttpClient cuyo HttpMessageHandler está mockeado con Moq, evitando
    /// llamadas de red reales. El delegado <paramref name="responder"/> decide qué
    /// respuesta devolver en base al HttpRequestMessage recibido.
    /// </summary>
    private static HttpClient CrearHttpClientMockeado(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => responder(request));

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://dolarapi.com/v1/")
        };
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static DolarService CrearServicio(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var httpClient = CrearHttpClientMockeado(responder);
        return new DolarService(httpClient, NullLogger<DolarService>.Instance);
    }

    // ---------------------------------------------------------------
    // Conversión ARS -> USD
    // ---------------------------------------------------------------

    [Fact]
    public async Task ConvertirPesosADolaresAsync_ConMontoValido_DeberiaUsarValorDeVenta()
    {
        // Arrange: dólar oficial -> venta = 1000
        var service = CrearServicio(_ => JsonResponse(HttpStatusCode.OK, OficialJson));

        // Act
        var resultado = await service.ConvertirPesosADolaresAsync(100_000m, "oficial");

        // Assert
        resultado.MontoConvertido.Should().Be(100.00m); // 100.000 / 1000
        resultado.TasaUtilizada.Should().Be(1000.00m);
        resultado.MonedaOrigen.Should().Be("ARS");
        resultado.MonedaDestino.Should().Be("USD");
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("BLUE")]
    public async Task ConvertirPesosADolaresAsync_ConTipoDolarBlue_DeberiaUsarSuPropiaTasaDeVenta(string tipoDolar)
    {
        // Arrange: dólar blue -> venta = 1200
        var service = CrearServicio(_ => JsonResponse(HttpStatusCode.OK, BlueJson));

        // Act
        var resultado = await service.ConvertirPesosADolaresAsync(120_000m, tipoDolar);

        // Assert
        resultado.MontoConvertido.Should().Be(100.00m); // 120.000 / 1200
        resultado.TasaUtilizada.Should().Be(1200.00m);
    }

    [Fact]
    public async Task ConvertirPesosADolaresAsync_ConAliasMep_DeberiaConsultarLaCasaBolsa()
    {
        // Arrange
        HttpRequestMessage? requestCapturado = null;
        var service = CrearServicio(req =>
        {
            requestCapturado = req;
            return JsonResponse(HttpStatusCode.OK, OficialJson.Replace("oficial", "bolsa").Replace("Oficial", "Bolsa (MEP)"));
        });

        // Act
        await service.ConvertirPesosADolaresAsync(50_000m, "mep");

        // Assert: el alias "mep" debe traducirse a la ruta "dolares/bolsa"
        requestCapturado.Should().NotBeNull();
        requestCapturado!.RequestUri!.ToString().Should().EndWith("dolares/bolsa");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task ConvertirPesosADolaresAsync_ConMontoInvalido_DeberiaLanzarArgumentOutOfRangeException(decimal montoInvalido)
    {
        // Arrange: el handler nunca debería llegar a invocarse
        var service = CrearServicio(_ => throw new InvalidOperationException("No debería llamarse a la API con un monto inválido."));

        // Act
        var act = async () => await service.ConvertirPesosADolaresAsync(montoInvalido, "oficial");

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------
    // Conversión USD -> ARS
    // ---------------------------------------------------------------

    [Fact]
    public async Task ConvertirDolaresAPesosAsync_ConMontoValido_DeberiaUsarValorDeCompra()
    {
        // Arrange: dólar oficial -> compra = 980
        var service = CrearServicio(_ => JsonResponse(HttpStatusCode.OK, OficialJson));

        // Act
        var resultado = await service.ConvertirDolaresAPesosAsync(50m, "oficial");

        // Assert
        resultado.MontoConvertido.Should().Be(49_000.00m); // 50 * 980
        resultado.TasaUtilizada.Should().Be(980.00m);
        resultado.MonedaOrigen.Should().Be("USD");
        resultado.MonedaDestino.Should().Be("ARS");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-25)]
    public async Task ConvertirDolaresAPesosAsync_ConMontoInvalido_DeberiaLanzarArgumentOutOfRangeException(decimal montoInvalido)
    {
        var service = CrearServicio(_ => throw new InvalidOperationException("No debería llamarse a la API con un monto inválido."));

        var act = async () => await service.ConvertirDolaresAPesosAsync(montoInvalido, "oficial");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------
    // Tipo de dólar inexistente
    // ---------------------------------------------------------------

    [Fact]
    public async Task ObtenerCotizacionAsync_ConTipoDolarInexistente_DeberiaLanzarArgumentException()
    {
        // Arrange: la API responde 404 cuando la "casa" no existe
        var service = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(string.Empty)
        });

        // Act
        var act = async () => await service.ObtenerCotizacionAsync("moneda-inventada");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*moneda-inventada*");
    }

    [Fact]
    public async Task ConvertirPesosADolaresAsync_ConTipoDolarInexistente_DeberiaPropagarArgumentException()
    {
        // Arrange
        var service = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(string.Empty)
        });

        // Act
        var act = async () => await service.ConvertirPesosADolaresAsync(1000m, "no-existe");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ConvertirDolaresAPesosAsync_ConTipoDolarInexistente_DeberiaPropagarArgumentException()
    {
        // Arrange
        var service = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(string.Empty)
        });

        // Act
        var act = async () => await service.ConvertirDolaresAPesosAsync(10m, "no-existe");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------
    // Errores de servidor / API caída
    // ---------------------------------------------------------------

    [Fact]
    public async Task ObtenerCotizacionAsync_ConErrorDeServidor_DeberiaLanzarHttpRequestException()
    {
        // Arrange: la API responde 500
        var service = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal error")
        });

        // Act
        var act = async () => await service.ObtenerCotizacionAsync("oficial");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ObtenerCotizacionesAsync_ConRespuestaVacia_DeberiaLanzarInvalidOperationException()
    {
        // Arrange: la API responde un array vacío
        var service = CrearServicio(_ => JsonResponse(HttpStatusCode.OK, "[]"));

        // Act
        var act = async () => await service.ObtenerCotizacionesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---------------------------------------------------------------
    // Listado completo de cotizaciones
    // ---------------------------------------------------------------

    [Fact]
    public async Task ObtenerCotizacionesAsync_DeberiaDeserializarTodasLasCotizaciones()
    {
        // Arrange
        var service = CrearServicio(_ => JsonResponse(HttpStatusCode.OK, ListadoJson));

        // Act
        var cotizaciones = await service.ObtenerCotizacionesAsync();

        // Assert
        cotizaciones.Should().HaveCount(2);
        cotizaciones.Should().Contain(c => c.Casa == "oficial" && c.Venta == 1000.00m);
        cotizaciones.Should().Contain(c => c.Casa == "blue" && c.Compra == 1180.00m);
    }
}
