namespace DocsisPie.Core;

/// <summary>
/// Actuador: convierte DropProb en descartes de paquetes (RFC 8034, Apéndice A.3).
/// Equivale a actuador.py.
/// </summary>
public class Actuador
{
    public readonly Controlador Controlador;   // readonly = constante: siempre el mismo objeto
    public double AccuProb;                    // probabilidad acumulada (desaleatorización)

    public Actuador(Controlador controlador)
    {
        Controlador = controlador;
        Reset();
    }

    public void Reset()
    {
        AccuProb = 0.0;
    }

    public void OnTailDrop()
    {
        AccuProb = 0.0;
    }

    /// <summary>Devuelve true si el paquete que llega debe descartarse.</summary>
    public bool DropEarly(double size, double q, double bufferSize)
    {
        Controlador c = Controlador;
        double refe = Parametros.TargetMs / 1000.0;

        // Protección de ráfagas
        if (c.BurstAllowance > 0)
            return false;

        if (c.DropProb == 0)
            AccuProb = 0.0;

        if (c.State == EstadoAqm.INACTIVE)
        {
            if (q < bufferSize / 3)
                return false;
            c.PasarAQuiescent();
        }

        double p1 = Math.Min(c.DropProb * size / Config.MEAN_PKTSIZE, Config.PROB_LOW);
        AccuProb += p1;

        // Salvaguarda para que no descarte con colas casi vacías
        if ((c.QdelayOld < 0.5 * refe && c.DropProb < 0.2) || q <= 2 * Config.MEAN_PKTSIZE)
            return false;

        // Desaleatorización
        if (AccuProb < Config.PROB_LOW)
            return false;
        if (AccuProb < Config.PROB_HIGH && Random.Shared.NextDouble() > p1)
            return false;

        AccuProb = 0.0;
        if (c.State == EstadoAqm.QUIESCENT)
            c.PasarAActive();
        return true;
    }
}
