using EventService.Domains;
using EventService.EventSourcing;

namespace EventService.Queries
{
    public class GetDogadjajEventHistoryQueryHandler
    {
        private readonly IEventStoreService _eventStore;

        public GetDogadjajEventHistoryQueryHandler(IEventStoreService eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<List<EventStoreEntry>> Handle(GetDogadjajEventHistoryQuery query)
            => _eventStore.GetHistoryAsync(query.DogadjajId);
    }
}