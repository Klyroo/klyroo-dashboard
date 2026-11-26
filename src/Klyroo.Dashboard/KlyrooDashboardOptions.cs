namespace Klyroo.Dashboard;

/// <summary>Options of Klyroo.Dashboard, set with <c>services.AddKlyrooDashboard(options =&gt; ...)</c>.</summary>
public sealed class KlyrooDashboardOptions
{
    /// <summary>
    /// The largest data, in bytes as sent, that one resource hands from the prerender to the interactive render through
    /// <see cref="DashboardPersistence"/> (8 KB by default). The size counts the escaping and the two base64 encodings Blazor adds, so the JSON
    /// of the data itself is roughly half of it. A resource over it is not handed over: the interactive render loads it again once.
    /// </summary>
    public int PersistMaxBytes { get; set; } = 8 * 1024;

    /// <summary>
    /// The largest data, in bytes as sent, that all the dashboard resources of one prerendered page hand over together (12 KB by default).
    /// The browser sends everything persisted back to the server when an Interactive Server circuit starts, in one message that
    /// SignalR refuses past 32 KB by default, so keep this and the limits of other libraries (Klyroo.Table) under that together.
    /// </summary>
    public int PersistPageMaxBytes { get; set; } = 12 * 1024;

    /// <summary>
    /// The oldest handed-over data <see cref="DashboardPersistence"/> restores (5 minutes by default), measured from when it was loaded. A prerender
    /// hands its data over within seconds, but .NET can also persist the state of an Interactive Server circuit when it pauses and give it back
    /// when the circuit resumes, possibly much later: older data is dropped and the resource loads again.
    /// </summary>
    public TimeSpan PersistMaxAge { get; set; } = TimeSpan.FromMinutes(5);
}
