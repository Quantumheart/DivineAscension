using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Services;

namespace DivineAscension.Systems.Altar.Pipeline;

/// <summary>
/// Executes prayer steps in sequence, with support for short-circuiting.
/// </summary>
public class PrayerPipeline(IEnumerable<IPrayerStep> steps, ILoggerWrapper logger) : IPrayerPipeline
{
    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IReadOnlyList<IPrayerStep> _steps =
        steps?.ToList() ?? throw new ArgumentNullException(nameof(steps));

    public PrayerContext Execute(PrayerContext context)
    {
        foreach (var step in _steps)
        {
            _logger.Debug($"[PrayerPipeline] Executing {step.Name}");

            try
            {
                step.Execute(context);
            }
            catch (Exception ex)
            {
                _logger.Error($"[PrayerPipeline] Error in {step.Name}: {ex.Message}");
                context.Success = false;
                context.Message = "An error occurred while processing your prayer.";
                context.IsComplete = true;
                break;
            }

            if (context.IsComplete)
            {
                _logger.Debug($"[PrayerPipeline] Pipeline completed at {step.Name} (Success: {context.Success})");
                break;
            }
        }

        return context;
    }
}