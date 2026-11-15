using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests what a request that Sonarr answers with an error status gives back, which is what a user reads when something
/// fails and what their error handling acts on.
/// </summary>
public sealed class ErrorResponseTests
{
	/// <summary>
	/// The kinds of body that Sonarr, or a server in front of it, sends with an error status.
	/// </summary>
	public enum ErrorBody
	{
		/// <summary>One error object, as Sonarr sends for an exception.</summary>
		Object,
		/// <summary>An array of validation failures, as Sonarr sends for a request that it turns away.</summary>
		ValidationFailures,
		/// <summary>Problem details, as Sonarr sends for a body that it can't read.</summary>
		ProblemDetails,
		/// <summary>No body, as Sonarr sends for a wrong API key.</summary>
		Empty,
		/// <summary>An HTML page, as a reverse proxy sends.</summary>
		Html,
		/// <summary>JSON that doesn't parse.</summary>
		Malformed,
	}

	/// <summary>
	/// The message of the error in <see cref="ErrorBody.Object"/>.
	/// </summary>
	private const string OBJECT_MESSAGE = "Series with ID 7 does not exist";

	/// <summary>
	/// The content type and text of each kind of body, or <see langword="null"/> for none.
	/// </summary>
	private static readonly Dictionary<ErrorBody, (string MediaType, string Text)?> s_bodies = new()
	{
		[ErrorBody.Object] = ("application/json", $$"""
			{
				"message": "{{OBJECT_MESSAGE}}",
				"description": "NzbDrone.Core.Datastore.ModelNotFoundException: {{OBJECT_MESSAGE}}\n   at NzbDrone.Core.Datastore.BasicRepository`1.Get(Int32 id)"
			}
			"""),
		[ErrorBody.ValidationFailures] = ("application/json", """
			[
				{
					"propertyName": "Path",
					"errorMessage": "'Path' must not be empty.",
					"attemptedValue": "",
					"severity": "error",
					"errorCode": "NotEmptyValidator"
				},
				{
					"propertyName": "QualityProfileId",
					"errorMessage": "'Quality Profile Id' must be greater than '0'.",
					"attemptedValue": 0,
					"severity": "error",
					"errorCode": "GreaterThanValidator"
				}
			]
			"""),
		[ErrorBody.ProblemDetails] = ("application/problem+json", """
			{
				"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
				"title": "One or more validation errors occurred.",
				"status": 400,
				"errors": {
					"$.name": ["The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 13."]
				},
				"traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
			}
			"""),
		[ErrorBody.Empty] = null,
		[ErrorBody.Html] = ("text/html", "<html><head><title>502 Bad Gateway</title></head><body><h1>502 Bad Gateway</h1><hr>nginx</body></html>"),
		[ErrorBody.Malformed] = ("application/json", """{"message": "The server was"""),
	};

	/// <summary>
	/// The text and the error category of each status that the tests answer with.
	/// </summary>
	private static readonly Dictionary<int, (string Text, ErrorCategory Category)> s_statuses = new()
	{
		[400] = ("400 (Bad Request)", ErrorCategory.InvalidData),
		[401] = ("401 (Unauthorized)", ErrorCategory.AuthenticationError),
		[403] = ("403 (Forbidden)", ErrorCategory.AuthenticationError),
		[404] = ("404 (Not Found)", ErrorCategory.ObjectNotFound),
		[405] = ("405 (Method Not Allowed)", ErrorCategory.InvalidOperation),
		[409] = ("409 (Conflict)", ErrorCategory.NotSpecified),
		[422] = ("422 (Unprocessable Entity)", ErrorCategory.InvalidArgument),
		[500] = ("500 (Internal Server Error)", ErrorCategory.ConnectionError),
		[502] = ("502 (Bad Gateway)", ErrorCategory.ConnectionError),
		[503] = ("503 (Service Unavailable)", ErrorCategory.ConnectionError),
		[504] = ("504 (Gateway Timeout)", ErrorCategory.ConnectionError),
	};

	/// <summary>
	/// The error responses that the tests answer with, each a status and a kind of body.
	/// </summary>
	private static readonly (int Status, ErrorBody Body)[] s_errors =
	[
		(400, ErrorBody.ValidationFailures),
		(400, ErrorBody.ProblemDetails),
		(401, ErrorBody.Empty),
		(403, ErrorBody.Html),
		(404, ErrorBody.Object),
		(404, ErrorBody.Empty),
		(405, ErrorBody.Empty),
		(409, ErrorBody.Object),
		(422, ErrorBody.Object),
		(500, ErrorBody.Object),
		(502, ErrorBody.Html),
		(503, ErrorBody.Malformed),
		(504, ErrorBody.Empty),
	];

