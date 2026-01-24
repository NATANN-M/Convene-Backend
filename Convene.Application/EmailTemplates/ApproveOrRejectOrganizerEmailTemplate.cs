using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.EmailTemplates
{
    public class ApproveOrRejectOrganizerEmailTemplate
    {
        public static string ApprovetOrganizerEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Approved - Convene</title>
</head>
<body style=""margin:0;padding:20px;background-color:#f5f7fa;font-family:Arial,sans-serif;"">
    <div style=""max-width:600px;margin:0 auto;background:white;border-radius:8px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.1);"">
        <!-- Header -->
        <div style=""background:#4facfe;color:white;padding:30px 20px;text-align:center;"">
            <div style=""background:rgba(255,255,255,0.2);display:inline-block;padding:8px 16px;border-radius:20px;margin-bottom:20px;border:1px solid rgba(255,255,255,0.3);"">
                <span style=""color:white;font-weight:600;font-size:14px;"">? ACCOUNT APPROVED</span>
            </div>
            <h1 style=""color:white;font-size:24px;font-weight:700;margin:0 0 10px 0;"">Congratulations! ??</h1>
            <p style=""color:rgba(255,255,255,0.9);font-size:16px;margin:0;"">Welcome to the Convene Organizer Community</p>
        </div>
        
        <!-- Content -->
        <div style=""padding:30px 20px;"">
            <!-- Welcome Message -->
            <div style=""text-align:center;margin-bottom:30px;"">
                <div style=""font-size:48px;margin-bottom:20px;"">??</div>
                <h2 style=""color:#2d3748;font-size:22px;margin:0 0 15px 0;font-weight:700;"">Your Organizer Account is Live!</h2>
                <p style=""color:#4a5568;font-size:16px;line-height:1.6;max-width:500px;margin:0 auto 20px auto;"">
                    We're thrilled to inform you that your organizer account has been approved. 
                    You now have full access to create, manage, and promote events on Convene.
                </p>
            </div>
            
            <!-- Features (using tables for compatibility) -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:30px 0;"">
                <tr>
                    <td width=""33%"" valign=""top"" style=""padding:10px;"">
                        <div style=""background:#f8fafc;border-radius:8px;padding:20px;text-align:center;border:1px solid #e2e8f0;"">
                            <div style=""font-size:24px;margin-bottom:15px;"">??</div>
                            <h3 style=""color:#2d3748;font-size:16px;margin:0 0 10px 0;font-weight:600;"">Create Events</h3>
                            <p style=""color:#64748b;font-size:13px;line-height:1.5;margin:0;"">Design and publish events with our intuitive event creation tools</p>
                        </div>
                    </td>
                    <td width=""33%"" valign=""top"" style=""padding:10px;"">
                        <div style=""background:#f8fafc;border-radius:8px;padding:20px;text-align:center;border:1px solid #e2e8f0;"">
                            <div style=""font-size:24px;margin-bottom:15px;"">??</div>
                            <h3 style=""color:#2d3748;font-size:16px;margin:0 0 10px 0;font-weight:600;"">Manage Tickets</h3>
                            <p style=""color:#64748b;font-size:13px;line-height:1.5;margin:0;"">Handle ticket sales, attendee lists, and check-ins seamlessly</p>
                        </div>
                    </td>
                    <td width=""33%"" valign=""top"" style=""padding:10px;"">
                        <div style=""background:#f8fafc;border-radius:8px;padding:20px;text-align:center;border:1px solid #e2e8f0;"">
                            <div style=""font-size:24px;margin-bottom:15px;"">??</div>
                            <h3 style=""color:#2d3748;font-size:16px;margin:0 0 10px 0;font-weight:600;"">Advanced Analytics</h3>
                            <p style=""color:#64748b;font-size:13px;line-height:1.5;margin:0;"">Track your event performance with detailed insights</p>
                        </div>
                    </td>
                </tr>
            </table>
            
            <!-- Call to Action -->
            <div style=""background:#f8fafc;border-radius:8px;padding:30px 20px;text-align:center;margin:30px 0;border:2px dashed #cbd5e1;"">
                <h3 style=""color:#2d3748;font-size:18px;margin:0 0 15px 0;font-weight:700;"">Ready to Create Your First Event?</h3>
                <p style=""color:#4a5568;font-size:16px;margin:0 0 25px 0;line-height:1.5;"">
                    Start building your event now and reach thousands of potential attendees. 
                    It only takes a few minutes to set up!
                </p>
                <a href=""https://Convene.com/dashboard/events/create"" style=""display:inline-block;background:#4facfe;color:white;text-decoration:none;padding:16px 32px;border-radius:25px;font-weight:700;font-size:16px;"">Create Your First Event</a>
            </div>
        </div>
        
        <!-- Footer -->
        <div style=""background:#1a202c;color:white;padding:30px 20px;text-align:center;"">
            <h4 style=""font-size:16px;margin:0 0 15px 0;color:#4facfe;"">Need Help? We're Here For You</h4>
            <p style=""color:#a0aec0;line-height:1.6;margin:0 0 20px 0;font-size:14px;"">
                Our support team is ready to help you succeed. Whether you have questions 
                about features or need advice on event planning, we're just a message away.
            </p>
            
            <div style=""background:rgba(255,255,255,0.05);padding:20px;border-radius:8px;margin:20px 0;"">
                <p style=""color:white;margin:0 0 10px 0;"">?? Email: 
                    <a href=""mailto:support@Convene.com"" style=""color:#4facfe;text-decoration:none;font-weight:600;"">support@Convene.com</a>
                </p>
                <p style=""color:white;margin:0;"">?? Phone: 
                    <a href=""tel:+15551234567"" style=""color:#4facfe;text-decoration:none;font-weight:600;"">+1 (555) 123-4567</a>
                </p>
            </div>
            
            <p style=""font-style:italic;color:#a0aec0;margin:20px 0;"">""Empowering creators to build amazing experiences""</p>
            
            <div style=""color:#718096;font-size:12px;margin-top:20px;padding-top:20px;border-top:1px solid #2d3748;"">
                © 2024 Convene Inc. All rights reserved.<br>
                This is an automated message. Please do not reply to this email.
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public static string RejectOrganizerEmailTemplate(string adminNotes)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Status Update - Convene</title>
    <style>
        /* Reset and base styles */
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }}
        
        body {{
            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
            margin: 0;
            padding: 20px;
            min-height: 100vh;
        }}
        
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.08);
        }}
        
        /* Header */
        .header {{
            background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
            position: relative;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }}
        
        .header-icon {{
            font-size: 48px;
            margin-bottom: 20px;
            display: inline-block;
            opacity: 0.9;
        }}
        
        .status-badge {{
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(255, 255, 255, 0.15);
            backdrop-filter: blur(10px);
            padding: 10px 20px;
            border-radius: 25px;
            margin-bottom: 20px;
            border: 1px solid rgba(255, 255, 255, 0.2);
        }}
        
        .status-badge span {{
            color: white;
            font-weight: 600;
            font-size: 14px;
            letter-spacing: 0.5px;
        }}
        
        .header h1 {{
            font-size: 28px;
            font-weight: 600;
            margin-bottom: 10px;
            color: white;
        }}
        
        .header p {{
            color: rgba(255, 255, 255, 0.85);
            font-size: 16px;
            line-height: 1.5;
            max-width: 500px;
            margin: 0 auto;
        }}
        
        /* Content */
        .content {{
            padding: 40px 30px;
        }}
        
        .main-message {{
            text-align: center;
            margin-bottom: 40px;
            padding-bottom: 30px;
            border-bottom: 1px solid #e5e7eb;
        }}
        
        .main-message h2 {{
            color: #374151;
            font-size: 24px;
            margin-bottom: 20px;
            font-weight: 600;
        }}
        
        .main-message p {{
            color: #6b7280;
            font-size: 16px;
            line-height: 1.6;
            max-width: 500px;
            margin: 0 auto 15px;
        }}
        
        /* Decision Section */
        .decision-section {{
            background: #f9fafb;
            border-radius: 12px;
            padding: 30px;
            margin: 30px 0;
            border: 1px solid #e5e7eb;
        }}
        
        .decision-header {{
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 20px;
        }}
        
        .decision-icon {{
            font-size: 24px;
            color: #ef4444;
        }}
        
        .decision-header h3 {{
            color: #374151;
            font-size: 18px;
            font-weight: 600;
        }}
        
        .reason-box {{
            background: white;
            border-radius: 8px;
            padding: 20px;
            border-left: 4px solid #ef4444;
            margin: 20px 0;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
        }}
        
        .reason-box h4 {{
            color: #374151;
            font-size: 14px;
            font-weight: 600;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        
        .reason-content {{
            color: #6b7280;
            font-size: 15px;
            line-height: 1.6;
            padding: 10px;
            background: #fef2f2;
            border-radius: 6px;
            font-style: italic;
        }}
        
        /* Improvement Suggestions */
        .suggestions-section {{
            background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
            border-radius: 12px;
            padding: 30px;
            margin: 40px 0;
            border: 1px solid #bae6fd;
        }}
        
        .suggestions-section h3 {{
            color: #0369a1;
            font-size: 18px;
            margin-bottom: 20px;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 10px;
        }}
        
        .suggestion-list {{
            list-style: none;
        }}
        
        .suggestion-list li {{
            display: flex;
            align-items: flex-start;
            gap: 12px;
            margin-bottom: 15px;
            padding-bottom: 15px;
            border-bottom: 1px solid rgba(186, 230, 253, 0.5);
        }}
        
        .suggestion-list li:last-child {{
            margin-bottom: 0;
            padding-bottom: 0;
            border-bottom: none;
        }}
        
        .suggestion-icon {{
            color: #0ea5e9;
            font-size: 18px;
            flex-shrink: 0;
            margin-top: 2px;
        }}
        
        .suggestion-text {{
            color: #475569;
            font-size: 14px;
            line-height: 1.5;
        }}
        
        /* Next Steps */
        .next-steps {{
            background: white;
            border-radius: 12px;
            padding: 30px;
            border: 1px solid #e5e7eb;
            margin: 30px 0;
        }}
        
        .next-steps h3 {{
            color: #374151;
            font-size: 18px;
            margin-bottom: 20px;
            font-weight: 600;
            text-align: center;
        }}
        
        .steps-container {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }}
        
        .step-card {{
            background: #f8fafc;
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            transition: all 0.3s ease;
            border: 1px solid #e2e8f0;
        }}
        
        .step-card:hover {{
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
        }}
        
        .step-icon {{
            font-size: 24px;
            margin-bottom: 15px;
            color: #6b7280;
        }}
        
        .step-card h4 {{
            color: #374151;
            font-size: 15px;
            margin-bottom: 10px;
            font-weight: 600;
        }}
        
        .step-card p {{
            color: #6b7280;
            font-size: 13px;
            line-height: 1.5;
        }}
        
        /* Contact Info */
        .contact-section {{
            text-align: center;
            padding: 30px;
            background: #f8fafc;
            border-radius: 12px;
            margin: 30px 0;
        }}
        
        .contact-section h3 {{
            color: #374151;
            font-size: 18px;
            margin-bottom: 15px;
            font-weight: 600;
        }}
        
        .contact-section p {{
            color: #6b7280;
            font-size: 14px;
            line-height: 1.6;
            margin-bottom: 20px;
            max-width: 500px;
            margin-left: auto;
            margin-right: auto;
        }}
        
        .contact-links {{
            display: flex;
            justify-content: center;
            gap: 20px;
            flex-wrap: wrap;
            margin-top: 20px;
        }}
        
        .contact-link {{
            color: #3b82f6;
            text-decoration: none;
            font-weight: 500;
            padding: 8px 16px;
            border: 1px solid #dbeafe;
            border-radius: 6px;
            background: white;
            font-size: 14px;
            transition: all 0.3s ease;
        }}
        
        .contact-link:hover {{
            background: #3b82f6;
            color: white;
            border-color: #3b82f6;
        }}
        
        /* Footer */
        .footer {{
            background: #1f2937;
            color: white;
            padding: 40px 30px;
            text-align: center;
        }}
        
        .footer-content {{
            max-width: 500px;
            margin: 0 auto;
        }}
        
        .footer h4 {{
            font-size: 16px;
            margin-bottom: 15px;
            color: #d1d5db;
            font-weight: 500;
        }}
        
        .footer p {{
            color: #9ca3af;
            line-height: 1.6;
            margin-bottom: 20px;
            font-size: 14px;
        }}
        
        .encouragement {{
            font-style: italic;
            color: #d1d5db;
            margin: 25px 0;
            padding: 20px;
            background: rgba(255, 255, 255, 0.05);
            border-radius: 8px;
            border-left: 3px solid #6b7280;
        }}
        
        .copyright {{
            color: #6b7280;
            font-size: 12px;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #374151;
        }}
        
        /* Responsive */
        @media (max-width: 480px) {{
            .header {{
                padding: 30px 20px;
            }}
            
            .content {{
                padding: 30px 20px;
            }}
            
            .decision-section {{
                padding: 20px;
            }}
            
            .suggestions-section {{
                padding: 20px;
            }}
            
            .steps-container {{
                grid-template-columns: 1fr;
            }}
            
            .contact-links {{
                flex-direction: column;
                align-items: center;
            }}
            
            .contact-link {{
                width: 100%;
                max-width: 250px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <!-- Header -->
        <div class=""header"">
            <div class=""header-icon"">??</div>
            <div class=""status-badge"">
                <span>?? ACCOUNT STATUS UPDATE</span>
            </div>
            <h1>Organizer Application Review</h1>
            <p>Convene - Creating Memorable Experiences</p>
        </div>
        
        <!-- Content -->
        <div class=""content"">
            <!-- Main Message -->
            <div class=""main-message"">
                <h2>Application Status: Not Approved</h2>
                <p>
                    Thank you for your interest in becoming an organizer on Convene. 
                    We've carefully reviewed your application, but unfortunately, we're unable 
                    to approve it at this time.
                </p>
                <p>
                    We appreciate the time and effort you put into your application.
                </p>
            </div>
            
            <!-- Decision Details -->
            <div class=""decision-section"">
                <div class=""decision-header"">
                    <div class=""decision-icon"">?</div>
                    <h3>Application Decision Details</h3>
                </div>
                
                <p style=""color: #6b7280; margin-bottom: 20px; line-height: 1.6;"">
                    Our review team has evaluated your application against our organizer criteria. 
                    While we value your interest, we have some concerns that need to be addressed.
                </p>
                
                <div class=""reason-box"">
                    <h4>Primary Reason for Rejection</h4>
                    <div class=""reason-content"">
                        {adminNotes}
                    </div>
                </div>
                
                <p style=""color: #6b7280; font-size: 14px; line-height: 1.6; margin-top: 20px;"">
                    This decision is based on our current platform requirements and quality standards. 
                    We encourage you to address these concerns and consider reapplying in the future.
                </p>
            </div>
            
            <!-- Improvement Suggestions -->
            <div class=""suggestions-section"">
                <h3>?? Suggestions for Improvement</h3>
                <p style=""color: #475569; margin-bottom: 20px; font-size: 14px;"">
                    Based on our review, here are some areas you might consider improving:
                </p>
                
                <ul class=""suggestion-list"">
                    <li>
                        <div class=""suggestion-icon"">?</div>
                        <div class=""suggestion-text"">
                            <strong>Complete Profile Information:</strong> Ensure all required fields are filled with detailed, accurate information
                        </div>
                    </li>
                    <li>
                        <div class=""suggestion-icon"">?</div>
                        <div class=""suggestion-text"">
                            <strong>Professional Documentation:</strong> Provide clear, professional documentation and verification materials
                        </div>
                    </li>
                    <li>
                        <div class=""suggestion-icon"">?</div>
                        <div class=""suggestion-text"">
                            <strong>Event Quality Standards:</strong> Review our event quality guidelines and ensure your proposed events meet these standards
                        </div>
                    </li>
                    <li>
                        <div class=""suggestion-icon"">?</div>
                        <div class=""suggestion-text"">
                            <strong>Platform Guidelines:</strong> Familiarize yourself with our terms of service and community guidelines
                        </div>
                    </li>
                </ul>
            </div>
        </div>
        
        <!-- Footer -->
        <div class=""footer"">
            <div class=""footer-content"">
                <h4>Convene Platform</h4>
                <p>
                    We're committed to maintaining a high-quality platform for both 
                    organizers and attendees. Thank you for your understanding.
                </p>
                
                <div class=""encouragement"">
                    ""Every great event starts with thorough preparation. We encourage you to 
                    address the feedback and consider reapplying when you're ready.""
                </div>
                
                <div style=""color: #9ca3af; font-size: 13px; margin: 20px 0; padding: 15px; background: rgba(255, 255, 255, 0.05); border-radius: 6px;"">
                    <p style=""margin-bottom: 10px;""><strong>Support Hours:</strong> Mon-Fri, 9 AM - 6 PM</p>
                    <p><strong>Response Time:</strong> 24-48 hours for email inquiries</p>
                </div>
                
                <div class=""copyright"">
                    © 2024 Convene Inc. All rights reserved.<br>
                    This is an automated message regarding your application status.<br>
                    Please do not reply directly to this email.
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
