using DocsisPie.Core;
using ScottPlot.WinForms;

namespace DocsisPie.App;

/// <summary>
/// Monitor: muestra la simulación en gráficos y permite ajustarla desde un panel.
/// Equivale a monitor.py. Es una ventana (Form) de Windows Forms.
///
/// Diferencia importante con Python: acá EL LOOP SE VE. Es el Timer "reloj":
/// cada 30 ms llama a Reloj_Tick, que avanza la simulación y redibuja.
/// (En Python ese loop estaba escondido dentro de FuncAnimation.)
/// </summary>
public class Monitor : Form
{
    private readonly Sim sim;   // constante: siempre la misma simulación

    // --- Gráficos (ScottPlot) ---
    private readonly FormsPlot gRetardo = new();   // ax1: retardo de cola
    private readonly FormsPlot gProb = new();      // ax2: probabilidad de descarte
    private readonly FormsPlot gCola = new();      // ax3: cola en paquetes

    // --- Controles ---
    private readonly System.Windows.Forms.Label titulo = new();
    private readonly Button bReset = new() { Text = "Reiniciar" };
    private readonly Button bPausa = new() { Text = "Pausa" };
    private readonly CheckBox chkPie = new() { Text = "DOCSIS-PIE" };

    // --- El loop ---
    private readonly System.Windows.Forms.Timer reloj = new() { Interval = Config.INTERVALO_MS };

    public Monitor(Sim sim)
    {
        this.sim = sim;
        Text = "Simulación DOCSIS-PIE (RFC 8034) - Teoría de Control";
        // Tamaño: 1200 × 950, pero sin pasarse de la pantalla (descontando la barra de tareas)
        var pantalla = Screen.PrimaryScreen!.WorkingArea;
        Width = Math.Min(1200, pantalla.Width);
        Height = Math.Min(950, pantalla.Height);
        StartPosition = FormStartPosition.CenterScreen;

        CrearPantalla();
        CrearBotones();

        reloj.Tick += Reloj_Tick;   // "cuando el reloj suene, llamá a Reloj_Tick"
        reloj.Start();
    }

    // =========================================================================
    // Armado de la pantalla
    // =========================================================================

