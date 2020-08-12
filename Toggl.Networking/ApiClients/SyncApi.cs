using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;

namespace Toggl.Networking.ApiClients
{
    internal sealed class SyncApi : BaseApi, ISyncApi
    {
        private readonly SyncApiEndpoints endpoints;
        private readonly IApiClient apiClient;
        private readonly IJsonSerializer serializer;
        private readonly IEnumerable<HttpHeader> headers; 

        public SyncApi(Endpoints endpoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endpoints.LoggedIn)
        {
            this.endpoints = endpoints.SyncServerEndpoints;
            this.apiClient = apiClient;
            this.serializer = serializer;

            headers = new[] {
                AuthHeader,
                new HttpHeader("X-Toggl-Client", "mobile")
            };
        }

        public async Task<Sync.Pull.IResponse> Pull(DateTimeOffset? since)
            => await SendRequest<Sync.Pull.Response>(endpoints.Pull(since), headers);

        public async Task<Sync.Push.IResponse> Push(Guid id, Sync.Push.Request request)
        {
            var body = serializer.Serialize(request, SerializationReason.Post);
            return await SendRequest<Sync.Push.Response>(endpoints.Push(id), headers, body);
        }
        public async Task<Sync.Push.IResponse> OutstandingPush(Guid id)
            => await SendRequest<Sync.Push.Response>(endpoints.OutstandingPush(id), headers);
    }
}
