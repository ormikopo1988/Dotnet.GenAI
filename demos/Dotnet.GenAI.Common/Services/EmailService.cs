using System;
using System.Threading.Tasks;

namespace Dotnet.GenAI.Common.Services
{
    public class EmailService
    {
        public Task EmailFriend(string friendName, string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine($"Emailing {friendName} with: {message}");
            
            Console.ResetColor();
            
            return Task.CompletedTask;
        }
    }
}
