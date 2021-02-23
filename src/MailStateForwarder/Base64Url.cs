using System;
using System.Text;

namespace MailStateForwarder
{
	public static class Base64Url
	{
		public static string Encode(string decoded, bool discardPadding = true)
		{
			if (decoded == null)
				return null;

			string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(decoded))
				.Replace('+', '-')
				.Replace('/', '_');

			if (discardPadding)
				encoded = encoded.TrimEnd('=');

			return encoded;
		}

		public static string Decode(string encoded)
		{
			if (encoded == null)
				return null;

			string urlDecoded = encoded
				.Replace('_', '/')
				.Replace('-', '+');

			urlDecoded = (urlDecoded.Length % 4) switch
				{
					2 => urlDecoded + "==",
					3 => urlDecoded + "=",
					_ => urlDecoded
				};

			return Encoding.UTF8.GetString(Convert.FromBase64String(urlDecoded));
		}
	}
}
