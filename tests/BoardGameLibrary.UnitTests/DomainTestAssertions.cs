using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.UnitTests;

internal static class DomainTestAssertions
{
    internal static DomainException Throws(
        string expectedCode,
        Action action,
        DomainErrorType expectedType = DomainErrorType.Validation)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedType, exception.Type);
        return exception;
    }

    internal static void VersionIsSeven(Guid identifier)
    {
        Assert.NotEqual(Guid.Empty, identifier);
        Assert.Equal('7', identifier.ToString("D")[14]);
    }
}
