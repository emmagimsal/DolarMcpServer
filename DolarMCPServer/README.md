# DolarMcpServer

Servidor MCP (Model Context Protocol) en .NET 10 que expone herramientas para
consultar cotizaciones del dólar en Argentina y convertir entre ARS y USD,
usando la API pública [dolarapi.com](https://dolarapi.com).

## Estructura

```
DolarMcpServer/
├── DolarMcpServer.csproj
├── Program.cs
├── Models/
│   └── DolarResponse.cs
├── Services/
│   ├── IDolarService.cs
│   └── DolarService.cs
└── Tools/
    └── DolarTools.cs
```

## Herramientas expuestas

| Herramienta                | Descripción                                                             |
|-----------------------------|--------------------------------------------------------------------------|
| `ObtenerCotizacionesDolar`  | Lista todas las cotizaciones (oficial, blue, bolsa/MEP, tarjeta, etc.)   |
| `ConvertirPesosADolares`    | Convierte `montoARS` a USD usando el valor de venta del `tipoDolar`      |
| `ConvertirDolaresAPesos`    | Convierte `montoUSD` a ARS usando el valor de compra del `tipoDolar`     |
| `ObtenerTasaConversion`     | Devuelve compra/venta exactos para un `tipoDolar`                       |

Valores válidos de `tipoDolar` (no distingue mayúsculas): `oficial`, `blue`,
`bolsa` (o `mep`), `cripto`, `tarjeta`, `mayorista`, `contadoconliqui` (o `ccl`).

## Compilar y ejecutar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd DolarMcpServer
dotnet restore
dotnet build -c Release
```

Prueba rápida por consola (el servidor espera mensajes JSON-RPC por stdin):

```bash
dotnet run --project DolarMcpServer.csproj
```

Para generar un ejecutable standalone:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
# o linux-x64 / osx-x64 según tu plataforma
```

## Configuración en Claude Desktop

Editá el archivo `claude_desktop_config.json` de Claude Desktop
(`%APPDATA%\Claude\claude_desktop_config.json` en Windows,
`~/Library/Application Support/Claude/claude_desktop_config.json` en macOS) y
agregá una entrada como la del archivo `claude_desktop_config.json` incluido
en este proyecto. Dos alternativas:

**Opción A — usando `dotnet run` (útil en desarrollo):**

```json
{
  "mcpServers": {
    "dolar-argentina": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\ruta\\a\\tu\\proyecto\\DolarMcpServer\\DolarMcpServer.csproj"]
    }
  }
}
```

**Opción B — usando el binario ya publicado (recomendado para uso normal):**

```json
{
  "mcpServers": {
    "dolar-argentina": {
      "command": "C:\\ruta\\a\\tu\\proyecto\\publish\\DolarMcpServer.exe"
    }
  }
}
```

En macOS/Linux, reemplazá la ruta por la del binario publicado sin extensión
(por ejemplo `/home/usuario/dolar-mcp/publish/DolarMcpServer`).

Reiniciá Claude Desktop después de guardar el archivo; la herramienta
"dolar-argentina" debería aparecer en el ícono de herramientas (🔨) del chat.

## Notas de diseño

- **Stdout limpio**: todo el logging se envía a stderr (`Program.cs`), ya que
  el transporte stdio reserva stdout exclusivamente para los mensajes JSON-RPC
  del protocolo MCP.
- **Tasas de conversión**: para pasar de ARS a USD se usa el valor de *venta*
  (lo que el mercado cobra por vender dólares); para pasar de USD a ARS se usa
  el valor de *compra* (lo que el mercado paga al recibir dólares).
- **Manejo de errores**: cada herramienta captura excepciones de red, tipos de
  dólar inválidos (HTTP 404 de la API) y montos negativos, devolviendo un
  mensaje de error legible en lugar de propagar una excepción no controlada.
