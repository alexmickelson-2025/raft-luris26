using System.Net;
using classlibrary;

public record NodeData
{
    public string? Id { get; set; }
    public string? CurrentLiderID { get; set; }
    // public DateTime ElectionStartTime { get; set; }
    // public TimeSpan ElectionTimeout { get; set; }
    public int Term { get; set; }
    public int CommitIndex { get; set; }
    public List<LogEntry>? Log { get; set; }
    public string State { get; set; }
}
