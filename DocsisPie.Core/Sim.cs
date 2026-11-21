namespace DocsisPie.Core;

/// <summary>
/// Lazo de control cerrado: conecta el proceso, el sensor, el controlador y el actuador.
/// Equivale a sim.py.
/// </summary>
public class Sim
{
    public double T;                 // tiempo simulado [s]
    public long K;                   // cantidad de pasos simulados
    public Proceso Proceso = null!;  // (null! = "se asigna en Reset()")
    public Sensor Sensor = null!;
    public Controlador Controlador = null!;
    public Actuador Actuador = null!;
    public double NextUpdate;        // instante de la próxima ejecución del controlador [s]

    // Historial para los gráficos: una muestra cada 10 ms (en Python, los deque h_t, h_qd, ...)
    public Queue<double> HT = new(), HQd = new(), HDp = new(), HQ = new(), HTput = new();
    public Queue<double> HErr = new();   // señal de error e = retardo − target [ms]
    private int maxMuestras;

    public Sim()
    {
        Reset();
    }

    public void Reset()
    {
        T = 0.0;
        K = 0;
        Proceso = new Proceso();
        Sensor = new Sensor();
        Controlador = new Controlador();
        Actuador = new Actuador(Controlador);
        NextUpdate = Parametros.TUpdateMs / 1000.0;
        maxMuestras = (int)(Config.VENTANA_S / (Config.DT * Config.MUESTREO_PASOS)) + 50;
        foreach (var h in new[] { HT, HQd, HDp, HQ, HTput, HErr })
            h.Clear();
    }

    // --- Un paso de simulación (1 ms) ---------------------------------------
    public void Step()
    {
        bool pie = Parametros.PieOn;

        Proceso.Step(T, pie ? Actuador : null);
        T += Config.DT;
        K++;

        // Controlador: se ejecuta cada INTERVAL
        if (pie)
        {
            if (T >= NextUpdate)
            {
                double qdelay = Sensor.EstimateQdelay(Proceso);
                Controlador.CalculateDropProb(qdelay, Parametros.TargetMs / 1000.0);
                NextUpdate += Parametros.TUpdateMs / 1000.0;
            }
        }
        else
        {
            Controlador.Reset();
            Actuador.Reset();
            NextUpdate = T + Parametros.TUpdateMs / 1000.0;
        }

        if (K % Config.MUESTREO_PASOS == 0)
        {
            double tput = Proceso.Served * 8 / (Config.DT * Config.MUESTREO_PASOS) / 1e6;
            Proceso.Served = 0.0;
            double qdMs = Sensor.EstimateQdelay(Proceso) * 1000.0;
            Agregar(HT, T);
            Agregar(HQd, qdMs);
            // Señal de error: lo que se mide menos lo que se quiere (igual que en el PI del Controlador)
            Agregar(HErr, qdMs - Parametros.TargetMs);
            Agregar(HDp, Controlador.DropProb);
            Agregar(HQ, Proceso.Q / Config.MSS);
            Agregar(HTput, tput);
        }
    }

    /// <summary>Throughput promedio del último segundo [Mbps].</summary>
    public double Throughput1s()
    {
        var datos = HTput.Skip(Math.Max(0, HTput.Count - 100)).ToList();
        return datos.Count > 0 ? datos.Average() : 0.0;
    }

    /// <summary>Agrega una muestra y descarta la más vieja si se pasa del máximo (como deque(maxlen=...)).</summary>
    private void Agregar(Queue<double> h, double valor)
    {
        h.Enqueue(valor);
        while (h.Count > maxMuestras)
            h.Dequeue();
    }
}
