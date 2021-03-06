using System;
using Toggl.Networking.Helpers;
using ReportsEndpoints = Toggl.Networking.Network.Reports.Endpoints;
using IntegrationsEndpoints = Toggl.Networking.Network.Integrations.Endpoints;

namespace Toggl.Networking.Network
{
    internal sealed class Endpoints
    {
        private readonly Uri baseUrl;
        private readonly Uri syncServerBaseUrl;

        public AuthEndpoints Auth => new AuthEndpoints(baseUrl);
        public UserEndpoints User => new UserEndpoints(baseUrl);
        public WorkspaceEndpoints Workspaces => new WorkspaceEndpoints(baseUrl);
        public ClientEndpoints Clients => new ClientEndpoints(baseUrl);
        public ProjectEndpoints Projects => new ProjectEndpoints(baseUrl);
        public TaskEndpoints Tasks => new TaskEndpoints(baseUrl);
        public TimeEntryEndpoints TimeEntries => new TimeEntryEndpoints(baseUrl);
        public TagEndpoints Tags => new TagEndpoints(baseUrl);
        public StatusEndpoints Status => new StatusEndpoints(baseUrl);
        public WorkspaceFeaturesEndpoints WorkspaceFeatures => new WorkspaceFeaturesEndpoints(baseUrl);
        public Endpoint LoggedIn => Endpoint.Get(baseUrl, "me/logged");
        public PreferencesEndpoints Preferences => new PreferencesEndpoints(baseUrl);
        public PushServicesEndpoints PushServices => new PushServicesEndpoints(baseUrl);
        public CountryEndpoints Countries => new CountryEndpoints(baseUrl);
        public LocationEndpoints Location => new LocationEndpoints(baseUrl);
        public FeedbackEndpoints Feedback => new FeedbackEndpoints(baseUrl);
        public TimezoneEndpoints Timezones => new TimezoneEndpoints(baseUrl);
        public SyncApiEndpoints SyncServerEndpoints => new SyncApiEndpoints(syncServerBaseUrl);
        public ReportsEndpoints ReportsEndpoints { get; }
        public IntegrationsEndpoints IntegrationsEndpoints { get; }

        public Endpoints(ApiEnvironment environment)
        {
            baseUrl = BaseUrls.ForApi(environment);
            syncServerBaseUrl = BaseUrls.ForSyncServer(environment);
            ReportsEndpoints = new ReportsEndpoints(environment);
            IntegrationsEndpoints = new IntegrationsEndpoints(environment);
        }
    }
}
