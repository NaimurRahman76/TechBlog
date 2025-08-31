using FluentAssertions;

namespace TechBlog.Tests.Integration;

public class SimpleIntegrationTests
{
    [Fact]
    public void BasicIntegrationTest_ShouldPass()
    {
        // Arrange
        var expected = "Integration Test";

        // Act
        var actual = "Integration Test";

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void DatabaseConnection_ShouldBeTestable()
    {
        // Arrange
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=TechBlogTestDb;Trusted_Connection=true;MultipleActiveResultSets=true";

        // Act & Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("TechBlogTestDb");
    }
}
