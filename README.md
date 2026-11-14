# DocsisPie (C#)

Simulación del lazo de control DOCSIS-PIE (RFC 8034) para Teoría de Control — UTN FRBA.

## Proyectos

- **DocsisPie.Core**: la lógica del lazo (Sensor, Controlador, Actuador, Proceso, Sim, Parametros, Config).
- **DocsisPie.Consola**: corre la simulación e imprime los resultados en texto.
- **DocsisPie.App**: ventana con los gráficos (retardo, probabilidad de descarte y cola), sliders, Reset, Pausa y la casilla DOCSIS-PIE. Usa ScottPlot.

## Cómo correrlo

Abrir `DocsisPie.sln` en Visual Studio 2022 y ejecutar con F5 (arranca DocsisPie.App), o desde la terminal:

```
dotnet run --project DocsisPie.App
```

Requiere .NET 7 SDK.
