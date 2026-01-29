using Cognify.Server.Controllers;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Controllers;

public class WeatherForecastControllerTests
{
    [Fact]
    public void Get_ShouldReturnFiveForecasts()
    {
        var controller = new WeatherForecastController();

        var result = controller.Get().ToList();

        result.Should().HaveCount(5);
        result.All(f => !string.IsNullOrWhiteSpace(f.Summary)).Should().BeTrue();
    }
}
