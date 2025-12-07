using System.Net;
using System.Net.Http.Json;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Models;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class AnomalyEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AnomalyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up any test data
        await _client.DeleteAsync("/v1/screening/flood-anomalies");
    }

    [Fact]
    public async Task PostAnomaly_WithValidReport_ReturnsAccepted()
    {
        var report = new FloodAnomalyReport
        {
            Address = "123 Test Street, Brisbane",
            OverallRisk = "High",
            Source = "BCC Parcel Metrics",
            MetricsMissing = true,
            Notes = "Test anomaly report"
        };

        var response = await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", report);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostAnomaly_WithEmptyAddress_ReturnsValidationProblem()
    {
        var report = new FloodAnomalyReport
        {
            Address = "",
            OverallRisk = "High"
        };

        var response = await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", report);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnomalies_AfterPost_ReturnsPostedItem()
    {
        var report = new FloodAnomalyReport
        {
            Address = "456 Test Avenue, Brisbane",
            OverallRisk = "Medium",
            DisagreesWithFloodWise = true
        };

        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", report);

        var response = await _client.GetAsync("/v1/screening/flood-anomalies");
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<FloodAnomalyReport>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Address == "456 Test Avenue, Brisbane");
    }

    [Fact]
    public async Task DeleteAnomaly_WithValidId_ReturnsNoContent()
    {
        // Create an anomaly first
        var report = new FloodAnomalyReport
        {
            Address = "789 Delete Test, Brisbane",
            OverallRisk = "Low"
        };
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", report);

        // Get all to find the ID
        var getResponse = await _client.GetAsync("/v1/screening/flood-anomalies");
        var items = await getResponse.Content.ReadFromJsonAsync<List<FloodAnomalyReport>>();
        var created = items?.FirstOrDefault(i => i.Address == "789 Delete Test, Brisbane");
        Assert.NotNull(created);

        // Delete it
        var deleteResponse = await _client.DeleteAsync($"/v1/screening/flood-anomalies/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's gone
        var verifyResponse = await _client.GetAsync("/v1/screening/flood-anomalies");
        var remaining = await verifyResponse.Content.ReadFromJsonAsync<List<FloodAnomalyReport>>();
        Assert.DoesNotContain(remaining!, i => i.Id == created.Id);
    }

    [Fact]
    public async Task DeleteAnomaly_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/v1/screening/flood-anomalies/nonexistent-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ClearAllAnomalies_RemovesAllItems()
    {
        // Create some anomalies
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", new FloodAnomalyReport { Address = "Clear Test 1" });
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", new FloodAnomalyReport { Address = "Clear Test 2" });

        // Clear all
        var clearResponse = await _client.DeleteAsync("/v1/screening/flood-anomalies");
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        // Verify empty
        var getResponse = await _client.GetAsync("/v1/screening/flood-anomalies");
        var items = await getResponse.Content.ReadFromJsonAsync<List<FloodAnomalyReport>>();
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetAnomalies_ReturnsOrderedByCreatedUtcDescending()
    {
        // Clear first
        await _client.DeleteAsync("/v1/screening/flood-anomalies");

        // Create in order
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", new FloodAnomalyReport { Address = "First" });
        await Task.Delay(10); // Small delay to ensure different timestamps
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", new FloodAnomalyReport { Address = "Second" });
        await Task.Delay(10);
        await _client.PostAsJsonAsync("/v1/screening/flood-anomalies", new FloodAnomalyReport { Address = "Third" });

        var response = await _client.GetAsync("/v1/screening/flood-anomalies");
        var items = await response.Content.ReadFromJsonAsync<List<FloodAnomalyReport>>();

        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        // Most recent first
        Assert.Equal("Third", items[0].Address);
        Assert.Equal("Second", items[1].Address);
        Assert.Equal("First", items[2].Address);
    }
}