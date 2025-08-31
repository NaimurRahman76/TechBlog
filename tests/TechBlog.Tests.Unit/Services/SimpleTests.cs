using FluentAssertions;

namespace TechBlog.Tests.Unit.Services;

public class SimpleTests
{
    [Fact]
    public void BasicTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void StringTest_ShouldPass()
    {
        // Arrange
        var expected = "TechBlog";

        // Act
        var actual = "TechBlog";

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void NumberTest_ShouldPass()
    {
        // Arrange
        var expected = 42;

        // Act
        var actual = 21 + 21;

        // Assert
        actual.Should().Be(expected);
    }
}
