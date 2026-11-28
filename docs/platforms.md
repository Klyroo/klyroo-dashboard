# Platforms

What runs where. The facts below come from the project files and from running the samples; anything not listed here is not verified.

| | `Klyroo.Dashboard.Core` | `Klyroo.Dashboard` |
| :--- | :--- | :--- |
| Target framework | `net10.0` | `net10.0` |
| `IsTrimmable` | true | true |
| `IsAotCompatible` | true | not declared |
| Supported platforms | not restricted | `browser`, `ios`, `android`, `maccatalyst`, `windows` |
| Depends on | nothing | `Klyroo.Dashboard.Core`, `Klyroo.UI` |

## Blazor render modes

The components use plain `@onclick` handlers, so there is no browser-only code path for the tabs, the filters, the refresh button, sorting and the card menu. What differs between modes is whether those handlers can run. A few features do use JavaScript, and none of them is needed to render a block:

- `_content/Klyroo.Dashboard/klyroo-dashboard.js` is a small ES module with one function, `downloadText`, that saves a file. It is imported on demand (`import()` through JS interop) the first time the user clicks Download CSV and `KlyDashboardCard.OnCsvExported` has no handler. Nothing is loaded before that click, and a handler replaces it (see [.NET MAUI Blazor Hybrid](#net-maui-blazor-hybrid)).
- The copy button of `KlyDashboardDetails` calls `navigator.clipboard.writeText` through JS interop. It does nothing outside an interactive render mode.
- The chart tooltip with its arrow-key exploration (`KlyChart`) and the Expand dialog (`KlyModal`) are custom elements of Klyroo.UI, loaded with the Klyroo.UI script that `<KlyApp>` adds. The chart tooltip works in static rendering too, since it runs in the browser; opening the Expand dialog is a click handler, so it needs an interactive render mode.

| Render mode | Blocks render | Interaction (tabs, filters, refresh, Retry, sort, menu) |
| :--- | :--- | :--- |
| Static SSR | Yes, with the data your page loaded before rendering | No, the buttons are inert |
| Interactive Server | Yes | Yes |
| Interactive WebAssembly | Yes | Yes |
| Interactive Auto | Yes | Yes |

For a dashboard you almost always want an interactive mode, since the page itself is made of buttons. Add `@rendermode InteractiveServer` (or `InteractiveWebAssembly`, `InteractiveAuto`) to the page, as in [Getting started](getting-started.md).

In Static SSR, await your resources in `OnInitializedAsync` before the render completes, as the samples do. The page then arrives with its data, but the view tabs, the filter pills and the refresh button do nothing.

**Registration.** Call `AddKlyrooDashboard()` in the server project, and also in the `.Client` project when the page can run in WebAssembly (Interactive WebAssembly or Auto). A page that runs in both places needs the services in both.

**Prerendering runs your loaders twice.** An interactive page is rendered on the server first and then again when the circuit or the WebAssembly runtime starts. Each loader runs once per pass, unless the resource has a `json` type info and the page attaches its controller to `DashboardPersistence` and loads with `LoadAsync`: the prerender then hands the loaded data to the interactive render, which keeps it (see [Hand the prerendered data to the interactive render](guides.md#hand-the-prerendered-data-to-the-interactive-render)). The loader still runs where the prerender happens, on the server, so in WebAssembly or Auto it must work on both sides. Turn prerendering off for the page if the queries are expensive and too large to hand over, for example `@rendermode="new InteractiveServerRenderMode(prerender: false)"`. Dispose the controller in the page's `Dispose` so a prerendered pass leaves no `AutoRefresh` timer behind.

**WebAssembly.** The loaders run in the browser, so they must call an API your client can reach. The parallel stages are concurrent tasks on the single browser thread, not extra threads.

**State is per user.** Create the `DashboardController` in the component, not as a singleton: it holds the filters and data of one user and is not thread-safe.

## .NET MAUI Blazor Hybrid

`Klyroo.Dashboard` declares `ios`, `android`, `maccatalyst` and `windows` as supported platforms, which are the platforms a MAUI app targets. In a Hybrid app:

1. Add the package to the MAUI project and call `builder.Services.AddKlyrooDashboard();` in `MauiProgram.cs`.
2. Reference the stylesheets in `wwwroot/index.html`, in the same order as on the web:

```html
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.preflight.css" />
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.css" />
<link rel="stylesheet" href="_content/Klyroo.Dashboard/klyroo-dashboard.css" />
```

3. Use the components in a Razor page of the `BlazorWebView` as you would in a web app. A Hybrid app renders in the web view on the device, with no server-side prerendering step.

Without a handler, Download CSV starts a browser download through the `_content/Klyroo.Dashboard/klyroo-dashboard.js` module, which a web view does not always support. In a Hybrid app handle `KlyDashboardCard.OnCsvExported`, which receives the CSV text instead, and write the file with the platform file APIs. The copy button of `KlyDashboardDetails` uses `navigator.clipboard.writeText`, which the web views allow for the app's own content.

### Native C# MAUI without a web view

If a MAUI app uses XAML or C# markup instead of a `BlazorWebView`, reference only `Klyroo.Dashboard.Core`. It has no ASP.NET Core, Razor or Klyroo.UI dependency. The controller raises a `Changed` event, not `INotifyPropertyChanged`, so a view model subscribes to it and raises its own change notifications. Marshal to the UI thread yourself, since the event comes from whichever thread completed the load.

The repository also holds native views over that controller, `Klyroo.Dashboard.Maui` (`src/Klyroo.Dashboard.Maui`): the page, the grid and the thirteen blocks as MAUI controls built on `Klyroo.UI.Maui`, with the same type names in the namespace `Klyroo.Dashboard.Maui`. It needs the MAUI workload and a checkout of klyroo-ui next to this repository, so it is not part of `Klyroo.Dashboard.slnx`. Its README lists the controls and what differs from the Blazor ones.

## NativeAOT and trimming

`Klyroo.Dashboard.Core` sets `IsTrimmable` and `IsAotCompatible`, which enables the trim and AOT analyzers when you build it. The CI of this repository builds it with `EnableAotAnalyzer` and `EnableTrimAnalyzer`. The controller, the resources and `DashboardFormat` use no reflection and no dynamic code. The console app from [Getting started](getting-started.md) publishes with `PublishAot` on linux-x64, and the native binary runs:

```bash
dotnet publish -c Release -p:PublishAot=true
```

`DashboardPersistence` writes the data with the `JsonTypeInfo` you give the resource, so that part needs no reflection; two internal calls suppress the trimming warning `IL2026` for the string-only `PersistentComponentState` calls, with a justification in the code. `Klyroo.Dashboard` is marked `IsTrimmable` but not `IsAotCompatible`, and the components inherit from Klyroo.UI. Treat NativeAOT as supported for `Klyroo.Dashboard.Core` only. For Blazor WebAssembly, the trimmer is part of the normal publish; this package opts in to it, but this documentation does not make a claim about AOT-compiled WebAssembly builds of the components.

## Known limitations

- The block texts follow the Klyroo.UI locale. Only English and French are reviewed. `Klyroo.Dashboard.Core` has no locale, so the default options of `Boolean` and `Comparison` filters and `DashboardFormat.FormatRelativeTime` without `DashboardRelativeTimeTexts` stay English.
- The chart block has the features of `KlyChart` but no zoom, and a donut shows a single series. See the [FAQ](faq.md#known-limitations) for what the compact drawing of bar-and-line charts lacks.
- More in the [FAQ](faq.md#known-limitations).
