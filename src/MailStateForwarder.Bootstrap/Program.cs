using System.Threading.Tasks;
using MailStateForwarder.Model;

namespace MailStateForwarder.Bootstrap
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Configuration.Load();
			await Gmail.AuthorizeAsync(Configuration.Instance.Gmail.UserId);
		}
    }
}
