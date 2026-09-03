using Scada.Editor.Wpf;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class EditorAuthorizationTests
{
    [Theory]
    [InlineData(EditorRole.Developer)]
    [InlineData(EditorRole.Engineer)]
    public void DevelopersAndEngineersCanEnterEngineeringMode(EditorRole role)
    {
        Assert.True(EditorAuthorization.CanEdit(role));
    }

    [Theory]
    [InlineData(EditorRole.Operator)]
    [InlineData(EditorRole.Viewer)]
    public void OperatorsAndViewersCannotEnterEngineeringMode(EditorRole role)
    {
        Assert.False(EditorAuthorization.CanEdit(role));
    }
}
