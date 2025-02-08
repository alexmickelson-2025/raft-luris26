using System.Text.Json;
using System.Text.Json.Serialization;
using classlibrary;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");



var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_URL environment variable not set");
var nodeInternalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL") ?? throw new Exception("NODE_INTERVAL environment variable not set");
var app = builder.Build();

var ServicesName = nodeId + "#Node";

List<IServerNode> otherNodes = otherNodesRaw
    .Split(";")
    .Select(s => (IServerNode)new HttpRpcOtherNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
    .ToList();

var node = new ServerNode(vote: false, neighbors: otherNodes, id: nodeId);
node.StartSimulationLoop();

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


// Receive RPC request
app.MapPost("/request/appendEntries", async (AppendEntriesData request) =>
{
    await node.AppendEntries(request);
    return Results.Ok();
});

//Receive `RequestVote` RPC request
app.MapPost("/request/vote", async (VoteRequestData request) =>
{
    bool voteGranted = await node.RequestVoteAsync(request);
    return Results.Ok(new { VoteGranted = voteGranted, Term = node.Term });
});

// Receive `AppendEntries` response
app.MapPost("/response/appendEntries", async (RespondEntriesData response) =>
{
    await node.RespondToAppendEntriesAsync(response);
    return Results.Ok();
});

// Receive `RequestVote` response
app.MapPost("/response/vote", async (VoteResponseData response) =>
{
    node.respondRPC(response);
    return Results.Ok();
});

//Send command to leader
app.MapPost("/request/command", async (LogEntry command) =>
{
    bool success = await node.ReceiveClientCommandAsync(command);
    return Results.Ok(new { Success = success });
});
app.MapGet("/logs", () =>
{
    return Results.Json(new { NodeId = node.Id, Logs = node.GetLogEntries() });
});

// app.UseHttpsRedirection();
app.Run();