using System.Text.Json.Serialization;

namespace TestHub.Models.Report;

/// <summary>
/// Response payload for <c>GET /v1/contractor/reports/financial</c>. Drives
/// the Reports page header tiles ("Total Revenue", "Outstanding") and the
/// monthly Revenue Trend bar chart.
/// </summary>
public sealed class FinancialReportDto
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("outstandingAmount")]
    public decimal OutstandingAmount { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("monthlyRevenue")]
    public List<MonthlyRevenue>? MonthlyRevenue { get; set; }
}
