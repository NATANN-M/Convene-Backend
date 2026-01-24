public class RecommendationMetrics
{
    public int Id { get; set; }
    public int TotalInteractions { get; set; }
    public float MLRmse { get; set; }            // Model RMSE
    public float MLAccuracy { get; set; }        // R²
    public float ColdStartPercentage { get; set; }

    public string ModelVersion { get; set; } = null!;
    public DateTime LastTrained { get; set; }
    public DateTime LastUpdated { get; set; }

    // New visualization-friendly metrics
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public string TopCategoriesJson { get; set; } = "{}"; // JSON string of category counts
    public string TopEventsJson { get; set; } = "{}";     // JSON string of top event interactions
    public string ProximityDistributionJson { get; set; } = "{}"; // distance-based distribution
}
