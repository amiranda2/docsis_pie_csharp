namespace DocsisPie.Core;

/// <summary>
/// Parámetros ajustables desde el panel. Equivale al diccionario P de config.py:
/// donde Python escribe P["target_ms"], acá se escribe Parametros.TargetMs.
/// Son "static": hay una sola copia, compartida por todas las clases.
/// </summary>
public static class Parametros
{
    public static double TargetMs = 10.0;     // LATENCY_TARGET (10 ms por defecto)
    public static double TUpdateMs = 16.0;    // INTERVAL (16 ms)
    public static double Alpha = 0.25;        // A: ganancia sobre el error
    public static double Beta = 2.5;          // B: ganancia sobre la variación del retardo
    public static double MaxBurstMs = 142.0;  // MAX_BURST (150 ms - 8 ms)
    public static double MsrMbps = 10.0;      // Maximum Sustained Traffic Rate
    public static double PeakMbps = 20.0;     // Peak Traffic Rate
    public static double BurstKb = 500.0;     // Max Traffic Burst (profundidad del token bucket)
    public static double BufferMs = 250.0;    // BUFFER_SIZE expresado en ms a la tasa MSR
    public static int NFlows = 5;             // cantidad de flujos TCP
    public static double RttMs = 50.0;        // RTT base (sin encolamiento)
    public static bool PieOn = true;          // DOCSIS-PIE activo / solo tail drop
    public static bool Pausa = false;
}
