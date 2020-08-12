﻿using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.DataSources;
using Toggl.Shared;
using Toggl.Shared.Models;
using Toggl.Storage;
using static Toggl.Storage.SyncStatus;
using static Toggl.Networking.Sync.Push.ActionType;
using Toggl.Networking.Sync.Push;
using Toggl.Shared.Extensions;

namespace Toggl.Core.Interactors
{
    public class PreparePushRequestInteractor : IInteractor<Task<Request>>
    {
        private readonly ITogglDataSource dataSource;
        private readonly string userAgent;

        public PreparePushRequestInteractor(string userAgent, ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.userAgent = userAgent;
            this.dataSource = dataSource;
        }

        public async Task<Request> Execute()
        {
            var pushRequest = new Request(userAgent);

            await dataSource.Workspaces
                .GetAll(isDirty)
                .Select(entitiesToCreate)
                .Do(pushRequest.CreateWorkspaces);

            await dataSource.Clients
                .GetAll(isDirty)
                .Select(entitiesToCreate)
                .Do(pushRequest.CreateClients);

            await dataSource.Projects
                .GetAll(isDirty)
                .Select(entitiesToCreate)
                .Do(pushRequest.CreateProjects);

            await dataSource.Tags
                .GetAll(isDirty)
                .Select(entitiesToCreate)
                .Do(pushRequest.CreateTags);

            await dataSource.Preferences
                .Get()
                .FirstOrDefaultAsync(isDirty)
                .Do(pushRequest.UpdatePreferences);

            await dataSource.User
                .Get()
                .FirstOrDefaultAsync(isDirty)
                .Do(pushRequest.UpdateUser);

            var timeEntries = await dataSource.TimeEntries
                .GetAll(isDirty)
                .Select(tes => tes.Where(te => !isLocalDelete(te)));

            timeEntries.DistributedExecute(distributeBy: actionType,
                    (Create, pushRequest.CreateTimeEntries),
                    (Delete, pushRequest.DeleteTimeEntries),
                    (Update, timeEntries => pushRequest.UpdateTimeEntries(timeEntries)));

            return pushRequest;
        }

        private IEnumerable<T> entitiesToCreate<T>(IEnumerable<T> entities) where T : IIdentifiable
            => entities.Where(entity => entity.Id < 0);

        private bool isDirty(IDatabaseSyncable entity)
            => entity.SyncStatus == SyncNeeded;

        private bool isLocalDelete<T>(T entity) where T : IDatabaseSyncable, IIdentifiable
            => entity.IsDeleted && entity.Id < 0;

        private ActionType actionType<T>(T entity) where T : IIdentifiable, IDatabaseSyncable
            => entity.IsDeleted
            ? Delete
            : entity.Id >= 0 ? Update : Create;
    }
}
