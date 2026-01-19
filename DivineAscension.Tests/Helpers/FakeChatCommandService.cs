using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation for testing command registration.
/// Tracks registered commands without executing them.
/// </summary>
public class FakeChatCommandService : IChatCommandService
{
    public List<string> RegisteredCommands { get; } = new();
    public List<string> RegisteredSubCommands { get; } = new();
    private readonly Mock<ICommandBuilder> _mockBuilder;

    public FakeChatCommandService()
    {
        _mockBuilder = new Mock<ICommandBuilder>();

        // Setup the mock to track subcommands
        _mockBuilder.Setup(c => c.BeginSubCommand(It.IsAny<string>()))
            .Callback<string>(name => RegisteredSubCommands.Add(name))
            .Returns(_mockBuilder.Object);

        // Setup other fluent methods to return self
        _mockBuilder.Setup(c => c.WithDescription(It.IsAny<string>())).Returns(_mockBuilder.Object);
        _mockBuilder.Setup(c => c.RequiresPlayer()).Returns(_mockBuilder.Object);
        _mockBuilder.Setup(c => c.RequiresPrivilege(It.IsAny<string>())).Returns(_mockBuilder.Object);
        _mockBuilder.Setup(c => c.WithArgs(It.IsAny<ICommandArgumentParser[]>())).Returns(_mockBuilder.Object);
        _mockBuilder.Setup(c => c.HandleWith(It.IsAny<OnCommandDelegate>())).Returns(_mockBuilder.Object);
        _mockBuilder.Setup(c => c.EndSubCommand()).Returns(_mockBuilder.Object);
    }

    public ICommandBuilder Create(string command)
    {
        RegisteredCommands.Add(command);
        return _mockBuilder.Object;
    }

    public CommandArgumentParsers Parsers => null!; // Not needed for basic registration tests
}
