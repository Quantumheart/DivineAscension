using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation wrapping IChatCommandAPI.
/// Thin pass-through to Vintage Story's command system.
/// </summary>
public class ServerChatCommandService : IChatCommandService
{
    private readonly IChatCommandApi _chatCommands;

    public ServerChatCommandService(IChatCommandApi chatCommands)
    {
        _chatCommands = chatCommands ?? throw new ArgumentNullException(nameof(chatCommands));
    }

    public ICommandBuilder Create(string command)
    {
        var cmd = _chatCommands.Create(command);
        return new CommandBuilderWrapper(cmd);
    }

    public CommandArgumentParsers Parsers => _chatCommands.Parsers;

    public void ExecuteUnparsed(string commandLine, IServerPlayer player)
    {
        if (player == null) return;

        // Build caller context for command execution
        var caller = new Caller
        {
            Player = player,
            Entity = player.Entity,
            Type = EnumCallerType.Player,
            CallerPrivileges = player.Privileges,
            CallerRole = player.Role?.Code ?? "suplayer",
            Pos = player.Entity?.Pos?.XYZ ?? new Vec3d(),
            FromChatGroupId = GlobalConstants.GeneralChatGroup
        };

        var args = new TextCommandCallingArgs
        {
            Caller = caller,
            RawArgs = new CmdArgs()
        };

        _chatCommands.ExecuteUnparsed(commandLine, args);
    }
}

/// <summary>
/// Wraps ChatCommand to provide ICommandBuilder interface.
/// </summary>
internal class CommandBuilderWrapper : ICommandBuilder
{
    private readonly IChatCommand _command;

    public CommandBuilderWrapper(IChatCommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public ICommandBuilder WithDescription(string description)
    {
        _command.WithDescription(description);
        return this;
    }

    public ICommandBuilder RequiresPlayer()
    {
        _command.RequiresPlayer();
        return this;
    }

    public ICommandBuilder RequiresPrivilege(string privilege)
    {
        _command.RequiresPrivilege(privilege);
        return this;
    }

    public ICommandBuilder WithArgs(params ICommandArgumentParser[] parsers)
    {
        _command.WithArgs(parsers);
        return this;
    }
    
    public ICommandBuilder HandleWith(OnCommandDelegate handler)
    {
        _command.HandleWith(handler);
        return this;
    }

    public ICommandBuilder BeginSubCommand(string name)
    {
        var result = _command.BeginSubCommand(name);
        return new CommandBuilderWrapper(result);
    }

    public ICommandBuilder EndSubCommand()
    {
        var result = _command.EndSubCommand();
        return new CommandBuilderWrapper(result);
    }
}
