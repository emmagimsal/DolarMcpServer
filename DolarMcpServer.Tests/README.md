## Pruebas unitarias

El proyecto `DolarMcpServer.Tests` (xUnit + Moq + FluentAssertions) prueba
`DolarService` mockeando el `HttpMessageHandler` interno del `HttpClient`, sin
hacer llamadas de red reales. Cubre:

- Conversión ARS → USD usando el valor de **venta** (incluye distintos tipos
  de dólar y el alias `mep` → `bolsa`).
- Conversión USD → ARS usando el valor de **compra**.
- Montos inválidos (negativos y cero) → `ArgumentOutOfRangeException`.
- Tipo de dólar inexistente (HTTP 404 de la API) → `ArgumentException`,
  tanto en la consulta directa como al propagarse desde ambas conversiones.
- Errores de servidor (HTTP 500) → `HttpRequestException`.
- Respuesta vacía del listado de cotizaciones → `InvalidOperationException`.
- Deserialización correcta del listado completo de cotizaciones.

Ejecutar las pruebas:

```bash
cd DolarMcpServer.Tests
dotnet test
```

## Notas de diseño

- **Stdout limpio**: todo el logging se envía a stderr (`Program.cs`), ya que
  el transporte stdio reserva stdout exclusivamente para los mensajes JSON-RPC
  del protocolo MCP.
- **Tasas de conversión**: para pasar de ARS a USD se usa el valor de _venta_
  (lo que el mercado cobra por vender dólares); para pasar de USD a ARS se usa
  el valor de _compra_ (lo que el mercado paga al recibir dólares).
- **Manejo de errores**: cada herramienta captura excepciones de red, tipos de
  dólar inválidos (HTTP 404 de la API) y montos negativos, devolviendo un
  mensaje de error legible en lugar de propagar una excepción no controlada.
