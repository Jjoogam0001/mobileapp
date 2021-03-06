using System.Collections.Generic;
using System.Reactive;
using Toggl.Shared.Models.Calendar;

namespace Toggl.Storage.Queries
{
    public interface IQueryFactory
    {
        IQuery<Unit> ProcessPullResult(Networking.Sync.Pull.IResponse response);
        IQuery<Unit> ProcessPushResult(Networking.Sync.Push.IResponse response);
        IQuery<Unit> MarkEntitiesAsSyncing(Networking.Sync.Push.Request request);
        IQuery<Unit> MigrateBackToOldSyncing();
        IQuery<Unit> ResetLocalState(Networking.Sync.Pull.IResponse response);
        IQuery<Unit> PersistExternalCalendarsData(Dictionary<IExternalCalendar, IEnumerable<IExternalCalendarEvent>> calendarData);
    }
}
