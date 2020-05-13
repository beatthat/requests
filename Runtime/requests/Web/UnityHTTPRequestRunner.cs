using System.Collections.Generic;

namespace BeatThat.Requests
{
    public delegate UnityHTTPRequest BeforeSendMiddleware(
        UnityHTTPRequest req,
        BeforeSendMiddlewareNextFunction next);

    public delegate UnityHTTPRequest BeforeSendMiddlewareNextFunction(
        UnityHTTPRequest req
    );


    public static class ApplyMiddlewareExt
    {
        public static UnityHTTPRequest ApplyMiddleware(
            this UnityHTTPRequest req,
            IList<BeforeSendMiddleware> middleware,
            int index = 0)
        {
            return middleware[index](
                req,
                (nextReq) =>
                {
                    return index + 1 < middleware.Count
                        ? ApplyMiddleware(nextReq, middleware, index + 1)
                        : nextReq;
                }
            );
        }
    }

    public interface UnityHTTPRequestRunner
    {
        void Execute(UnityHTTPRequest req);

        /// Remove previously added middleware (if found)
        bool Remove(BeforeSendMiddleware middleware);

        /// Add middleware that can modify a request before send.
        /// An example use would be to add auth headers on all
        /// requests to a specific API.
        void Use(BeforeSendMiddleware m);
    }
}
