
   namespace Convene.Application.EmailTemplates
               {
                 public class PaymentReminderAndFailedToPayEmailTemplete
                    {

                       public static string GetPaymentReminderHtml(
                    string payerName,
                    string eventTitle,
                    string paymentLink)
                        {
                            return $@"<!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                            background: #f8f9fa;
                        }}
                        .email-container {{
                            background: white;
                            border-radius: 12px;
                            padding: 40px 30px;
                            box-shadow: 0 5px 20px rgba(0,0,0,0.08);
                            border: 1px solid #e9ecef;
                        }}
                        h3 {{
                            color: #2c3e50;
                            font-size: 24px;
                            margin-bottom: 20px;
                            font-weight: 600;
                        }}
                        p {{
                            color: #495057;
                            font-size: 16px;
                            line-height: 1.7;
                            margin-bottom: 20px;
                        }}
                        strong {{
                            color: #3498db;
                        }}
                        .payment-button {{
                            display: inline-block;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white !important;
                            padding: 14px 28px;
                            border-radius: 8px;
                            text-decoration: none;
                            font-weight: 600;
                            font-size: 16px;
                            margin: 25px 0;
                            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
                            transition: all 0.3s ease;
                            border: none;
                        }}
                        .payment-button:hover {{
                            transform: translateY(-2px);
                            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
                        }}
                        .signature {{
                            color: #6c757d;
                            margin-top: 30px;
                            padding-top: 20px;
                            border-top: 1px solid #e9ecef;
                        }}
                        .footer-note {{
                            color: #95a5a6;
                            font-size: 14px;
                            margin-top: 30px;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""email-container"">
                        <h3>Hi {payerName},</h3>
                        <p>This is a friendly reminder that your payment for the event <strong>{eventTitle}</strong> is still pending.</p>
                        <p>Please complete your payment soon to confirm your booking.</p>
                        <a href='{paymentLink}' class='payment-button'>Complete Payment</a>
                        <p class=""signature"">Thank you,<br/>Convene Team</p>
                        <div class=""footer-note"">
                            This is an automated payment reminder.
                        </div>
                    </div>
                </body>
                </html>";
                        }
                    }
                }
