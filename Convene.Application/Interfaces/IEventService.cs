using Convene.Application.DTOs.Event;


namespace Convene.Application.Interfaces
{
    public interface IEventService
    {
     

        Task<EventResponseDto> CreateEventAsync(EventCreateDto dto, Guid organizerId);
        Task<EventResponseDto> UpdateEventAsync(EventUpdateDto dto ,Guid eventid);
        Task<bool> PublishEventAsync(Guid eventId);
        Task<EventResponseDto> GetEventByIdAsync(Guid eventId);
        Task<List<EventResponseDto>> GetOrganizerEventsAsync(Guid organizerId);
        List<TicketTypeResponseDto> GetDefaultTicketTypes();
        List<PricingRuleResponseDto> GetDefaultPricingRules();
       
        Task<EventTelegramDto?> CompileEventTelegramDataAsync(Guid eventId);

    }
}
