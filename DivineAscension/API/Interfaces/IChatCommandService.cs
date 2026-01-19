using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Thin wrapper around Vintage Story's ChatCommands API for improved testability.
/// Provides fluent interface for command registration.
/// </summary>
public interface IChatCommandService
{
    /// <summary>
    /// Creates a new root command with the specified name.
    /// Returns a builder for fluent configuration.
    /// </summary>
    ICommandBuilder Create(string command);

    /// <summary>
    /// Gets the underlying parsers for command arguments.
    /// Exposes QuotedString, OptionalQuotedString, Int, OptionalInt, etc.
    /// </summary>
    CommandArgumentParsers Parsers { get; }
}

/// <summary>
/// Fluent builder for constructing chat commands.
/// Mirrors Vintage Story's ChatCommand builder API.
/// </summary>
public interface ICommandBuilder
{
    ICommandBuilder WithDescription(string description);
    ICommandBuilder RequiresPlayer();
    ICommandBuilder RequiresPrivilege(string privilege);
    ICommandBuilder WithArgs(params ICommandArgumentParser[] parsers);
    ICommandBuilder HandleWith(OnCommandDelegate handler);
    ICommandBuilder BeginSubCommand(string name);
    ICommandBuilder EndSubCommand();
}
