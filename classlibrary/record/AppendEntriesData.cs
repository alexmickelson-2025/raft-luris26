using System.Dynamic;
using classlibrary;

public record AppendEntriesData
{
    public string leader { get; set; }
    public int term { get; set; }
    public List<LogEntry> logEntries { get; set; }
    public int leaderCommitIndex { get; set; }
    public int prevLogIndex { get; set; }
    public int prevLogTerm { get; set; }
}