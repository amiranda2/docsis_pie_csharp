namespace DocsisPie.Core;

/// <summary>
/// Constantes de la simulación y de DOCSIS-PIE (equivale a la primera parte de config.py).
/// "const" = no cambian nunca.
/// </summary>
public static class Config
{
    // ------------------------------------------------------------------------
    // Constantes de la simulación (no ajustables desde el panel)
    // ------------------------------------------------------------------------
    public const int MSS = 1500;               // tamaño de paquete [bytes]
    public const double DT = 0.001;            // paso de simulación [s]
    public const double CWND_MAX = 2000;       // ventana máxima de cada emisor [paquetes]
    public const double VENTANA_S = 20;        // segundos visibles en los gráficos
    public const int MUESTREO_PASOS = 10;      // se registra una muestra cada 10 pasos (10 ms)
    public const int PASOS_POR_FRAME = 50;     // pasos simulados por cuadro de animación
    public const int INTERVALO_MS = 30;        // intervalo de la animación [ms]

    // ------------------------------------------------------------------------
    // Constantes de DOCSIS-PIE (RFC 8034, Apéndice A.1)
    // ------------------------------------------------------------------------
    public const double BURST_RESET_TIMEOUT = 1.0;   // [s]
    public const double MEAN_PKTSIZE = 1024;         // [bytes]
    public const double MIN_PKTSIZE = 64;            // [bytes]
    public const double PROB_LOW = 0.85;
    public const double PROB_HIGH = 8.5;
    public const double LATENCY_LOW = 0.005;         // [s]
    public const double LATENCY_HIGH = 0.200;        // [s]
}
