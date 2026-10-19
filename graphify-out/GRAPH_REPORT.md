# Graph Report - Backend  (2026-10-19)

## Corpus Check
- 17 files · ~12,414 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 7 file(s) not represented in the graph (top: .props 2, .targets 2, .cache 2)

## Summary
- 43 nodes · 38 edges · 8 communities (4 shown, 4 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e50fddc4`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- https
- http
- Api
- Tests.csproj
- UnitTest1.cs
- CLAUDE.md
- README.md

## God Nodes (most connected - your core abstractions)
1. `Api` - 7 edges
2. `http` - 7 edges
3. `https` - 7 edges
4. `profiles` - 3 edges
5. `environmentVariables` - 2 edges
6. `ASPNETCORE_ENVIRONMENT` - 2 edges
7. `environmentVariables` - 2 edges
8. `UnitTest1` - 2 edges
9. `net10.0` - 1 edges
10. `Microsoft.AspNetCore.OpenApi (10.0.13)` - 1 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Import Cycles
- None detected.

## Communities (8 total, 4 thin omitted)

### Community 0 - "https"
Cohesion: 0.22
Nodes (8): applicationUrl, commandName, dotnetRunMessages, launchBrowser, launchUrl, profiles, https, $schema

### Community 1 - "http"
Cohesion: 0.22
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, environmentVariables (+1 more)

### Community 2 - "Api"
Cohesion: 0.25
Nodes (7): Api, net10.0, Microsoft.AspNetCore.OpenApi (10.0.13), Microsoft.EntityFrameworkCore.Design (10.0.13), Microsoft.VisualStudio.Web.CodeGeneration.Design (10.0.2), Swashbuckle.AspNetCore.SwaggerUI (10.3.0), Microsoft.NET.Sdk.Web

### Community 3 - "Tests.csproj"
Cohesion: 0.29
Nodes (6): coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, net10.0

## Knowledge Gaps
- **12 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)`, `Microsoft.VisualStudio.Web.CodeGeneration.Design (10.0.2)`, `Swashbuckle.AspNetCore.SwaggerUI (10.3.0)` (+7 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 30 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `http` connect `http` to `https`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **What connects `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` to the rest of the system?**
  _12 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Why does `https` connect `https` to `http`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **Why does `profiles` connect `https` to `http`?**
  _High betweenness centrality (0.084) - this node is a cross-community bridge._