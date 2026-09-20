using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Railway.GraphQlClient.Tests;

public sealed class RailwayGraphQlClientTests
{
    [Test]
    public async Task SendsTypedQueryToExactEndpoint()
    {
        using var handler = new Handler(async request =>
        {
            if (request.Method != HttpMethod.Post || request.RequestUri?.AbsoluteUri != "https://backboard.railway.com/graphql/v2")
                throw new Exception("Incorrect GraphQL endpoint or method.");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            if (body.RootElement.GetProperty("variables").GetProperty("id").GetString() != "project-123" ||
                !body.RootElement.GetProperty("query").GetString()!.Contains("project(id: $id)"))
                throw new Exception("Typed query or variables were not serialized correctly.");
            return Json("""{"data":{"project":{"id":"project-123","name":"Example"}}}""");
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backboard.railway.com/graphql/v2") };
        var client = new RailwayGraphQlClient(new GraphQlHttpClient(http));
        var result = await client.GetProject.GetValue(new GetProjectVariables { Id = "project-123" });
        if (result?.Id != "project-123" || result.Name != "Example")
            throw new Exception("Typed response was not deserialized.");
    }

    [Test]
    public async Task PreservesGraphQlErrorsAndPartialData()
    {
        using var handler = new Handler(_ => Task.FromResult(Json("""{"data":{"project":{"id":"p"}},"errors":[{"message":"Forbidden","path":["project","name"]}]}""")));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/graphql") };
        var response = await new RailwayGraphQlClient(new GraphQlHttpClient(http)).GetProject.Execute(new GetProjectVariables { Id = "p" });
        if (!response.HasErrors || response.Errors![0].Message != "Forbidden" || response.Data?.Project.Id != "p")
            throw new Exception("GraphQL errors or partial data were lost.");
    }

    [Test]
    public void HandlesEnumsDatesAndArbitraryJsonScalars()
    {
        var deployment = JsonSerializer.Deserialize<Deployment>("""{"status":"SUCCESS","createdAt":"2026-09-20T00:00:00Z","meta":{"nested":[1,true,"value"]}}""")!;
        if (deployment.Status != DeploymentStatus.SUCCESS || deployment.CreatedAt.Year != 2026 ||
            deployment.Meta!.Value.GetProperty("nested")[2].GetString() != "value")
            throw new Exception("Railway scalar or enum conversion failed.");
        if (JsonSerializer.Serialize(DeploymentStatus.SUCCESS) != "\"SUCCESS\"")
            throw new Exception("GraphQL enums must serialize as names.");
        var bucket = JsonSerializer.Deserialize<BucketInstanceDetails>("""{"objectCount":123456789012345678901234567890,"sizeBytes":1}""")!;
        if (bucket.ObjectCount.GetRawText() != "123456789012345678901234567890")
            throw new Exception("BigInt precision was lost.");
    }

    [Test]
    public async Task PropagatesHttpFailures()
    {
        using var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/graphql") };
        try { await new GraphQlHttpClient(http).Execute<object>("{ me { id } }"); }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized) { return; }
        throw new Exception("HTTP authentication failure was not propagated.");
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(request);
    }
}

