﻿using System;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MongoDb.Views
{
    [TestFixture]
    public class TestMongoDbViewDispatcher : FixtureBase
    {
        MongoDatabase _database;
        BasicViewManager _viewManager;
        MongoDbViewDispatcher<SomeView> _viewDispatcher;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _viewDispatcher = new MongoDbViewDispatcher<SomeView>(_database.GetCollection<SomeView>("views"));
            _viewManager = new BasicViewManager(new IViewDispatcher[]{_viewDispatcher});
        }

        [Test]
        public void CanDispatchEvents()
        {
            var firstRoot = Guid.NewGuid();
            var secondRoot = Guid.NewGuid();

            _viewManager.Dispatch(new DomainEvent[] { EventFor(firstRoot) });
            _viewManager.Dispatch(new DomainEvent[] { EventFor(firstRoot) });
            _viewManager.Dispatch(new DomainEvent[] { EventFor(secondRoot) });

            var viewInstances = _viewDispatcher.ToList();

            Assert.That(viewInstances.Count, Is.EqualTo(2));

            Assert.That(viewInstances.Count(i => i.AggregateRootId == firstRoot), Is.EqualTo(1),
                "Expected one single view instance for aggregate root {0}", firstRoot);

            Assert.That(viewInstances.Single(i => i.AggregateRootId == firstRoot).NumberOfEventsHandled, Is.EqualTo(2),
                "Expected two events to have been processed");

            Assert.That(viewInstances.Count(i => i.AggregateRootId == secondRoot), Is.EqualTo(1),
                "Expected one single view instance for aggregate root {0}", secondRoot);

            Assert.That(viewInstances.Single(i => i.AggregateRootId == secondRoot).NumberOfEventsHandled, Is.EqualTo(1),
                "Expected one event to have been processed");
        }

        [TestCase(1000)]
        [TestCase(10000)]
        public void CheckPerformance(int numberOfEvents)
        {
            var firstRoot = Guid.NewGuid();

            TakeTime("Dispatch " + numberOfEvents + " events",
                () => numberOfEvents.Times(() => _viewManager.Dispatch(new DomainEvent[] { EventFor(firstRoot) })));
        }

        static SomeEvent EventFor(Guid newGuid)
        {
            var e = new SomeEvent();
            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = newGuid;
            return e;
        }

        class SomeEvent : DomainEvent
        {

        }

        class SomeView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<SomeEvent>, IMongoDbView
        {
            public SomeView()
            {
                NumberOfEventsHandled = 0;
            }

            public Guid AggregateRootId { get; set; }

            public int NumberOfEventsHandled { get; set; }

            public void Handle(SomeEvent domainEvent)
            {
                AggregateRootId = new Guid(domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString());

                NumberOfEventsHandled++;
            }

            public string Id { get; set; }
        }

    }
}