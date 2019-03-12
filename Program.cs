using System;
using System.Security.Cryptography;
using System.Text;

namespace DownloadTicket
{

  class Program
  {
    static readonly string SignatureSigningSecret = "some-shared-secret";

    static void Main(string[] args)
    {
      // Validate an example URL with download ticket.
      var exampleUrl = "https://http-m.nota.dk/Download/10140345/25000?ticket=OAMPFWZraKpVNksEN4ryoXXOY47tZTV7pwwcNU0twTE&validUntil=1554967962181";

      if (ResourceURLHasValidDownloadTicket(exampleUrl))
      {
        Console.WriteLine("Ticket is valid, redicecting to download...");
      }
      else
      {
        Console.WriteLine("Ticket is invalid, please go away!");
      }

      // Generate new download ticket.
      var newResourceUrl = "https://http-m.nota.dk/Download/10140346/37027";
      var validUntil = DateTimeOffset.Now.AddDays(30).ToUnixTimeMilliseconds();
      var downloadTicket = GetDownloadTicket(newResourceUrl, validUntil);

      Console.WriteLine("");
      Console.WriteLine($"Generating ticket for resource URL {newResourceUrl}");
      Console.WriteLine($"Ticket URL: {newResourceUrl}?ticket={downloadTicket}&validUntil={validUntil}");
    }

    private static Boolean ResourceURLHasValidDownloadTicket(string ticketUrl)
    {
      try
      {
        var ticketUri = new Uri(ticketUrl);
        var resourceUrl = ticketUrl.Substring(0, ticketUrl.IndexOf(ticketUri.Query, StringComparison.Ordinal));
        var query = System.Web.HttpUtility.ParseQueryString(ticketUri.Query);
        long validUntil = long.Parse(query.Get("validUntil"));
        string ticket = query.Get("ticket");

        if (validUntil < DateTimeOffset.Now.ToUnixTimeMilliseconds())
        {
          return false;
        }

        var validTicket = GetDownloadTicket(resourceUrl, validUntil);

        Console.WriteLine($"Provided Ticket: {ticket}");
        Console.WriteLine($"Valid Ticket: {validTicket}");

        return ticket == validTicket;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Not a valid resource URL with download ticket: {ex.Message}");
        return false;
      }
    }

    private static string GetDownloadTicket(string resourceUrl, long validUntil)
    {
      return GetHMACSignature(resourceUrl + validUntil.ToString());
    }

    private static string GetHMACSignature(string message)
    {
      var encoding = new ASCIIEncoding();
      byte[] messageBytes = encoding.GetBytes(message);
      byte[] keyBytes = encoding.GetBytes(SignatureSigningSecret);
      using (var hmacsha256 = new HMACSHA256(keyBytes))
      {
        byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        return GetUrlSafeBase64String(hashmessage);
      }
    }

    private static string GetUrlSafeBase64String(byte[] input)
    {
      return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
  }
}
