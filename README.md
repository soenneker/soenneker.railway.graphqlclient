# Soenneker.Railway.GraphQlClient

Typed .NET 10 GraphQL queries, mutations, inputs and response models generated from Railway's live schema. The schema snapshot is in `graphql.schema`; generated code is in `src/Soenneker.Railway.GraphQlClient/Generated`.

```csharp
using System.Net.Http.Headers;
using Soenneker.Railway.GraphQlClient;

using var http = new HttpClient
{
    BaseAddress = new Uri("https://backboard.railway.com/graphql/v2")
};
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var client = new RailwayGraphQlClient(new GraphQlHttpClient(http));
var response = await client.GetProject.Execute(new GetProjectVariables { Id = projectId });
if (response.HasErrors)
    throw new InvalidOperationException(response.Errors![0].Message);
var project = response.Data?.Project;
```

Use `client.Project.Create.Execute(new ProjectCreateVariables { Input = new ProjectCreateInput { Name = "example", WorkspaceId = workspaceId } })` to create a project. Mutations modify Railway resources. This example is not executed by tests.

The HTTP transport uses the exact base URI, preserves GraphQL errors and partial data, and throws on unsuccessful HTTP status codes. `GetValue` is a convenience accessor; use `Execute` when you need error details. The caller owns the supplied `HttpClient`.

Generated operations use automatically selected fields. To choose a smaller selection set, use `new GraphQlHttpClient(http).Execute<T>(query, variables)` with your own response model. GraphQL subscription streaming and multipart uploads are not implemented by this HTTP transport.

Dates use `DateTimeOffset`, enums use GraphQL string names, and custom scalars (including JSON configuration and BigInt) use `JsonElement` to preserve arbitrary data and numeric precision. GraphQL interfaces use their generated C# interfaces; deserialize concrete response models for custom polymorphic selections.

For dependency injection use the companion `Soenneker.Railway.GraphQlClientUtil` package. Regenerate with `Soenneker.Railway.Runners.GraphQlClient`; do not edit generated files manually.

API documentation: https://docs.railway.com/integrations/api
