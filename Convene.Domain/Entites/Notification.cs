using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;

namespace Convene.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }         // who receives it
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public new DateTime CreatedAt { get; set; } =DateTime.UtcNow;
        public NotificationType Type { get; set; }

        public string? ReferenceKey { get; set; }

        public User User { get; set; } = null!;
    }
}
