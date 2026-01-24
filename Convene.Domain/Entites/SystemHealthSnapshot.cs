
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convene.Infrastructure.Entities
{
   
    public class SystemHealthSnapshot
    {
        
        public Guid Id { get; set; } = Guid.NewGuid();

        
        public string JsonData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
