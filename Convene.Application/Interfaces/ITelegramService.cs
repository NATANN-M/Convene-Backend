using Convene.Application.DTOs.Event;

using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface ITelegramService
    {
        /// <summary>
        /// Sends an event to the configured Telegram channel.
        /// </summary>
        /// <param name="dto">Compiled event data including images and videos</param>
        Task SendEventToChannelAsync(EventTelegramDto dto);
    }
}
