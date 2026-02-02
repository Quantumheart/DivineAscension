namespace DivineAscension.Systems.Altar.Pipeline;

/// <summary>
/// A single step in the prayer pipeline.
/// Each step performs a focused task: validation, calculation, or side effect.
/// </summary>
public interface IPrayerStep
{
    /// <summary>
    /// Gets the name of this step for logging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes this step of the prayer pipeline.
    /// Set context.IsComplete = true to short-circuit the pipeline.
    /// </summary>
    /// <param name="context">The prayer context containing input and accumulated state.</param>
    void Execute(PrayerContext context);
}