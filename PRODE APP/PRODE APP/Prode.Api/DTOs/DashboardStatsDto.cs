namespace Prode.Api.DTOs;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }

    public int TotalMatches { get; set; }

    public int FinishedMatches { get; set; }

    public int TotalPredictions { get; set; }

    public double AveragePoints { get; set; }
}