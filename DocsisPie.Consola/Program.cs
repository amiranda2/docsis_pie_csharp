using DocsisPie.Core;

namespace DocsisPie.Consola;

/// <summary>
/// Punto de partida (equivale a simulacion_pie.py, pero sin ventana).
/// Corre la simulación e imprime una línea por segundo simulado.
/// A los 15 s aplica una perturbación: pasa de 5 a 20 flujos TCP.
/// </summary>
public class Program
{
    const double DURACION_S = 30;          // cuánto tiempo simular [s]
    const double IMPRIMIR_CADA_S = 1.0;    // cada cuánto imprimir una línea [s]
    const double PERTURBACION_S = 15;      // cuándo cambiar la cantidad de flujos [s]
    const int FLUJOS_PERTURBACION = 20;

    public static void Main()
    {
        var sim = new Sim();

        long pasosTotales = (long)Math.Round(DURACION_S / Config.DT);
        long pasosPorLinea = (long)Math.Round(IMPRIMIR_CADA_S / Config.DT);
        long pasoPerturbacion = (long)Math.Round(PERTURBACION_S / Config.DT);

        Console.WriteLine($"DOCSIS-PIE {(Parametros.PieOn ? "activo" : "apagado")} | target = {Parametros.TargetMs} ms | " +
                          $"{Parametros.NFlows} flujos | MSR = {Parametros.MsrMbps} Mbps");
        Console.WriteLine();
        Console.WriteLine("  t [s]  retardo [ms]  drop_prob  cola [paq]  tput [Mbps]  descartes  estado");
        Console.WriteLine("  -----  ------------  ---------  ----------  -----------  ---------  ---------");

        // Este es EL LOOP: acá se ve explícito (en Python lo manejaba matplotlib)
        while (sim.K < pasosTotales)
        {
            if (sim.K == pasoPerturbacion)
            {
                Parametros.NFlows = FLUJOS_PERTURBACION;
                Console.WriteLine($"  >>> Perturbación: ahora hay {FLUJOS_PERTURBACION} flujos TCP");
            }

            sim.Step();

            if (sim.K % pasosPorLinea == 0)
                ImprimirLinea(sim);
        }
    }

    static void ImprimirLinea(Sim sim)
    {
        double retardoMs = sim.Sensor.EstimateQdelay(sim.Proceso) * 1000.0;
        string estado = Parametros.PieOn ? sim.Controlador.State.ToString() : "tail drop";
        Console.WriteLine($"  {sim.T,5:F1}  {retardoMs,12:F2}  {sim.Controlador.DropProb,9:F4}  " +
                          $"{sim.Proceso.Q / Config.MSS,10:F1}  {sim.Throughput1s(),11:F2}  " +
                          $"{sim.Proceso.Drops,9}  {estado}");
    }
}
