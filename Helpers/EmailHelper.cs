using System;
using System.Net.Mail;

namespace AreEyeP.Helpers
{
    public class EmailHelper
    {
        public static void SendEmailConfirmation(string toEmail, string userName)
        {
            // Gmail SMTP settings
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587, // SMTP port for Gmail
                Credentials = new System.Net.NetworkCredential("areeyep05@gmail.com", "emretzzeacqyspwx"), // Replace with your Gmail and App Password
                EnableSsl = true, // Enable SSL for secure communication
            };

            string subject = "Welcome to AreEyeP!";
            string body = GenerateEmailBody(userName);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("areeyep05@gmail.com", "AreEyeP Team"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true, // Indicates that the body is HTML
            };

            mailMessage.To.Add(toEmail);

            try
            {
                smtpClient.Send(mailMessage);
                Console.WriteLine($"Email successfully sent to {toEmail}");
            }
            catch (Exception ex)
            {
                // Log the exception or handle errors
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        private static string GenerateEmailBody(string userName)
        {
            // Generate an HTML email layout
            return $@"
                <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px;'>
                            <div style='text-align: center;'>
                                <img src='https://via.placeholder.com/150' alt='AreEyeP Logo' style='max-width: 150px; margin-bottom: 20px;' />
                            </div>
                            <h1 style='color: #2c3e50; text-align: center;'>Welcome, {userName}!</h1>
                            <p style='font-size: 16px;'>
                                Thank you for registering with <strong>AreEyeP</strong>. Your account has been successfully created.
                            </p>
                            <p style='font-size: 16px;'>
                                If you have any questions, feel free to reach out to us at <a href='mailto:support@areeyep.com' style='color: #3498db;'>support@areeyep.com</a>.
                            </p>
                            <hr style='border-top: 1px solid #ddd;' />
                            <p style='font-size: 14px; text-align: center;'>
                                Best regards,<br />
                                <strong>The AreEyeP Team</strong>
                            </p>
                            <p style='font-size: 12px; text-align: center; color: #999;'>
                                This is an automated email. Please do not reply.
                            </p>
                        </div>
                    </body>
                </html>
            ";
        }
    }
}
