namespace DocsisPie.Core;

/// <summary>Sensor: estima el retardo de cola (RFC 8034, Apéndice A.2). Equivale a sensor.py.</summary>
public class Sensor
{
    /// <summary>Retardo predicho [s] a partir del estado del token bucket.</summary>
    public double EstimateQdelay(Proceso proceso)
    {
        if (proceso.Q <= proceso.Tokens)
            return proceso.Q / proceso.Peak;
        return (proceso.Q - proceso.Tokens) / proceso.Msr + proceso.Tokens / proceso.Peak;
    }
}
