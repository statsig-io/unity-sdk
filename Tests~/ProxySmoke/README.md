# Real Unity proxy smoke test

This fixture exercises the public SDK through actual UnityWebRequest. The companion
Node server checks received paths, queries, headers, user changes, event payloads,
token rotation/removal, and graceful shutdown. It uses synthetic credentials only.

## Setup

1. Create a disposable Unity project using the target Editor version. Add this SDK
   checkout as a local package and install that Editor's compatible Unity Test Framework.
2. Copy `ProxySmokeTests.cs` and `Statsig.ProxySmoke.asmdef` from this directory to
   `Assets/Tests/ProxySmoke/`. This directory ends in `~` so Unity does not import
   these optional fixtures into customer projects automatically.
3. Create `Assets/Tests/ProxySmoke/Resources/statsig-proxy-test-url.txt` containing
   the proxy URL, e.g. `http://127.0.0.1:8787` for a local Editor run.
4. Start the controlled server with Node 18 or newer:

   ```sh
   node /absolute/path/to/unity-sdk/Tests~/ProxySmoke/server.cjs
   ```

For a device, use a host reachable from the device, not the device's own loopback
address. `HOST=0.0.0.0` allows LAN connections; restrict access to the test network.
Alternatively, use `adb reverse tcp:8787 tcp:8787` for a USB-connected Android device.

Use trusted HTTPS for the platform/TLS check. The server supports `TLS_CERT` and
`TLS_KEY` paths for a certificate valid for its hostname and trusted by the device,
or place it behind a controlled HTTPS reverse proxy. Do not add a permissive
CertificateHandler or disable certificate verification. Plain HTTP may require a
development-only Unity/Android setting; it does not validate production TLS.

## Execute

Run `ProxySmokeTests` in the Test Runner's PlayMode tab. For an unattended Editor run:

```sh
"$UNITY_EDITOR" -batchmode -projectPath "$SMOKE_PROJECT" \
  -runTests -testPlatform PlayMode -testFilter ProxySmokeTests \
  -testResults "$RESULTS_DIR/unity-proxy.xml" -logFile "$RESULTS_DIR/unity-proxy.log"
```

For Android, install the Android build support, SDK and NDK matching the Editor,
connect a supported device, and select ARM64/IL2CPP in Player Settings. Run the
PlayMode tests on the Android player using Test Runner, or the matching Test Framework
CLI with `-testPlatform Android -testSettingsFile /path/to/android-tests.json`:

```json
{ "scriptingBackend": "IL2CPP", "architecture": 2 }
```

The architecture value `2` denotes ARM64 in the Unity 2021-era Test Framework.
Check the installed Test Framework's settings schema when changing versions. A successful
build is not a successful player test. Retain NUnit XML, Editor/player logs, version,
backend, architecture, device model, and whether HTTP or trusted HTTPS was used.

Run separately on the customer-confirmed Unity 2021.3.58f1 and planned Unity
6000.3.27f2, subject to availability. Run on the customer's Focus 3/Focus Vision
and tablet targets before claiming compatibility with those devices. A customer
beta against the real proxy remains a separate validation step.

## Expected outcome and limits

One test passes after six captured POSTs: initialize/event with token A,
UpdateUser/event with token B, then UpdateUser/shutdown event with no Authorization
header. The app-id and SDK-required headers remain present throughout.

The focused Kong xUnit suite covers retry timing, invalid inputs, independent
endpoint overrides, scheduled flushes and isolation. This smoke fixture supplies
the additional real-transport/platform evidence; it does not replace those tests.

The fixture was authored without an installed Unity Editor or Android device.
Do not interpret its presence or a passing .NET shim run as an executed Unity test.
