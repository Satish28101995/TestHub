using System.Text.Json.Serialization;

namespace TestHub.Models.Contractor;

/// <summary>
/// Response payload for <c>GET /v1/contractor/dashboard/stats</c>.
/// Drives the four dashboard summary tiles (Projects, Contracts, Revenue, Pending).
/// </summary>
public sealed class DashboardStatsDto
{
    [JsonPropertyName("totalProjects")]
    public int TotalProjects { get; set; }

    [JsonPropertyName("totalContracts")]
    public int TotalContracts { get; set; }

    [JsonPropertyName("totalContractValue")]
    public decimal TotalContractValue { get; set; }

    [JsonPropertyName("projectsThisWeek")]
    public int ProjectsThisWeek { get; set; }

    [JsonPropertyName("projectsLastWeek")]
    public int ProjectsLastWeek { get; set; }

    [JsonPropertyName("projectsWeeklyDelta")]
    public int ProjectsWeeklyDelta { get; set; }

    [JsonPropertyName("projectsWeeklyDeltaDisplay")]
    public string? ProjectsWeeklyDeltaDisplay { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("revenueThisMonth")]
    public decimal RevenueThisMonth { get; set; }

    [JsonPropertyName("revenueLastMonth")]
    public decimal RevenueLastMonth { get; set; }

    [JsonPropertyName("revenueMtdGrowthPercent")]
    public decimal RevenueMtdGrowthPercent { get; set; }

    [JsonPropertyName("revenueMtdGrowthDisplay")]
    public string? RevenueMtdGrowthDisplay { get; set; }

    [JsonPropertyName("pendingAmount")]
    public decimal PendingAmount { get; set; }

    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }
}
