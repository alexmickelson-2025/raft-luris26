using classlibrary;

public record VoteRequestData
{
    public string Candidate { get; set; }
    public int term { get; set; }
}