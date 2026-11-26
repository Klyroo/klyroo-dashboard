namespace Klyroo.Dashboard;

/// <summary>Options of Klyroo.Dashboard, set with <c>services.AddKlyrooDashboard(options =&gt; ...)</c>.</summary>
public sealed class KlyrooDashboardOptions
{
    /// <summary>
    /// The largest data, in bytes as sent, that one resource hands from the prerender to the interactive render through
    /// <see cref="DashboardPersistence"/> (8 KB by default). The size counts the escaping and the two base64 encodings Blazor adds, so the JSON
    /// of the data itself is about a third of it. A resource over it is not handed over: the interactive render loads it again once. All the
    /// resources, and the other Klyroo libraries of the page (Klyroo.Table lists), also share <c>KlyrooOptions.PersistedStateMaxBytes</c> (28 KB as
    /// sent by default): the message that starts an Interactive Server circuit carries all of it, and SignalR refuses it past 32 KB.
    /// </summary>
    public int PersistMaxBytes { get; set; } = 8 * 1024;

    /// <summary>
    /// The oldest handed-over data <see cref="DashboardPersistence"/> restores (5 minutes by default), measured from when it was loaded. A prerender
    /// hands its data over within seconds, but .NET can also persist the state of an Interactive Server circuit when it pauses and give it back
    /// when the circuit resumes, possibly much later: older data is dropped and the resource loads again.
    /// </summary>
    public TimeSpan PersistMaxAge { get; set; } = TimeSpan.FromMinutes(5);
}
