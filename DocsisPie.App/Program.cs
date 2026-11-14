using DocsisPie.Core;

namespace DocsisPie.App;

/// <summary>
/// Punto de partida de la versión gráfica (equivale a simulacion_pie.py).
/// Crea la simulación y la ventana Monitor, y le entrega el control a Windows.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var sim = new Sim();              // el lazo de control
        var monitor = new Monitor(sim);   // la ventana que lo muestra y lo ajusta

        // Application.Run hace lo mismo que plt.show(): abre la ventana y espera a que se cierre.
        // Mientras tanto, el Timer del Monitor llama a sim.Step() una y otra vez.
        Application.Run(monitor);
    }
}
