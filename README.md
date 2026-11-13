# Statsig Unity SDK

[SDK documentation](https://docs.statsig.com/client/unitySDK)

## Proxy endpoints and request headers

Configure full URLs independently for initialization and event logging:

```csharp
using System.Collections.Generic;
using StatsigUnity;

var options = new StatsigOptions
{
    InitializationUrl = "https://proxy.example/lcf/statsig/client/v1/initialize",
    EventLoggingUrl = "https://proxy.example/lcf/statsig/client/v1/events",
    RequestHeaders = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer <initial-token>",
        ["Lcf-Client-App-Id"] = "my-vr-app"
    }
};

await Statsig.Initialize("client-<sdk-key>", new StatsigUser { UserID = "user-id" }, options);

// After your application obtains a refreshed token:
Statsig.SetRequestHeader("Authorization", "Bearer <refreshed-token>");

Statsig.LogEvent("training_started");
await Statsig.Flush();

// Remove an optional header when it is no longer needed:
Statsig.RemoveRequestHeader("Lcf-Client-App-Id");
```

`StatsigClient` exposes the same `SetRequestHeader(name, value)` and
`RemoveRequestHeader(name)` methods for applications managing client instances.
The static methods require `Statsig.Initialize` to have created a client, consistent
with the other static APIs. Call Unity-facing client APIs from Unity's main thread.

### Endpoint precedence

| Request | First choice | Fallback |
| --- | --- | --- |
| Initialize and UpdateUser | `InitializationUrl` | Existing `ApiUrlBase` plus `/initialize`, or the default initialization host |
| Event logging, including scheduled, explicit and graceful-shutdown flushes | `EventLoggingUrl` | Existing `ApiUrlBase` plus `/log_event`, or the default logging host |

Null or empty overrides are absent. An override must be an absolute HTTP(S) URL
without embedded credentials or a fragment; invalid overrides throw
`ArgumentException` when constructing the client. Use HTTPS for credentials.
The SDK passes the configured URL to Unity without appending a suffix or
rebuilding its path/query. Unity's underlying URL handling still applies.

The SDK copies the endpoint values at client construction. Mutating the options
afterward does not change destinations. Existing applications that do not use
these options retain their previous endpoint behavior.

### Header ownership and token rotation

- Initial headers are copied into client-owned state at construction. Later
  mutations to the original dictionary have no effect. Use the supported methods.
- Names are case-insensitive. Setting `authorization` replaces `Authorization`;
  removal uses the same comparison. Removing an absent custom header is a no-op.
- Each request attempt takes a consistent snapshot. Updates affect future
  attempts, including retries started after backoff, but cannot alter a request
  already in flight. Events queued before rotation use the headers current when
  their request attempt is created. Multiple header changes are separate operations,
  not an atomic batch.
- Header state is isolated between client instances. Only header state access is
  synchronized; this does not make all other SDK APIs thread-safe.
- `Content-Type`, `STATSIG-API-KEY`, and `STATSIG-CLIENT-TIME` are SDK-owned.
  Setting or removing them, in any casing, throws `ArgumentException`.
- Names must be nonempty HTTP tokens. Values must be nonempty printable ASCII
  (characters 32–126). Null, empty, control-character, and non-ASCII values throw
  `ArgumentException`. Initial options and runtime setters use the same validation.
  Use removal instead of a null or empty value. Unity/platform restrictions on
  other transport-managed headers still apply.
- The application acquires and refreshes tokens. This feature does not implement
  OAuth or add automatic retries for HTTP 401 responses.

### Destinations and redirects

Custom headers are sent to **both** configured destinations. If you override only
one URL, the other still uses `ApiUrlBase` or the Statsig default. Configure both
URLs when credentials must remain within your proxy infrastructure.

Requests with any custom headers disable automatic redirects. Configure the final
destination directly: a 3xx response will not forward credentials or trigger a
new redirect-specific retry policy. Requests without custom headers retain their
existing redirect behavior. There is no new fallback to a different host.

Graceful shutdown uses the same destination/header rules. It does not guarantee
delivery after an operating-system kill or forced process termination.

## Validation

The companion private Kong repository contains:

- `tests/unity-proxy`: deterministic xUnit tests linking this SDK's sources and
  Kong's fake Unity layer, with a local HTTP recording server.
- `src/__tests__/precomputed-client/network/unity-proxy.test.ts`: conformance
  tests for bridge option forwarding, header actions, and captured HTTP requests.

From the Kong checkout, with .NET 8 installed:

```sh
dotnet test tests/unity-proxy/UnityProxy.Tests.csproj \
  -p:UnitySdkPath=/absolute/path/to/unity-sdk
```

For another installed supported .NET SDK, set `-p:TestTargetFramework=net10.0`
(or the corresponding target). These tests use HttpClient-backed fake Unity
networking. They do not establish UnityWebRequest, IL2CPP, TLS, or headset compatibility.

See [the real Unity smoke-test instructions](Tests~/ProxySmoke/README.md) for
Editor and Android IL2CPP execution against a controlled proxy. Validate each
Unity version and device before making compatibility claims.
