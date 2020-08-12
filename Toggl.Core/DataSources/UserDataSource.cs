﻿using System;
using System.Reactive.Linq;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Sync.ConflictResolution;
using Toggl.Shared;
using Toggl.Storage;
using Toggl.Storage.Models;

namespace Toggl.Core.DataSources
{
    internal sealed class UserDataSource : SingletonDataSource<IThreadSafeUser, IDatabaseUser>
    {
        private ISingleObjectStorage<IDatabaseUser> storage;

        public UserDataSource(ISingleObjectStorage<IDatabaseUser> storage)
            : base(storage, null)
        {
            this.storage = storage;
        }

        protected override IThreadSafeUser Convert(IDatabaseUser entity)
            => User.From(entity);

        protected override ConflictResolutionMode ResolveConflicts(IDatabaseUser first, IDatabaseUser second)
            => Resolver.ForUser.Resolve(first, second);

        public override IObservable<IThreadSafeUser> Update(IThreadSafeUser entity)
            => storage.Single()
            .Do(userDb => backupUser(userDb, entity))
            .SelectMany(_ => base.Update(entity));

        private void backupUser(IDatabaseUser userDb, IThreadSafeUser user)
        {
            if (userDb.DefaultWorkspaceIdSyncStatus == PropertySyncStatus.InSync)
            {
                user.DefaultWorkspaceIdSyncStatus = PropertySyncStatus.SyncNeeded;
                user.DefaultWorkspaceIdBackup = userDb.DefaultWorkspaceId;
            }

            if (userDb.BeginningOfWeekSyncStatus == PropertySyncStatus.InSync)
            {
                user.BeginningOfWeekSyncStatus = PropertySyncStatus.SyncNeeded;
                user.BeginningOfWeekBackup = userDb.BeginningOfWeek;
            }
        }
    }
}
