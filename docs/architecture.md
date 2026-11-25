# Architecture — Klyroo.Dashboard

Klyroo.Dashboard follows the strict headless two-layer architecture shared across every repository in **Klyroo**:

```text
+-------------------------------------------------------------------+
|  Klyroo.Dashboard                                                   |
|  Blazor Web (SSR / Server / WASM / Auto) & .NET MAUI Hybrid       |
|  SupportedPlatform: browser, ios, android, maccatalyst, windows   |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|  Klyroo.Dashboard.Core                                              |
|  100% Headless C# · Zero UI Dependency · IsAotCompatible=true     |
|  Works in APIs, Workers, CLI, NativeAOT & Native C# MAUI          |
+-------------------------------------------------------------------+
```

## Design Principles

1. **Zero UI Coupling in `Klyroo.Dashboard.Core`** — State machines, validation rules, ASTs, formatters, and parsers live in `Klyroo.Dashboard.Core` so they can be unit tested in milliseconds with plain `xUnit` without spinning up a renderer.
2. **NativeAOT & Trimming Verified** — `Klyroo.Dashboard.Core` sets `<IsAotCompatible>true</IsAotCompatible>` and `<IsTrimmable>true</IsTrimmable>` with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
3. **Every Render Mode & Native Hybrid** — `Klyroo.Dashboard` avoids browser-only assumptions during prerendering and declares `<SupportedPlatform>` for `browser`, `ios`, `android`, `maccatalyst`, and `windows`.
4. **Composable with `Klyroo.Stack`** — Install `Klyroo.Dashboard` standalone or aggregate the entire ecosystem with `dotnet add package Klyroo.Stack` and `services.AddKlyrooStack()`.

## Supported Platforms

- Headless C# / NativeAOT
- Blazor Server / WASM / Auto
- .NET MAUI Blazor Hybrid
- RFC 4180 CSV Export
