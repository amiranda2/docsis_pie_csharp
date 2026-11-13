namespace DocsisPie.Core;

/// <summary>
/// Controlador: PI discreto y máquina de estados de DOCSIS-PIE (RFC 8034, Apéndice A.2).
/// Equivale a controlador.py.
/// </summary>
public class Controlador
{
    public double DropProb;          // probabilidad de descarte (la señal de control)
    public double QdelayOld;         // retardo de la actualización anterior [s]
    public double BurstAllowance;    // tiempo restante de protección de ráfaga [s]
    public double BurstReset;        // tiempo en calma dentro de QUIESCENT [s]
    public EstadoAqm State;          // INACTIVE, QUIESCENT o ACTIVE

    public Controlador()
    {
        Reset();
    }

    /// <summary>control_path_init()</summary>
    public void Reset()
    {
        DropProb = 0.0;
        QdelayOld = 0.0;
        BurstAllowance = 0.0;
        BurstReset = 0.0;
        State = EstadoAqm.INACTIVE;
    }

    public void PasarAQuiescent()
    {
        State = EstadoAqm.QUIESCENT;
    }

    public void PasarAActive()
    {
        State = EstadoAqm.ACTIVE;
        BurstAllowance = Parametros.MaxBurstMs / 1000.0;
    }

    /// <summary>Se llama cada INTERVAL. qdelay y refe en segundos.</summary>
    public void CalculateDropProb(double qdelay, double refe)
    {
        double interval = Parametros.TUpdateMs / 1000.0;

        if (BurstAllowance > 0)
        {
            DropProb = 0.0;
            BurstAllowance = Math.Max(0.0, BurstAllowance - interval);
        }
        else
        {
            // PI: término proporcional (error) + término sobre la variación del retardo
            double p = Parametros.Alpha * (qdelay - refe) + Parametros.Beta * (qdelay - QdelayOld);
            double dp = DropProb;

            // Auto-tuning extendido: la ganancia se escala según el nivel de congestión
            if (dp < 0.000001) p /= 2048;
            else if (dp < 0.00001) p /= 512;
            else if (dp < 0.0001) p /= 128;
            else if (dp < 0.001) p /= 32;
            else if (dp < 0.01) p /= 8;
            else if (dp < 0.1) p /= 2;
            else if (dp < 1) p /= 0.5;
            else if (dp < 10) p /= 0.125;
            else p /= 0.03125;

            if (dp >= 0.1 && p > 0.02)
                p = 0.02;
            dp += p;

            if (qdelay < Config.LATENCY_LOW && QdelayOld < Config.LATENCY_LOW)
                dp *= 0.98;                 // decaimiento exponencial
            else if (qdelay > Config.LATENCY_HIGH)
                dp += 0.02;                 // subida rápida

            // Saturación (puede superar 1: se escala luego por tamaño de paquete)
            DropProb = Math.Min(Math.Max(dp, 0.0), Config.PROB_LOW * Config.MEAN_PKTSIZE / Config.MIN_PKTSIZE);
        }

        bool quiet = qdelay < 0.5 * refe && QdelayOld < 0.5 * refe
                     && DropProb == 0 && BurstAllowance == 0;

        if (State == EstadoAqm.ACTIVE && quiet)
        {
            State = EstadoAqm.QUIESCENT;
            BurstReset = 0.0;
        }
        else if (State == EstadoAqm.QUIESCENT)
        {
            if (quiet)
            {
                BurstReset += interval;
                if (BurstReset > Config.BURST_RESET_TIMEOUT)
                {
                    BurstReset = 0.0;
                    State = EstadoAqm.INACTIVE;
                }
            }
            else
            {
                BurstReset = 0.0;
            }
        }

        QdelayOld = qdelay;
    }
}
