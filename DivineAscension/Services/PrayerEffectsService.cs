using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Services;

/// <summary>
/// Handles visual and audio effects for prayer interactions at altars.
/// Triggers player emotes, spawns domain-colored particles, and plays tier-based sounds.
/// </summary>
[ExcludeFromCodeCoverage]
public class PrayerEffectsService : IPrayerEffectsService
{
    private readonly IWorldService _worldService;
    private readonly IChatCommandService _chatCommandService;
    private readonly ILoggerWrapper _logger;

    public PrayerEffectsService(
        IWorldService worldService,
        IChatCommandService chatCommandService,
        ILoggerWrapper logger)
    {
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _chatCommandService = chatCommandService ?? throw new ArgumentNullException(nameof(chatCommandService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void PlayPrayerEffects(IPlayer player, BlockPos altarPosition, int holySiteTier, DeityDomain domain)
    {
        // Validate inputs
        if (player?.Entity == null || holySiteTier < 1)
        {
            _logger.Debug("[DivineAscension] Skipping prayer effects - invalid player or tier");
            return;
        }

        _logger.Notification($"[DivineAscension] Playing prayer effects for tier {holySiteTier} at {altarPosition}");

        try
        {
            var world = _worldService.World;
            if (world == null)
            {
                _logger.Error("[DivineAscension] World accessor is null!");
                return;
            }

            // 1. Trigger bow emote via the ChatCommands service (handles network sync)
            var serverPlayer = player as IServerPlayer;
            if (serverPlayer != null)
            {
                _chatCommandService.ExecuteUnparsed("/emote bow", serverPlayer);
                _logger.Notification("[DivineAscension] Triggered /emote bow via ChatCommandService");
            }
            else
            {
                _logger.Debug("[DivineAscension] Could not trigger emote - player is not IServerPlayer");
            }

            // 2. Spawn particles from each side and top of the altar
            var baseX = altarPosition.X + 0.5;
            var baseY = altarPosition.Y + 0.5;
            var baseZ = altarPosition.Z + 0.5;

            // Spawn from all 4 sides + top with outward/upward velocity
            var spawnPoints = new[]
            {
                (new Vec3d(baseX - 0.5, baseY, baseZ), new Vec3f(-0.5f, 0.5f, 0)), // West
                (new Vec3d(baseX + 0.5, baseY, baseZ), new Vec3f(0.5f, 0.5f, 0)), // East
                (new Vec3d(baseX, baseY, baseZ - 0.5), new Vec3f(0, 0.5f, -0.5f)), // North
                (new Vec3d(baseX, baseY, baseZ + 0.5), new Vec3f(0, 0.5f, 0.5f)), // South
                (new Vec3d(baseX, baseY + 0.5, baseZ), new Vec3f(0, 0.8f, 0)) // Top
            };

            foreach (var (pos, velocity) in spawnPoints)
            {
                var particleProps = CreateDivineParticles(holySiteTier, pos, velocity, domain);
                world.SpawnParticles(particleProps, null);
            }

            _logger.Notification("[DivineAscension] Spawned particles from 5 points (4 sides + top) of altar");

            // 3. Play sound (pass null to send to all nearby players)
            var soundAsset = GetBellSoundForTier(holySiteTier);
            _logger.Notification($"[DivineAscension] Playing sound: {soundAsset}");
            world.PlaySoundAt(
                soundAsset,
                altarPosition.X + 0.5,
                altarPosition.Y + 0.5,
                altarPosition.Z + 0.5,
                null, // null = send to all nearby players
                false, // randomizePitch
                32f, // range
                1.0f // volume
            );

            _logger.Notification("[DivineAscension] Prayer effects completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error playing prayer effects: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Creates divine particle properties scaled by holy site tier.
    /// Higher tiers produce more intense, longer-lasting particle effects.
    /// </summary>
    /// <param name="tier">Holy site tier (1-3)</param>
    /// <param name="basePos">Base position to spawn particles at</param>
    /// <param name="outwardVelocity">Direction particles should travel (outward from altar side)</param>
    /// <param name="domain">The deity domain (determines particle color)</param>
    /// <returns>Configured particle properties with domain-themed color</returns>
    internal SimpleParticleProperties CreateDivineParticles(int tier, Vec3d basePos, Vec3f outwardVelocity,
        DeityDomain domain)
    {
        // Tier-based scaling: quantity, lifetime, size
        // Divide by 5 since we're spawning from 5 points (4 sides + top)
        var (minQty, addQty, lifetime, minSize, maxSize) = tier switch
        {
            1 => (22f, 23f, 1.5f, 0.1f, 0.2f), // Shrine: Sparkles (110/5 = 22 per point)
            2 => (45f, 45f, 2.0f, 0.15f, 0.3f), // Temple: Glow (225/5 = 45 per point)
            3 => (68f, 67f, 2.5f, 0.2f, 0.4f), // Cathedral: Intense burst (340/5 = 68 per point)
            _ => (22f, 23f, 1.5f, 0.1f, 0.2f)
        };

        // Domain-based color (ARGB format: Alpha, Red, Green, Blue)
        var color = domain switch
        {
            DeityDomain.Craft => ColorUtil.ToRgba(255, 255, 60, 40), // Red/orange for forging
            DeityDomain.Wild => ColorUtil.ToRgba(255, 50, 220, 80), // Green for nature
            DeityDomain.Conquest => ColorUtil.ToRgba(255, 140, 20, 30), // Crimson/blood for battle
            DeityDomain.Harvest => ColorUtil.ToRgba(255, 255, 200, 50), // Golden for crops
            DeityDomain.Stone => ColorUtil.ToRgba(255, 160, 140, 120), // Brown/tan for earth
            _ => ColorUtil.ToRgba(255, 255, 255, 200) // White/silver default
        };

        var particles = new SimpleParticleProperties
        {
            MinQuantity = minQty,
            AddQuantity = addQty,
            Color = color,
            MinPos = basePos,
            AddPos = new Vec3d(0.2, 0.5, 0.2),
            MinVelocity = outwardVelocity,
            AddVelocity = new Vec3f(0.2f, 0.3f, 0.2f),
            LifeLength = lifetime,
            GravityEffect = -0.1f,
            MinSize = minSize,
            MaxSize = maxSize,
            ParticleModel = EnumParticleModel.Quad
        };

        return particles;
    }

    /// <summary>
    /// Gets the appropriate sound effect for the holy site tier.
    /// Uses vanilla Vintage Story collect sounds for satisfying feedback.
    /// </summary>
    /// <param name="tier">Holy site tier (1-3)</param>
    /// <returns>Asset location for the sound effect</returns>
    internal AssetLocation GetBellSoundForTier(int tier)
    {
        return tier switch
        {
            1 => new AssetLocation("game:sounds/player/collect1"), // Soft chime
            2 => new AssetLocation("game:sounds/player/collect2"), // Medium chime
            3 => new AssetLocation("game:sounds/player/collect3"), // Rich chime
            _ => new AssetLocation("game:sounds/player/collect1")
        };
    }
}
