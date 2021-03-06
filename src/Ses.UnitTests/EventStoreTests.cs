﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.UnitTests.Fakes;
using Xunit;

namespace Ses.UnitTests
{
    public class EventStoreTests
    {
        private readonly IEventStore _store;

        public EventStoreTests()
        {
            _store = new EventStoreBuilder()
                .WithInMemoryPersistor()
                .WithDefaultContractsRegistry(typeof(FakeEvent1).Assembly)
                .WithSerializer(new JsonNetSerializer())
                .Build();
        }

        [Fact]
        public async Task Load_event_stream_after_storing_2_messages_returns_2()
        {
            var streamId = Guid.Empty;
            var events = new IEvent[]
            {
                new FakeEvent1(),
                new FakeEvent2()
            };
            IEventStream stream = new EventStream(Guid.Empty, events);
            await _store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            var restoredStream = await _store.LoadAsync(streamId, 1, false);

            Assert.True(restoredStream.CommittedEvents.Length == events.Length);
        }

        [Fact]
        public async Task Storing_empty_collection_of_events_do_nothing_and_after_load_returns_null()
        {
            var streamId = Guid.Empty;
            var events = new IEvent[0];
            IEventStream stream = new EventStream(Guid.Empty, events);
            await _store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            var restoredStream = await _store.LoadAsync(streamId, 1, false);

            Assert.Null(restoredStream);
        }

        [Fact]
        public async Task Can_not_save_changes_with_forbidden_expected_version()
        {
            var streamId = Guid.Empty;
            IEventStream stream = new EventStream(Guid.Empty, new IEvent[0]);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _store.SaveChangesAsync(streamId, -2, stream);
            });
        }
    }
}