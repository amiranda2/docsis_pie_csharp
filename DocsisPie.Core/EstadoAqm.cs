namespace DocsisPie.Core;

/// <summary>
/// Estados de la máquina de estados de DOCSIS-PIE (RFC 8034, sección 4.3).
/// En Python son tres textos en config.py; en C# lo natural es un enum.
/// </summary>
public enum EstadoAqm
{
    INACTIVE,   // la cola está por debajo de 1/3: no se descarta
    QUIESCENT,  // transición: el primer descarte pasa a ACTIVE
    ACTIVE,     // control normal (después de la protección de ráfagas)
}
