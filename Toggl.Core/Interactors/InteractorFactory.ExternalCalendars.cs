using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Toggl.Core.Models;
using Toggl.Shared.Models.Calendar;

namespace Toggl.Core.Interactors
{
    public partial class InteractorFactory
    {
        public IInteractor<Task<SyncOutcome>> SyncExternalCalendars()
            => new SyncExternalCalendarsInteractor(this, timeService, lastTimeUsageStorage);

        public IInteractor<Task<List<ICalendarIntegration>>> PullCalendarIntegrations()
            => new PullCalendarIntegrationsInteractor(api);

        public IInteractor<Task<IEnumerable<IExternalCalendar>>> PullExternalCalendars(ICalendarIntegration integration)
            => new PullExternalCalendarsInteractor(api, integration);

        public IInteractor<Task<IEnumerable<IExternalCalendarEvent>>> PullExternalCalendarEvents(ICalendarIntegration integration, IExternalCalendar calendar)
            => new PullExternalCalendarEventsInteractor(api, timeService, integration, calendar);

        public IInteractor<Unit> PersistExternalCalendarsData(Dictionary<IExternalCalendar, IEnumerable<IExternalCalendarEvent>> calendarData)
            => new PersistExternalCalendarsDataInteractor(queryFactory, calendarData);

        public IInteractor<Task<Unit>> PushSelectedExternalCalendars()
            => new PushSelectedExternalCalendarsInteractor(dataSource, api);
        public IInteractor<Unit> SetExternalCalendarSelected(long calendarId, bool selected)
            => new SetExternalCalendarSelectedInteractor(dataSource, calendarId, selected);
    }
}
