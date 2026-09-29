using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Railway.GraphQlClient.Tests;

public sealed class GraphQlAotTests
{
    [Test]
    public async Task Generated_variables_and_response_use_generated_metadata()
    {
        using var handler = new ResponseHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid/graphql") };
        var client = new GraphQlHttpClient(http);
        var builder = new GetAgentUsageRequestBuilder(client);
        var response = await builder.Execute(new GetAgentUsageVariables { WorkspaceId = "workspace-test" });
        if (response.Data is null || response.HasErrors)
            throw new Exception("The generated GraphQL response was not deserialized.");
        using var request = JsonDocument.Parse(handler.Body!);
        if (request.RootElement.GetProperty("variables").GetProperty("workspaceId").GetString() != "workspace-test")
            throw new Exception("The generated variables were not serialized.");
    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        internal string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"agentUsage\":null}}", System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
