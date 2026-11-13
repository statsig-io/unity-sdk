# Changelog

## Unreleased

- Add independent `StatsigOptions.InitializationUrl` and `EventLoggingUrl` full
  endpoint overrides while preserving `ApiUrlBase` and default-host behavior.
- Add initial `RequestHeaders` and runtime `SetRequestHeader` / `RemoveRequestHeader`
  methods on `StatsigClient` and `Statsig`, with case-insensitive per-client state
  and fresh snapshots for retry attempts.
- Validate proxy options, protect SDK-managed headers, and disable redirects on
  requests carrying custom headers.
- Construct POST requests using the longstanding UnityWebRequest constructor API.
- Document proxy configuration and add a real Unity smoke-test fixture. Companion
  Kong changes provide automated conformance and focused regression coverage.
