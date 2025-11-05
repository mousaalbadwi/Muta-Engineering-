using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class SmtpOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = "Mutah Engineering Support";
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    public SmtpEmailSender(IOptions<SmtpOptions> opt) => _opt = opt.Value;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.EnableSsl,
            Credentials = new NetworkCredential(_opt.Username, _opt.Password)
        };

        using var msg = new MailMessage
        {
            From = new MailAddress(_opt.FromEmail, _opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(new MailAddress(to));

        // System.Net.Mail ما فيه Async حقيقي، فبنلفّه بتاسك
        await Task.Run(() => client.Send(msg));
    }
}
