using System.Net;
using System.Text.RegularExpressions;

namespace RentalPortal.Integration.Tests.Infrastructure;

public static partial class HttpClientExtensions
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    public static async Task LoginAsync(this HttpClient client, string email)
    {
        var fields = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = TestUsers.Password
        };

        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", fields);

        if (response.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException($"Login failed for {email}: {response.StatusCode}.");
    }

    /// <summary>
    /// Reads the antiforgery token from <paramref name="pageUrl"/>, then posts the form with it.
    /// </summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client,
        string pageUrl,
        string postUrl,
        Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(pageUrl);
        var match = AntiforgeryTokenRegex().Match(html);

        if (!match.Success)
            throw new InvalidOperationException($"No antiforgery token on {pageUrl}.");

        var form = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value)
        };

        return await client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    /// <summary>
    /// Posts a form as a signed-in user, taking the token from the home page (the logout form).
    /// </summary>
    public static Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client,
        string postUrl,
        Dictionary<string, string> fields) =>
        client.PostFormAsync("/", postUrl, fields);
}
