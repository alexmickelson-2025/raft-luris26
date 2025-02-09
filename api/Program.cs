using System.Text.Json;
using System.Text.Json.Serialization;
using classlibrary;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES not set");
var nodeInternalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL") ?? throw new Exception("NODE_INTERVAL not set");

var app = builder.Build();

List<IServerNode> otherNodes = otherNodesRaw
    .Split(";")
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .Select(s => (IServerNode)new HttpRpcOtherNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
    .ToList();

var node = new ServerNode(vote: false, neighbors: otherNodes, id: nodeId);

Console.WriteLine($"✅ [{nodeId}] Neighbors assigned: {string.Join(", ", otherNodes.Select(n => n.Id))}");

Task.Delay(2000).ContinueWith(_ =>
{
    Console.WriteLine($"🚀 Starting simulation for Node {nodeId}");
    node.StartSimulationLoop();
});

app.MapGet("/", () => "raft");

app.MapGet("/nodeData", () =>
{
    return Results.Json(new
    {
        Id = node.Id,
        State = node.State.ToString(),
        Term = node.Term,
        CurrentTermLeader = node.GetCurrentLeader()?.Id ?? "None",
        CommittedEntryIndex = node.CommitIndex,
        Logs = node.Log
    }, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    });
});

app.MapPost("/request/appendEntries", async (AppendEntriesData request) =>
{
    await node.AppendEntries(request);
    return Results.Ok();
});

app.MapPost("/request/vote", async (VoteRequestData request) =>
{
    if (request.Candidate == null)
    {
        Console.WriteLine("❌ Error: Candidate is null in VoteRequestData!");
        return Results.BadRequest("Candidate cannot be null");
    }

    Console.WriteLine($"🗳️ Vote requested for Candidate={request.Candidate}, Term={request.term}");

    bool voteGranted = await node.RequestVoteAsync(request);
    return Results.Ok(new { VoteGranted = voteGranted, Term = node.Term });
});

app.MapPost("/response/appendEntries", async (RespondEntriesData response) =>
{
    await node.RespondToAppendEntriesAsync(response);
    return Results.Ok();
});

app.MapPost("/response/vote", async (VoteResponseData response) =>
{
    await Task.Run(() => node.respondRPC(response));
    return Results.Ok();
});

app.MapPost("/request/command", async (LogEntry command) =>
{
    bool success = await node.ReceiveClientCommandAsync(command);
    return Results.Ok(new { Success = success });
});

app.MapGet("/logs", () =>
{
    return Results.Json(new { NodeId = node.Id, Logs = node.GetLogEntries() });
});

app.Run();
