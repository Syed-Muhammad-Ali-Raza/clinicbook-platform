using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ClinicBook.Api.IntegrationTests;

public sealed class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string StrongPassword = "Str0ng!Passw0rd#123";

    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Login_And_Me_ShouldWork_WithAuthCookies()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"user_{Guid.NewGuid():N}@example.com";

        var csrf = await GetCsrfTokenAsync(client);
        var registerResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/register", new
        {
            email,
            password = StrongPassword,
            firstName = "Test",
            lastName = "User",
            phoneNumber = "1234567890"
        }, csrf);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        csrf = await GetCsrfTokenAsync(client);
        var loginResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/login", new
        {
            email,
            password = StrongPassword
        }, csrf);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
        var cookieHeader = string.Join(";", setCookies!);
        cookieHeader.Should().Contain("cb_access=");
        cookieHeader.Should().Contain("cb_refresh=");
        cookieHeader.Should().Contain("httponly", "auth cookies must be HttpOnly");

        using var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        loginDoc.RootElement.GetProperty("requiresMfa").GetBoolean().Should().BeFalse();
        loginDoc.RootElement.GetProperty("user").GetProperty("email").GetString().Should().Be(email);

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meDoc = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        meDoc.RootElement.GetProperty("user").GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Login_ShouldReturnGenericUnauthorized_ForUnknownUser()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true });
        var csrf = await GetCsrfTokenAsync(client);

        var response = await PostJsonWithCsrfAsync(client, "/api/v1/auth/login", new
        {
            email = $"missing_{Guid.NewGuid():N}@example.com",
            password = "Wrong!Password#123"
        }, csrf);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("detail").GetString().Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WithoutAuthCookie()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnsafeRequest_WithoutCsrfToken_ShouldBeForbidden()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"csrf_{Guid.NewGuid():N}@example.com";

        var csrf = await GetCsrfTokenAsync(client);
        await PostJsonWithCsrfAsync(client, "/api/v1/auth/register", new
        {
            email,
            password = StrongPassword,
            firstName = "Csrf",
            lastName = "User"
        }, csrf);

        var loginBody = JsonContent.Create(new { email, password = StrongPassword });
        var response = await client.PostAsync("/api/v1/auth/login", loginBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refresh_ShouldRotateToken_AndReuseOfRevokedToken_ShouldInvalidateFamily()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"refresh_{Guid.NewGuid():N}@example.com";

        var csrf = await GetCsrfTokenAsync(client);
        (await PostJsonWithCsrfAsync(client, "/api/v1/auth/register", new
        {
            email,
            password = StrongPassword,
            firstName = "Refresh",
            lastName = "User"
        }, csrf)).StatusCode.Should().Be(HttpStatusCode.Created);

        csrf = await GetCsrfTokenAsync(client);
        var loginResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/login", new
        {
            email,
            password = StrongPassword
        }, csrf);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var originalRefreshCookie = ExtractCookieHeaderValue(loginResponse, "cb_refresh");
        originalRefreshCookie.Should().NotBeNullOrWhiteSpace();

        csrf = await GetCsrfTokenAsync(client);
        var refreshResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/refresh", new { }, csrf);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedRefreshCookie = ExtractCookieHeaderValue(refreshResponse, "cb_refresh");
        rotatedRefreshCookie.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshCookie.Should().NotBe(originalRefreshCookie);

        // Simulate token theft/reuse: present the previously issued (now rotated/revoked) refresh token again.
        using var reuseClient = _factory.CreateClient();
        var reuseCsrf = await GetCsrfTokenAsync(reuseClient);
        var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new { })
        };
        reuseRequest.Headers.Add("Cookie", $"cb_refresh={originalRefreshCookie}; cb_csrf={reuseCsrf}");
        reuseRequest.Headers.Add("X-CSRF-TOKEN", reuseCsrf);
        var reuseResponse = await reuseClient.SendAsync(reuseRequest);

        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Token family should be revoked after reuse detection, so the current rotated token must no longer work.
        using var afterReuseClient = _factory.CreateClient();
        var afterCsrf = await GetCsrfTokenAsync(afterReuseClient);
        var afterReuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new { })
        };
        afterReuseRequest.Headers.Add("Cookie", $"cb_refresh={rotatedRefreshCookie}; cb_csrf={afterCsrf}");
        afterReuseRequest.Headers.Add("X-CSRF-TOKEN", afterCsrf);
        var afterReuseResponse = await afterReuseClient.SendAsync(afterReuseRequest);

        afterReuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldClearCookies_AndRevokeRefreshToken()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true });
        var email = $"logout_{Guid.NewGuid():N}@example.com";

        var csrf = await GetCsrfTokenAsync(client);
        await PostJsonWithCsrfAsync(client, "/api/v1/auth/register", new
        {
            email,
            password = StrongPassword,
            firstName = "Logout",
            lastName = "User"
        }, csrf);

        csrf = await GetCsrfTokenAsync(client);
        var loginResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/login", new { email, password = StrongPassword }, csrf);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshCookie = ExtractCookieHeaderValue(loginResponse, "cb_refresh");

        csrf = await GetCsrfTokenAsync(client);
        var logoutResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/logout", new { }, csrf);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var loggedOutClient = _factory.CreateClient();
        var refreshCsrf = await GetCsrfTokenAsync(loggedOutClient);
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new { })
        };
        refreshRequest.Headers.Add("Cookie", $"cb_refresh={refreshCookie}; cb_csrf={refreshCsrf}");
        refreshRequest.Headers.Add("X-CSRF-TOKEN", refreshCsrf);
        var refreshAfterLogout = await loggedOutClient.SendAsync(refreshRequest);

        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MfaSetup_ShouldRequireAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/mfa/setup");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnsafeRequest_WithInvalidOrigin_ShouldBeForbidden()
    {
        using var client = _factory.CreateClient();
        var csrf = await GetCsrfTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com", password = "Wrong!Password#123" })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        request.Headers.Add("Cookie", $"cb_csrf={csrf}");
        request.Headers.TryAddWithoutValidation("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_ShouldBeRateLimited_AfterTooManyAttempts()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 12; i++)
        {
            var csrf = await GetCsrfTokenAsync(client);
            lastResponse = await PostJsonWithCsrfAsync(client, "/api/v1/auth/login", new
            {
                email = $"ratelimit_{i}@example.com",
                password = "Wrong!Password#123"
            }, csrf);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("csrfToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }

    private static async Task<HttpResponseMessage> PostJsonWithCsrfAsync(HttpClient client, string url, object payload, string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private static string? ExtractCookieHeaderValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            if (!value.StartsWith(cookieName + "=", StringComparison.Ordinal))
            {
                continue;
            }

            var firstSegment = value.Split(';', 2)[0];
            var idx = firstSegment.IndexOf('=');
            return idx >= 0 ? firstSegment[(idx + 1)..] : null;
        }

        return null;
    }
}
