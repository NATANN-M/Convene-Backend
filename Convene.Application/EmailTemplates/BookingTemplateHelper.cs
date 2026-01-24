using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.Helpers.EmailTemplates
{
    public static class BookingTemplateHelper
    {
        public static string GetBookingConfirmationHtml(
            string userName,
            string eventName,
            DateTime eventDate,
            string eventLocation,
            decimal totalAmount,
            string bookingLink
           ) 
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Booking Confirmation - Convene</title>
    <style>
        /* Reset and base styles */
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }}
        
        body {{
            background-color: #f5f7fa;
            margin: 0;
            padding: 20px;
        }}
        
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
        }}
        
        /* Header */
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
            position: relative;
        }}
        
        .header h1 {{
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 10px;
            letter-spacing: 0.5px;
        }}
        
        .header p {{
            font-size: 16px;
            opacity: 0.9;
        }}
        
        .confirmation-badge {{
            position: absolute;
            top: 20px;
            right: 20px;
            background: #10b981;
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .confirmation-badge::before {{
            content: ""?"";
            font-weight: bold;
        }}
        
        /* Content */
        .content {{
            padding: 40px 30px;
        }}
        
        .greeting {{
            color: #2d3748;
            font-size: 20px;
            margin-bottom: 25px;
            font-weight: 600;
        }}
        
        .greeting span {{
            color: #667eea;
        }}
        
        .booking-details {{
            background: #f8fafc;
            border-radius: 12px;
            padding: 30px;
            margin: 25px 0;
            border-left: 4px solid #667eea;
        }}
        
        .detail-item {{
            display: flex;
            justify-content: space-between;
            padding: 12px 0;
            border-bottom: 1px solid #e2e8f0;
        }}
        
        .detail-item:last-child {{
            border-bottom: none;
        }}
        
        .detail-label {{
            color: #64748b;
            font-weight: 500;
        }}
        
        .detail-value {{
            color: #1e293b;
            font-weight: 600;
            text-align: right;
        }}
        
        .highlight-box {{
            background: linear-gradient(135deg, #f0f4ff 0%, #f5f3ff 100%);
            border-radius: 12px;
            padding: 25px;
            margin: 30px 0;
            text-align: center;
            border: 1px solid #e0e7ff;
        }}
        
        .highlight-box h3 {{
            color: #667eea;
            margin-bottom: 15px;
            font-size: 18px;
        }}
        
        .ticket-info {{
            display: inline-flex;
            align-items: center;
            gap: 10px;
            background: white;
            padding: 15px 25px;
            border-radius: 10px;
            margin-top: 10px;
            box-shadow: 0 4px 6px rgba(79, 70, 229, 0.1);
        }}
        
        .ticket-info i {{
            color: #667eea;
            font-size: 20px;
        }}
        
        /* Action Button */
        .action-button {{
            display: block;
            width: 100%;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            padding: 18px;
            text-align: center;
            border-radius: 10px;
            font-weight: 600;
            font-size: 16px;
            margin: 30px 0;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
        }}
        
        .action-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
        }}
        
        /* Footer */
        .footer {{
            background: #f8fafc;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e2e8f0;
        }}
        
        .footer p {{
            color: #64748b;
            line-height: 1.6;
            margin-bottom: 15px;
        }}
        
        .social-links {{
            display: flex;
            justify-content: center;
            gap: 20px;
            margin: 25px 0;
        }}
        
        .social-icon {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 40px;
            height: 40px;
            background: #e2e8f0;
            border-radius: 50%;
            color: #64748b;
            text-decoration: none;
            transition: all 0.3s ease;
        }}
        
        .social-icon:hover {{
            background: #667eea;
            color: white;
            transform: translateY(-3px);
        }}
        
        .contact-info {{
            font-size: 14px;
            color: #94a3b8;
            margin-top: 20px;
        }}
        
        .company-name {{
            color: #667eea;
            font-weight: 700;
            font-size: 18px;
            margin-bottom: 5px;
        }}
        
        /* Responsive */
        @media (max-width: 480px) {{
            .header {{
                padding: 30px 20px;
            }}
            
            .content {{
                padding: 30px 20px;
            }}
            
            .booking-details {{
                padding: 20px;
            }}
            
            .detail-item {{
                flex-direction: column;
                gap: 5px;
            }}
            
            .detail-value {{
                text-align: left;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <!-- Header Section -->
        <div class=""header"">
            <div class=""confirmation-badge"">Confirmed</div>
            <h1>?? Booking Confirmed!</h1>
            <p>Your event experience awaits</p>
        </div>
        
        <!-- Content Section -->
        <div class=""content"">
            <h2 class=""greeting"">Hello <span>{userName}</span>,</h2>
            
            <p style=""color: #475569; line-height: 1.6; margin-bottom: 25px;"">
                We're excited to confirm your booking for <strong>{eventName}</strong>! 
                Get ready for an unforgettable experience.
            </p>
            
            <!-- Booking Details Card -->
            <div class=""booking-details"">
                <div class=""detail-item"">
                    <span class=""detail-label"">Event Name</span>
                    <span class=""detail-value"">{eventName}</span>
                </div>
                <div class=""detail-item"">
                    <span class=""detail-label"">Date & Time</span>
                    <span class=""detail-value"">{eventDate:dddd, MMMM dd, yyyy • hh:mm tt}</span>
                </div>
                <div class=""detail-item"">
                    <span class=""detail-label"">Location</span>
                    <span class=""detail-value"">{eventLocation}</span>
                </div>
               
                <div class=""detail-item"">
                    <span class=""detail-label"">Total Amount</span>
                    <span class=""detail-value"" style=""color: #10b981; font-size: 18px;"">
                        {totalAmount:C}
                    </span>
                </div>
            </div>
            
            <!-- Important Information -->
            <div class=""highlight-box"">
                <h3>?? Save the Date</h3>
                <p style=""color: #475569; margin-bottom: 15px;"">
                    Your tickets are now available. Make sure to arrive 30 minutes before the event starts.
                </p>
                <div class=""ticket-info"">
                    <span>??</span>
                    <span>e-Ticket • Mobile Friendly</span>
                </div>
            </div>
            
            <!-- Action Button -->
            <a href=""{bookingLink}"" class=""action-button"">
                ?? View My Tickets & Event Details
            </a>
            
            <!-- Additional Information -->
            <div style=""background: #f0f9ff; padding: 20px; border-radius: 10px; margin: 25px 0;"">
                <h4 style=""color: #0369a1; margin-bottom: 10px;"">?? Need Help?</h4>
                <p style=""color: #475569; font-size: 14px; line-height: 1.5;"">
                    • Questions about the event? Contact the organizer<br>
                    • Can't find your tickets? Check your account dashboard<br>
                    • Need to cancel or make changes? Contact support
                </p>
            </div>
        </div>
        
        <!-- Footer Section -->
        <div class=""footer"">
            <div class=""company-name"">Convene</div>
            <p style=""color: #64748b; line-height: 1.6;"">
                Thank you for choosing Convene. We look forward to seeing you at the event!
            </p>
            
            <div style=""margin: 25px 0;"">
                <p style=""color: #475569; font-style: italic;"">
                    ""Creating memorable experiences, one event at a time.""
                </p>
            </div>
            
            <!-- Social Links -->
            <div class=""social-links"">
                <a href=""#"" class=""social-icon"">??</a>
                <a href=""#"" class=""social-icon"">??</a>
                <a href=""#"" class=""social-icon"">??</a>
                <a href=""#"" class=""social-icon"">??</a>
            </div>
            
            <div class=""contact-info"">
                <p>Convene Inc. • 123 Event Street, City, Country</p>
                <p>support@Convene.com • +1 (555) 123-4567</p>
                <p style=""margin-top: 20px; font-size: 12px; color: #94a3b8;"">
                    This is an automated message. Please do not reply to this email.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
