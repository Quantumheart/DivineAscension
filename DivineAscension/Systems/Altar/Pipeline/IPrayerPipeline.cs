namespace DivineAscension.Systems.Altar.Pipeline;

/// <summary>
/// Orchestrates prayer processing by executing steps in sequence.
/// Steps can short-circuit the pipeline by setting IsComplete on the context.
/// </summary>
public interface IPrayerPipeline
{
    /// <summary>
    /// Executes all pipeline steps in order until completion or short-circuit.
    /// </summary>
    /// <param name="context">The prayer context to process.</param>
    /// <returns>The same context with accumulated results.</returns>
    PrayerContext Execute(PrayerContext context);
}