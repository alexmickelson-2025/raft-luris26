using classlibrary;

public record VoteRequestData
{
    public IServerNode Candidate { get; set; }
    public int term { get; set; }
}