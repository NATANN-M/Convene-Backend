using System;
using System.Collections.Generic;

namespace Convene.Application.DTOs.Recommendation
{
    public class AdminMetricsDto
    {
        public int TotalInteractions { get; set; }
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }

        public Dictionary<string, int> TopCategories { get; set; } = new();
        public Dictionary<string, int> TopEvents { get; set; } = new();
        public Dictionary<string, int> ProximityDistribution { get; set; } = new();

        public float MLAccuracy { get; set; }
        public float MLRmse { get; set; }
        public string? ModelVersion { get; set; }
        public DateTime? LastTrained { get; set; }

        public float ColdStartPercentage { get; set; } // optional if you compute it
    }
}
