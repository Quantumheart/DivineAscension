using DivineAscension.Services;
using DivineAscension.Systems.Altar.Pipeline;
using Moq;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline;

public class PrayerPipelineTests
{
    private readonly Mock<ILoggerWrapper> _logger;

    public PrayerPipelineTests()
    {
        _logger = new Mock<ILoggerWrapper>();
    }

    private static PrayerContext CreateContext() =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = 0,
            Player = null!
        };

    [Fact]
    public void Constructor_NullSteps_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PrayerPipeline(null!, _logger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PrayerPipeline(Array.Empty<IPrayerStep>(), null!));
    }

    [Fact]
    public void Execute_EmptyPipeline_ReturnsContext()
    {
        // Arrange
        var pipeline = new PrayerPipeline(Array.Empty<IPrayerStep>(), _logger.Object);
        var context = CreateContext();

        // Act
        var result = pipeline.Execute(context);

        // Assert
        Assert.Same(context, result);
    }

    [Fact]
    public void Execute_AllStepsExecuted_InOrder()
    {
        // Arrange
        var executionOrder = new List<int>();

        var step1 = new Mock<IPrayerStep>();
        step1.Setup(x => x.Name).Returns("Step1");
        step1.Setup(x => x.Execute(It.IsAny<PrayerContext>()))
            .Callback(() => executionOrder.Add(1));

        var step2 = new Mock<IPrayerStep>();
        step2.Setup(x => x.Name).Returns("Step2");
        step2.Setup(x => x.Execute(It.IsAny<PrayerContext>()))
            .Callback(() => executionOrder.Add(2));

        var step3 = new Mock<IPrayerStep>();
        step3.Setup(x => x.Name).Returns("Step3");
        step3.Setup(x => x.Execute(It.IsAny<PrayerContext>()))
            .Callback(() => executionOrder.Add(3));

        var pipeline = new PrayerPipeline(
            new[] { step1.Object, step2.Object, step3.Object },
            _logger.Object);
        var context = CreateContext();

        // Act
        pipeline.Execute(context);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
    }

    [Fact]
    public void Execute_StepSetsIsComplete_StopsExecution()
    {
        // Arrange
        var step1 = new Mock<IPrayerStep>();
        step1.Setup(x => x.Name).Returns("Step1");
        step1.Setup(x => x.Execute(It.IsAny<PrayerContext>()))
            .Callback<PrayerContext>(ctx => ctx.IsComplete = true);

        var step2 = new Mock<IPrayerStep>();
        step2.Setup(x => x.Name).Returns("Step2");

        var pipeline = new PrayerPipeline(
            new[] { step1.Object, step2.Object },
            _logger.Object);
        var context = CreateContext();

        // Act
        pipeline.Execute(context);

        // Assert
        step1.Verify(x => x.Execute(context), Times.Once);
        step2.Verify(x => x.Execute(It.IsAny<PrayerContext>()), Times.Never);
    }

    [Fact]
    public void Execute_StepThrowsException_SetsFailureAndStops()
    {
        // Arrange
        var step1 = new Mock<IPrayerStep>();
        step1.Setup(x => x.Name).Returns("FailingStep");
        step1.Setup(x => x.Execute(It.IsAny<PrayerContext>()))
            .Throws(new InvalidOperationException("Test error"));

        var step2 = new Mock<IPrayerStep>();
        step2.Setup(x => x.Name).Returns("Step2");

        var pipeline = new PrayerPipeline(
            new[] { step1.Object, step2.Object },
            _logger.Object);
        var context = CreateContext();

        // Act
        pipeline.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.NotNull(context.Message);
        step2.Verify(x => x.Execute(It.IsAny<PrayerContext>()), Times.Never);
        _logger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Test error"))), Times.Once);
    }

    [Fact]
    public void Execute_LogsEachStep()
    {
        // Arrange
        var step = new Mock<IPrayerStep>();
        step.Setup(x => x.Name).Returns("TestStep");

        var pipeline = new PrayerPipeline(new[] { step.Object }, _logger.Object);
        var context = CreateContext();

        // Act
        pipeline.Execute(context);

        // Assert
        _logger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("TestStep"))), Times.AtLeastOnce);
    }
}