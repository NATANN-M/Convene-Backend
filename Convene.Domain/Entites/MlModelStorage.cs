using System;

namespace Convene.Domain.Entities
{
    public class MlModelStorage
    {
        public int Id { get; set; } = 1;       // Always 1
        public byte[] ModelBinary { get; set; } = null!;
        public string Version { get; set; } = "v1";
        public DateTime LastTrained { get; set; } = DateTime.UtcNow;
    }
}
