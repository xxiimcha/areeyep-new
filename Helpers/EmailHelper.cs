using System;
using System.Net.Mail;

namespace AreEyeP.Helpers
{
    public class EmailHelper
    {
        public static void SendEmail(string toEmail, string subject, string body)
        {
            // Gmail SMTP settings
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587, // SMTP port for Gmail
                Credentials = new System.Net.NetworkCredential("areeyep05@gmail.com", "emretzzeacqyspwx"), // Replace with your Gmail and App Password
                EnableSsl = true, // Enable SSL for secure communication
            };

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
    }
}
