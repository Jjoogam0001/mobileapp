﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.Interactors;
using Toggl.Shared;
using Toggl.Storage.Settings;

namespace Toggl.Core.Services
{
    public sealed class BackgroundService : IBackgroundService
    {
        private readonly ITimeService timeService;
        private readonly IAnalyticsService analyticsService;
        private readonly IUpdateRemoteConfigCacheService updateRemoteConfigCacheService;
        private readonly IUnsyncedDataPersistenceService unsyncedDataPersistenceService;
        private readonly IInteractorFactory interactorFactory;

        private DateTimeOffset? lastEnteredBackground { get; set; }
        private ISubject<TimeSpan> appBecameActiveSubject { get; }

        public IObservable<TimeSpan> AppResumedFromBackground { get; }

        public bool AppIsInBackground { get; private set; }

        public BackgroundService(ITimeService timeService,
            IAnalyticsService analyticsService,
            IUpdateRemoteConfigCacheService updateRemoteConfigCacheService,
            IUnsyncedDataPersistenceService unsyncedDataPersistenceService,
            IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(updateRemoteConfigCacheService, nameof(updateRemoteConfigCacheService));
            Ensure.Argument.IsNotNull(unsyncedDataPersistenceService, nameof(unsyncedDataPersistenceService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.timeService = timeService;
            this.analyticsService = analyticsService;
            this.updateRemoteConfigCacheService = updateRemoteConfigCacheService;
            this.unsyncedDataPersistenceService = unsyncedDataPersistenceService;
            this.interactorFactory = interactorFactory;

            appBecameActiveSubject = new Subject<TimeSpan>();
            lastEnteredBackground = null;

            AppResumedFromBackground = appBecameActiveSubject.AsObservable();
            AppIsInBackground = false;
        }

        public void EnterBackgroundFetch()
        {
            AppIsInBackground = true;
        }

        public void EnterBackground()
        {
            analyticsService.AppSentToBackground.Track();
            lastEnteredBackground = timeService.CurrentDateTime;
            AppIsInBackground = true;
            unsyncedDataPersistenceService?.PersistUnsyncedData().ConfigureAwait(false);
        }

        public void EnterForeground()
        {
            if (updateRemoteConfigCacheService.NeedsToUpdateStoredRemoteConfigData())
                Task.Run(() => updateRemoteConfigCacheService.FetchAndStoreRemoteConfigData()).ConfigureAwait(false);

            interactorFactory.UpdateEventNotificationsSchedules().Execute().ConfigureAwait(false);

            if (lastEnteredBackground.HasValue == false)
                return;

            AppIsInBackground = false;
            var timeInBackground = timeService.CurrentDateTime - lastEnteredBackground.Value;
            lastEnteredBackground = null;
            appBecameActiveSubject.OnNext(timeInBackground);
            analyticsService.AppDidEnterForeground.Track();
        }
    }
}
