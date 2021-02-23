using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailStateForwarder.Model;
using Microsoft.AspNetCore.Mvc;

namespace MailStateForwarder.Controllers
{
	// mail state forwarder

	[ApiController]
	[Route("forward")]
	public class ForwardController : ControllerBase
	{
		[HttpPost]
		public async Task Filter([FromBody] IDictionary<string, string> data)
		{
			Console.WriteLine($"Data:     {string.Join(",", data.Select(x => $"{x.Key}={x.Value}"))}");

			Configuration config = Configuration.Instance;
			(string subject, string content) = await Gmail.GetLatestMessageAsync(config.Gmail.UserId, config.Gmail.Condition.Sender);

			Console.WriteLine($"Subject:  {subject}");
			Console.WriteLine($"Content:  {content}");

			bool messageMatch = Regex.IsMatch(subject, config.Gmail.Condition.Subject)
				&& Regex.IsMatch(content, config.Gmail.Condition.Content);

			Console.WriteLine($"Match:    {messageMatch}");

			if (!messageMatch)
				return;

			string forwardUrl = data.Aggregate(
				config.Forward.Url,
				(url, x) => url.Replace($"%{x.Key}%", x.Value));

			Console.WriteLine($"Forward:  {forwardUrl}");

			MakeHttpRequest(forwardUrl);
		}

		private static void MakeHttpRequest(string forwardUrl)
		{
			Task.Run(async () =>
				{
					try
					{
						Configuration config = Configuration.Instance;

						using (HttpClientHandler clientHandler = new HttpClientHandler())
						{
							clientHandler.ServerCertificateCustomValidationCallback += (message, cert, chain, errors) =>
								errors == SslPolicyErrors.None || cert.GetCertHashString() == config.Forward.ValidCertificateThumbprint;

							using (HttpClient client = new HttpClient(clientHandler))
							{
								string credentials = $"{config.Forward.User}:{config.Forward.Password}";
								string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
								client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
								HttpResponseMessage response = await client.GetAsync(forwardUrl);

								Console.WriteLine($"Response: {(int)response.StatusCode} {response.StatusCode}");
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error:    {ex}");
					}
				});
		}
	}
}
