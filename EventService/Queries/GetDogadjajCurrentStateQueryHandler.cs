using EventService.EventSourcing;

namespace EventService.Queries
{
    public class GetDogadjajCurrentStateQueryHandler
    {
        private readonly IEventStoreService _eventStore;

        public GetDogadjajCurrentStateQueryHandler(IEventStoreService eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<DogadjajState> Handle(GetDogadjajCurrentStateQuery query)
            => _eventStore.GetCurrentStateAsync(query.DogadjajId);
    }
}