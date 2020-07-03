using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosAyudamos
{
    class TestRequestRepository : IRequestRepository
    {
        Dictionary<string, Request> requests = new Dictionary<string, Request>();

        public Task<Request> GetAsync(string requestId, bool readOnly = true)
        {
            Request request = default;
            requests.TryGetValue(requestId, out request);
            return Task.FromResult(request);
        }

        public Task<Request> PutAsync(Request request)
        {
            requests[request.RequestId] = request;
            return Task.FromResult(request);
        }
    }
}
