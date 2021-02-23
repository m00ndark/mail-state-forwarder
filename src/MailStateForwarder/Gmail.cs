using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace MailStateForwarder
{
	public static class Gmail
	{
		const string SECRETS_FILE = @"C:\git\github\m00ndark\mail-state-monitor\client_secrets_desktop.json";

		public static async Task<UserCredential> AuthorizeAsync(string userId)
		{
			await using (FileStream stream = new FileStream(SECRETS_FILE, FileMode.Open, FileAccess.Read))
			{
				return await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					new[] { GmailService.Scope.GmailReadonly },
					userId,
					CancellationToken.None,
					new FileDataStore("Google.Apis.Auth"));
			}
		}

		public static async Task<(string Subject, string Content)> GetLatestMessageAsync(string userId, string fromFilter)
		{
			UserCredential credential = await AuthorizeAsync(userId);

			GmailService service = new GmailService(new BaseClientService.Initializer
				{
					HttpClientInitializer = credential,
					ApplicationName = "Mail State Forwarder"
				});

			//IList<Message> messages = await service.GetAllMessages(userId, "from:verisure is:unread");

			//while (messages.Any())
			//{
			//	await service.BatchSetRead(userId, messages);
			//	messages = await service.GetAllMessages(userId, "from:verisure is:unread");
			//}

			Message latestMessage = (await service.GetMessages(userId, $"from:{fromFilter}")).Skip(7).FirstOrDefault();

			if (latestMessage == null)
				return (null, null);

			return await service.GetFullMessage(userId, latestMessage.Id);
		}

		private static async Task<IList<Message>> GetAllMessages(this GmailService service, string userId, string query)
		{
			List<Message> messages = new List<Message>();
			UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(userId);
			request.Q = query;

			do
			{
				ListMessagesResponse response = await request.ExecuteAsync();
				messages.AddRange(response.Messages);
				request.PageToken = response.NextPageToken;
			}
			while (!string.IsNullOrEmpty(request.PageToken));

			return messages;
		}

		private static async Task<IList<Message>> GetMessages(this GmailService service, string userId, string query)
		{
			UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(userId);
			request.Q = query;
			ListMessagesResponse response = await request.ExecuteAsync();
			return response.Messages;
		}

		private static async Task<(string Subject, string Content)> GetFullMessage(this GmailService service, string userId, string latestMessageId)
		{
			UsersResource.MessagesResource.GetRequest getRequest = service.Users.Messages.Get(userId, latestMessageId);
			getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
			Message message = await getRequest.ExecuteAsync();
			return (message.GetSubject(), message.GetContent());
		}

		private static string GetSubject(this Message message)
		{
			return message.Payload?.Headers.FirstOrDefault(header => header.Name == "Subject")?.Value;
		}

		private static string GetContent(this Message message, string mimeType = "text/plain")
		{
			try
			{
				string[] content = message.Payload.GetContent(mimeType).ToArray();

				return !content.Any()
					? Base64Url.Decode(message.Payload?.Body?.Data) ?? string.Empty
					: string.Join(Environment.NewLine, content);
			}
			catch (Exception ex)
			{
				return message.Snippet;
			}
		}

		private static IEnumerable<string> GetContent(this MessagePart messagePart, string mimeType)
		{
			IList<MessagePart> parts = messagePart?.Parts;

			if (parts == null)
				yield break;

			foreach (MessagePart part in parts)
			{
				if (part.MimeType == mimeType)
					yield return Base64Url.Decode(part.Body?.Data);

				foreach (string content in part.GetContent(mimeType))
				{
					yield return content;
				}
			}
		}

		private static async Task BatchSetRead(this GmailService service, string userId, IEnumerable<Message> messages)
		{
			UsersResource.MessagesResource.BatchModifyRequest batchModifyRequest = service.Users.Messages.BatchModify(new BatchModifyMessagesRequest
				{
					Ids = messages.Select(message => message.Id).ToArray(),
					RemoveLabelIds = new[] { "UNREAD" }
				}, userId);

			await batchModifyRequest.ExecuteAsync();
		}
	}
}