	/// <summary>
	/// Every send method gives an error response as an error result, never as an exception. The result has the
	/// response's status code, the request's absolute URL, and the category that the status calls for, and it's
	/// ignorable for a 404 alone. Its message is the URL, then the status, then Sonarr's message: one error's message,
	/// each validation failure's message on its own line, or a problem's title and then each of its errors after the
	/// field that it's about. A body that holds no message, such as an empty one, a proxy's HTML page, or JSON that
	/// doesn't parse, gives the module's general message in place of Sonarr's. A method that returns a value gives a 404
	/// as the module's own <c>Not found.</c> error, which a cmdlet reports in its own words.
	/// </summary>
	/// <param name="method">The send method, by its name in <see cref="ClientCalls.ApiMethods"/>.</param>
	/// <param name="status">The response's status code.</param>
	/// <param name="body">The kind of body that the response carries.</param>
	[Theory]
	[MemberData(nameof(ApiErrors))]
	public void GivesAnErrorResponseAsAnErrorResultWithSonarrsMessage(string method, int status, ErrorBody body)
	{
		using ServiceProvider provider = ModuleServices.Build(AnswerWith(status, body));
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		SonarrClientResult result = ClientCalls.ApiMethods[method](client, TestContext.Current.CancellationToken);

		Assert.True(result.IsError, $"{method} succeeded.");
		SonarrErrorRecord error = result.Error;
		(string statusText, ErrorCategory category) = s_statuses[status];
		bool returnsValue = method.Contains("Get", StringComparison.Ordinal) || method.EndsWith("<T>", StringComparison.Ordinal);

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)status, result.StatusCode),
			() => Assert.Equal(ClientCalls.AbsoluteUrl, result.RequestUrl),
			() => Assert.Equal(category, error.CategoryInfo.Category),
			() => Assert.Equal(status == 404, result.IsIgnoreable),
			() => Assert.Equal(status == 404, error.IsIgnorable),
			() => Assert.Equal(
				status == 404 && returnsValue ? "Not found." : Message(ClientCalls.AbsoluteUrl, statusText, body),
				error.Message));
	}

	public static TheoryData<string, int, ErrorBody> ApiErrors
	{
		get
		{
			TheoryData<string, int, ErrorBody> data = [];
			foreach (string method in ClientCalls.ApiMethods.Keys)
			{
				foreach ((int status, ErrorBody body) in s_errors)
				{
					data.Add(method, status, body);
				}
			}

			return data;
		}
	}

	/// <summary>
	/// A connection test that Sonarr answers with an error status gives an error result, never an exception, with the
	/// status code, the test's absolute URL, and the category <see cref="ErrorCategory.ResourceUnavailable"/>, whose
	/// message is the <c>message</c> of the error object that the body holds, or else the status, which is what
	/// <c>Connect-SonarrInstance</c> and <c>Test-SonarrHost</c> report.
	/// </summary>
	/// <param name="status">The response's status code.</param>
	/// <param name="body">The kind of body that the response carries.</param>
	[Theory]
	[MemberData(nameof(TestErrors))]
	public void GivesAFailedConnectionTestAsAnErrorResultWithSonarrsMessage(int status, ErrorBody body)
	{
		using ServiceProvider provider = ModuleServices.Build(AnswerWith(status, body));
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		SonarrClientResult result = client.SendTest(TestContext.Current.CancellationToken);

		Assert.True(result.IsError, "The connection test succeeded.");
		SonarrErrorRecord error = result.Error;

		Assert.Multiple(
			() => Assert.Equal((HttpStatusCode)status, result.StatusCode),
			() => Assert.Equal(new Uri(FakeSonarr.BaseUri, "/api/v3/system/status").AbsoluteUri, result.RequestUrl),
			() => Assert.Equal(ErrorCategory.ResourceUnavailable, error.CategoryInfo.Category),
			() => Assert.Equal(body == ErrorBody.Object ? OBJECT_MESSAGE : s_statuses[status].Text, error.Message));
	}

	public static TheoryData<int, ErrorBody> TestErrors
	{
		get
		{
			TheoryData<int, ErrorBody> data = [];
			foreach ((int status, ErrorBody body) in s_errors)
			{
				data.Add(status, body);
			}

			return data;
		}
	}

	/// <summary>
	/// Gives the message that an error result has for a response to a request for the specified URL.
	/// </summary>
	private static string Message(string url, string statusText, ErrorBody body)
	{
		string newLine = Environment.NewLine;
		string details = body switch
		{
			ErrorBody.Object => OBJECT_MESSAGE,
			ErrorBody.ValidationFailures => string.Concat(
				"'Path' must not be empty.", newLine, "'Quality Profile Id' must be greater than '0'."),
			ErrorBody.ProblemDetails => string.Concat(
				"One or more validation errors occurred.",
				newLine,
				"$.name: The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 13."),
			_ => "An exception occurred in response",
		};

		return $"{url} - {statusText}: {details}";
	}

	/// <summary>
	/// Creates a fake instance that answers every request with the specified status and kind of body.
	/// </summary>
	private static FakeSonarr AnswerWith(int status, ErrorBody body)
	{
		return new FakeSonarr("errors", new ProbeLog(), (request, _) =>
		{
			HttpResponseMessage response = new((HttpStatusCode)status) { RequestMessage = request };
			if (s_bodies[body] is (string mediaType, string text))
			{
				response.Content = new StringContent(text, Encoding.UTF8, mediaType);
			}

			return Task.FromResult(response);
		});
	}
}
