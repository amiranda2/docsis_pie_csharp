namespace DocsisPie.Core;

/// <summary>Emisor TCP simplificado (slow start + AIMD). Equivale a flow.py.</summary>
public class Flow
{
    public double Cwnd = 10.0;           // ventana de congestión [paquetes]
    public double Ssthresh = 1e9;        // umbral entre slow start y crecimiento lineal
    public double Credit = 0.0;          // fracción de paquete acumulada (la "alcancía")
    public double? CutAt = null;         // instante en que el emisor "se entera" de la pérdida
                                         // (double? = puede valer null, como None en Python)
    public double RecoveryUntil = 0.0;   // una sola reducción por RTT
}
