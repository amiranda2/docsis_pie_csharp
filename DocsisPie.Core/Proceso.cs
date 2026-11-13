namespace DocsisPie.Core;

/// <summary>
/// Proceso (planta): cola, shaper DOCSIS y emisores TCP, sin sistema de control.
/// Equivale a proceso.py.
/// </summary>
public class Proceso
{
    public double Q = 0.0;                                  // bytes en cola
    public double Tokens = Parametros.BurstKb * 1000.0;     // crédito del token bucket
    public List<Flow> Flows = new List<Flow>();             // los emisores TCP
    public double Served = 0.0;                             // bytes despachados desde la última muestra
    public int Drops = 0;                                   // total de descartes

    // Propiedades de solo lectura (en Python: @property). Se recalculan cada vez que se leen.
    public double Msr => Parametros.MsrMbps * 1e6 / 8.0;                                // [bytes/s]
    public double Peak => Math.Max(Parametros.PeakMbps, Parametros.MsrMbps) * 1e6 / 8.0; // [bytes/s]
    public double BufferSize => Parametros.BufferMs / 1000.0 * Msr;                     // [bytes]

    /// <summary>Tiempo que tarda en vaciarse la cola: es la salida del proceso.</summary>
    public double RetardoCola()
    {
        if (Q <= Tokens)
            return Q / Peak;
        return (Q - Tokens) / Msr + Tokens / Peak;
    }

    // --- Un paso de simulación (1 ms) ---------------------------------------
    /// <summary>Sin actuador (null), el proceso funciona en lazo abierto: solo hay tail drop.</summary>
    public void Step(double t, Actuador? actuador = null)
    {
        double msr = Msr;
        double peak = Peak;
        double bucket = Parametros.BurstKb * 1000.0;
        double bufferSize = BufferSize;
        double rtt = Parametros.RttMs / 1000.0 + RetardoCola();

        // Ajusta la cantidad de flujos al valor del panel
        int n = Parametros.NFlows;
        while (Flows.Count < n)
            Flows.Add(new Flow());
        if (Flows.Count > n)
            Flows.RemoveRange(n, Flows.Count - n);

        foreach (Flow f in Flows)
        {
            // Reacción (retrasada un RTT) ante una pérdida
            if (f.CutAt != null && t >= f.CutAt.Value)
            {
                f.Ssthresh = Math.Max(f.Cwnd / 2.0, 2.0);
                f.Cwnd = f.Ssthresh;
                f.CutAt = null;
                f.RecoveryUntil = t + rtt;
            }

            // Paquetes que el emisor envía en este paso
            f.Credit += f.Cwnd / rtt * Config.DT;
            int nPkts = (int)f.Credit;
            f.Credit -= nPkts;
            for (int i = 0; i < nPkts; i++)
            {
                bool drop;
                if (Q + Config.MSS > bufferSize)
                {
                    drop = true;                       // buffer lleno (tail drop)
                    if (actuador != null)
                        actuador.OnTailDrop();
                }
                else if (actuador != null && actuador.DropEarly(Config.MSS, Q, bufferSize))
                {
                    drop = true;                       // descarte de DOCSIS-PIE
                }
                else
                {
                    drop = false;
                }

                if (drop)
                {
                    Drops++;
                    if (f.CutAt == null && t >= f.RecoveryUntil)
                        f.CutAt = t + rtt;
                }
                else
                {
                    Q += Config.MSS;
                }
            }

            // Crecimiento de la ventana
            if (f.Cwnd < f.Ssthresh)
                f.Cwnd += f.Cwnd * Config.DT / rtt;    // slow start
            else
                f.Cwnd += Config.DT / rtt;             // +1 paquete por RTT
            f.Cwnd = Math.Min(f.Cwnd, Config.CWND_MAX);
        }

        // Salida de la cola: shaper con token bucket (MSR) limitado por Peak Rate
        Tokens = Math.Min(Tokens + msr * Config.DT, bucket);
        double sv = Math.Min(Q, Math.Min(peak * Config.DT, Tokens));
        Q -= sv;
        Tokens -= sv;
        Served += sv;
    }
}
