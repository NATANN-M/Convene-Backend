using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Domain.Enums
{
    public enum NotificationType
    {
        General,
        BookingConfirmed,
        PaymentReminder,
        EventCancelled,
        EventStartingSoon,
        FeedbackReply,
        BookingCancelled,

        EventReminderOneDay,
        EventReminderTwoHours,
        EventStartingNow,


        //admin notifications


        AdminBrodcast,
        AdminDirectMessage,



            OrganizerMessage
    }
}

