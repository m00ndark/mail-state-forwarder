using System;
using System.IO;
using Newtonsoft.Json;

namespace MailStateForwarder.Model
{
	public class Configuration
	{
		public static Configuration Instance { get; private set; }

		public GmailConfiguration Gmail { get; set; }
		public ForwardConfiguration Forward { get; set; }

		public static void Load()
		{
			string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");

			if (!File.Exists(configFilePath))
				throw new Exception($"Config file missing: {configFilePath}");

			Instance = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configFilePath));
		}
	}

	public class GmailConfiguration
	{
		public string UserId { get; set; }
		public MailCondition Condition { get; set; }
	}

	public class MailCondition
	{
		public string Sender { get; set; }
		public string Subject { get; set; }
		public string Content { get; set; }
	}

	public class ForwardConfiguration
	{
		public string Url { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public string ValidCertificateThumbprint { get; set; }
	}
}