    /// <summary>
    /// La ventana es una grilla de 5 filas:
    /// título / gráfico retardo / gráfico prob. / gráfico cola / panel de controles.
    /// </summary>
    private void CrearPantalla()
    {
        var grilla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        grilla.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));     // título
        grilla.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));   // gráfico 1
        grilla.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));   // gráfico 2
        grilla.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));   // gráfico 3
        grilla.RowStyles.Add(new RowStyle(SizeType.Absolute, 6 * 32 + 12));   // controles (6 sliders)

        titulo.Dock = DockStyle.Fill;
        titulo.TextAlign = ContentAlignment.MiddleCenter;
        titulo.Font = new Font("Segoe UI", 10);

        foreach (var g in new[] { gRetardo, gProb, gCola })
            g.Dock = DockStyle.Fill;

        grilla.Controls.Add(titulo, 0, 0);
        grilla.Controls.Add(gRetardo, 0, 1);
        grilla.Controls.Add(gProb, 0, 2);
        grilla.Controls.Add(gCola, 0, 3);
        grilla.Controls.Add(CrearPanelControles(), 0, 4);
        Controls.Add(grilla);
    }

    /// <summary>Panel de abajo: dos columnas de sliders y una de botones (como en Python).</summary>
    private Control CrearPanelControles()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

        // Cada slider recibe: texto, mínimo, máximo, paso, valor inicial y
        // qué hacer cuando cambia (una "lambda": v => Parametros.X = v).
        var izquierda = Columna(
            Slider("Target [ms]", 1, 50, 0.5, Parametros.TargetMs, v => Parametros.TargetMs = v),
            Slider("INTERVAL [ms]", 5, 50, 1, Parametros.TUpdateMs, v => Parametros.TUpdateMs = v),
            Slider("A (alpha)", 0, 1.5, 0.01, Parametros.Alpha, v => Parametros.Alpha = v),
            Slider("B (beta)", 0, 6, 0.05, Parametros.Beta, v => Parametros.Beta = v),
            Slider("MAX_BURST [ms]", 0, 300, 2, Parametros.MaxBurstMs, v => Parametros.MaxBurstMs = v),
            Slider("Buffer [ms @MSR]", 20, 1000, 10, Parametros.BufferMs, v => Parametros.BufferMs = v));

        var derecha = Columna(
            Slider("MSR [Mbps]", 1, 50, 1, Parametros.MsrMbps, v => Parametros.MsrMbps = v),
            Slider("Peak Rate [Mbps]", 1, 100, 1, Parametros.PeakMbps, v => Parametros.PeakMbps = v),
            Slider("Max Burst [KB]", 10, 5000, 10, Parametros.BurstKb, v => Parametros.BurstKb = v),
            Slider("Flujos TCP", 1, 50, 1, Parametros.NFlows, v => Parametros.NFlows = (int)v),
            Slider("RTT base [ms]", 10, 200, 5, Parametros.RttMs, v => Parametros.RttMs = v));

        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        bReset.Width = bPausa.Width = 110;
        bReset.Height = bPausa.Height = 32;
        chkPie.Checked = Parametros.PieOn;
        chkPie.AutoSize = true;
        botones.Controls.Add(bReset);
        botones.Controls.Add(bPausa);
        botones.Controls.Add(chkPie);

        panel.Controls.Add(izquierda, 0, 0);
        panel.Controls.Add(derecha, 1, 0);
        panel.Controls.Add(botones, 2, 0);
        return panel;
    }

    /// <summary>Apila varios sliders uno debajo del otro.</summary>
    private static Control Columna(params Control[] sliders)
    {
        var col = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var s in sliders)
            col.Controls.Add(s);
        return col;
    }

    /// <summary>
    /// Arma un slider con su texto a la izquierda y su valor a la derecha (equivale a mk_slider).
    /// Detalle: el TrackBar de Windows solo maneja enteros, así que cuenta "pasos":
    /// valor = mínimo + pasos × paso.
    /// </summary>
    private static Control Slider(string texto, double min, double max, double paso,
                                  double inicial, Action<double> alCambiar)
    {
        var fila = new TableLayoutPanel { Width = 500, Height = 32, ColumnCount = 3, Margin = new Padding(0) };
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));

        var etiqueta = new System.Windows.Forms.Label { Text = texto, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        var valor = new System.Windows.Forms.Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var barra = new TrackBar
        {
            Minimum = 0,
            Maximum = (int)Math.Round((max - min) / paso),
            Value = (int)Math.Round((inicial - min) / paso),
            TickStyle = TickStyle.None,
            AutoSize = false,
            Height = 26,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
        };

        double Leer() => min + barra.Value * paso;   // función local: pasos → valor real
        valor.Text = Leer().ToString("0.##");

        barra.ValueChanged += (_, _) =>
        {
            double v = Leer();
            valor.Text = v.ToString("0.##");
            alCambiar(v);              // escribe en Parametros (como P.__setitem__ en Python)
        };

        fila.Controls.Add(etiqueta, 0, 0);
        fila.Controls.Add(barra, 1, 0);
        fila.Controls.Add(valor, 2, 0);
        return fila;
    }

    // =========================================================================
    // Botones (equivalen a on_reset, on_pausa y on_chk)
    // =========================================================================
    private void CrearBotones()
    {
        bReset.Click += (_, _) => sim.Reset();

        bPausa.Click += (_, _) =>
        {
            Parametros.Pausa = !Parametros.Pausa;
            bPausa.Text = Parametros.Pausa ? "Reanudar" : "Pausa";
        };

        chkPie.CheckedChanged += (_, _) => Parametros.PieOn = chkPie.Checked;
    }

    // =========================================================================
    // EL LOOP: se ejecuta cada INTERVALO_MS (equivale a update() de monitor.py)
    // =========================================================================
    private void Reloj_Tick(object? sender, EventArgs e)
    {
        // 1) Avanzar la simulación: 50 pasos de 1 ms por cada cuadro
        if (!Parametros.Pausa)
        {
            for (int i = 0; i < Config.PASOS_POR_FRAME; i++)
                sim.Step();
        }

        if (sim.HT.Count == 0)
            return;

        // 2) Copiar el historial a arreglos (ScottPlot dibuja arreglos)
        double[] t = sim.HT.ToArray();
        double[] qd = sim.HQd.ToArray();
        double[] dp = sim.HDp.ToArray();
        double[] q = sim.HQ.ToArray();
        double bufPkts = Parametros.BufferMs / 1000.0 * Parametros.MsrMbps * 1e6 / 8.0 / Config.MSS;

        // Ventana de tiempo visible: los últimos 20 s
        double x1 = Math.Max(sim.T, Config.VENTANA_S);
        double x0 = x1 - Config.VENTANA_S;

        // 3) Redibujar los tres gráficos
        Dibujar(gRetardo, t, qd, ScottPlot.Colors.Blue, "Retardo [ms]", x0, x1,
                ymax: Math.Max(Math.Max(2 * Parametros.TargetMs, qd.Max() * 1.1), 1),
                lineaH: Parametros.TargetMs, colorH: ScottPlot.Colors.Red, leyenda: true);

        Dibujar(gProb, t, dp, ScottPlot.Colors.Orange, "Prob. de descarte", x0, x1,
                ymax: Math.Max(0.05, dp.Max() * 1.2));

        Dibujar(gCola, t, q, ScottPlot.Colors.Green, "Cola [paquetes]", x0, x1,
                ymax: Math.Max(Math.Max(10, bufPkts * 1.05), q.Max() * 1.1),
                lineaH: bufPkts, colorH: ScottPlot.Colors.Gray, xLabel: "Tiempo [s]");

        // 4) Actualizar el título
        string estado = Parametros.PieOn ? $"Estado: {sim.Controlador.State}" : "Solo tail drop";
        titulo.Text = $"t = {sim.T,5:F1} s   |   Throughput (último s): {sim.Throughput1s(),5:F1} Mbps   |   " +
                      $"Descartes totales: {sim.Proceso.Drops}   |   {estado}";
        titulo.Refresh();   // pintarlo ya (si no, Windows lo posterga mientras los gráficos se redibujan)
    }

    /// <summary>Dibuja una curva en un gráfico, con una línea horizontal opcional (referencia o buffer).</summary>
    private static void Dibujar(FormsPlot g, double[] x, double[] y, ScottPlot.Color color, string yLabel,
                                double x0, double x1, double ymax,
                                double? lineaH = null, ScottPlot.Color? colorH = null,
                                bool leyenda = false, string? xLabel = null)
    {
        var plot = g.Plot;
        plot.Clear();   // se borra y se vuelve a dibujar en cada cuadro

        var curva = plot.Add.Scatter(x, y);
        curva.Color = color;
        curva.LineWidth = 1.5f;
        curva.MarkerSize = 0;

        if (lineaH.HasValue)
        {
            var h = plot.Add.HorizontalLine(lineaH.Value);
            h.Color = colorH ?? ScottPlot.Colors.Gray;
            h.LineWidth = 1.5f;
            h.LinePattern = leyenda ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Dotted;
            if (leyenda)
            {
                curva.LegendText = "Retardo de cola (estimado)";
                h.LegendText = "Referencia (target)";
            }
        }

        if (leyenda)
            plot.ShowLegend(ScottPlot.Alignment.UpperRight);

        plot.YLabel(yLabel);
        if (xLabel != null)
            plot.XLabel(xLabel);

        plot.Axes.SetLimits(x0, x1, 0, ymax);
        g.Refresh();
    }
}
