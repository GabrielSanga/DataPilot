namespace DataPilot.Core.Tests.Helpers;

public sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;
    public override DateTimeOffset GetUtcNow() => Agora;
}
