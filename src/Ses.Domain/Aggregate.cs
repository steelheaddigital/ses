﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;

namespace Ses.Domain
{
    /// <summary>
    /// Base functionality for concrete aggregate
    /// </summary>
    public abstract class Aggregate : IAggregate
    {
        private readonly Dictionary<Type, Action<object>> _handlers = new Dictionary<Type, Action<object>>();
        private readonly IList<IEvent> _uncommittedEvents = new List<IEvent>();

        /// <summary>
        /// Replay existing events to get current state
        /// </summary>
        /// <param name="history">Events from history</param>
        protected void RestoreFrom(IEvent[] history)
        {
            if (history == null || history.Length == 0) return;
            var snapshot = history[0] as IMemento;
            if (snapshot != null)
            {
                CommittedVersion = snapshot.Version;
                RestoreFromSnapshot(snapshot);
            }

            for (var i = snapshot == null ? 0 : 1; i < history.Length; i++)
            {
                Invoke(history[i]);
                CommittedVersion++;
            }
        }

        public virtual void Restore(Guid id, IEnumerable<IEvent> history)
        {
            Id = id;
            RestoreFrom(history.ToArray());
        }

        protected virtual void RestoreFromSnapshot(IMemento memento) { }

        /// <summary>
        /// Returns aggregate identifier
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// Returns committed aggregate version
        /// </summary>
        public int CommittedVersion { get; protected set; }

        protected int UncommittedVersion => CommittedVersion + _uncommittedEvents.Count;

        /// <summary>
        /// Returns new events registered during one scope of changes.
        /// </summary>
        /// <returns>Collection of events</returns>
        public IEnumerable<IEvent> TakeUncommittedEvents()
        {
            var events = _uncommittedEvents.ToArray();
            _uncommittedEvents.Clear();
            return events;
        }

        /// <summary>
        /// Register aggregate internal event handler which should change internal state when an event was invoked.
        /// </summary>
        /// <typeparam name="TEvent">Type of an event</typeparam>
        /// <param name="action">Event handler action</param>
        protected void Handles<TEvent>(Action<TEvent> action) where TEvent : class, IEvent
        {
            _handlers[typeof (TEvent)] = item => action((TEvent)item);
        }

        /// <summary>
        /// Apply a new event and add to pending events to be committed to event store
        /// when transaction completes.
        /// </summary>
        /// <param name="event">An event which should be applied</param>
        protected void Apply(IEvent @event)
        {
            Invoke(@event);
            _uncommittedEvents.Add(@event);
        }

        /// <summary>
        /// Invoke handler to change state of aggregate in response to event.
        /// Event may be an old event from the event store, or may be an event triggered
        /// during the lifetime of this instance.
        /// </summary>
        /// <param name="event">An event which should be invoked</param>
        protected void Invoke(IEvent @event)
        {
            Action<object> action;
            if (!_handlers.TryGetValue(@event.GetType(), out action)) return;
            action(@event);
        }
    }
}