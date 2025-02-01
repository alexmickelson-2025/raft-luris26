var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();


var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");

var nodeLogger = app.Services.GetRequiredService<ILogger<HttpRpcOtherNode>>();

var otherNodes = otherNodesRaw.Split(";").Select(nodeInfo =>
{
    var parts = nodeInfo.Split(",");
    int id = int.Parse(parts[0]);
    string url = parts[1];
    return new HttpRpcOtherNode(id, url, nodeLogger);
}).ToList();

app.MapGet("/health", () => "healthy");
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.MapGet("/", () => "Raft Node is Running!");
app.MapGet("/node", () => $"Raft Node {nodeId} is Running!");

app.MapPost("/request/appendEntries", async (AppendEntriesData data) =>
{
    logger.LogInformation($"Received AppendEntries request from Leader {data.leader}");

    return Results.Ok(new { Success = true, Term = data.term });
});

app.MapPost("/request/vote", async (VoteRequestData data) =>
{
    logger.LogInformation($"Received VoteRequest from Candidate {data.Candidate} for Term {data.term}");

    return Results.Ok(new { VoteGranted = true, Term = data.term });
});
app.UseHttpsRedirection();
app.Run();