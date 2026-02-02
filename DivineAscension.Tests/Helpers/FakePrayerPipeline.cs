using DivineAscension.Systems.Altar.Pipeline;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IPrayerPipeline for testing.
/// Allows configuring the result returned for any prayer context.
/// </summary>
public class FakePrayerPipeline : IPrayerPipeline
{
    /// <summary>
    /// When set, Execute will apply this action to modify the context.
    /// </summary>
    public Action<PrayerContext>? OnExecute { get; set; }

    /// <summary>
    /// When true, sets Success=true on context. When false, sets Success=false.
    /// Default: true
    /// </summary>
    public bool DefaultSuccess { get; set; } = true;

    /// <summary>
    /// Default message to set on context. Default: "Prayer successful"
    /// </summary>
    public string DefaultMessage { get; set; } = "Prayer successful";

    /// <summary>
    /// Records the contexts that were passed to Execute for verification.
    /// </summary>
    public List<PrayerContext> ExecutedContexts { get; } = new();

    public PrayerContext Execute(PrayerContext context)
    {
        ExecutedContexts.Add(context);

        if (OnExecute != null)
        {
            OnExecute(context);
        }
        else
        {
            context.Success = DefaultSuccess;
            context.Message = DefaultMessage;
        }

        return context;
    }

    /// <summary>
    /// Configures the pipeline to simulate a successful prayer.
    /// </summary>
    public void SetupSuccess(int favor = 10, int prestige = 10, string message = "Prayer successful")
    {
        OnExecute = ctx =>
        {
            ctx.Success = true;
            ctx.Message = message;
            ctx.FavorAwarded = favor;
            ctx.PrestigeAwarded = prestige;
        };
    }

    /// <summary>
    /// Configures the pipeline to simulate a failed prayer.
    /// </summary>
    public void SetupFailure(string message = "Prayer failed")
    {
        OnExecute = ctx =>
        {
            ctx.Success = false;
            ctx.Message = message;
        };
    }
}