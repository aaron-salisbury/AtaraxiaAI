# AtaraxiaAI Development Conventions

## Purpose and scope

These conventions apply to human contributors and AI coding assistants.
Prefer clear, simple implementations and deliberate contracts over
speculative abstractions.

AtaraxiaAI is an experimental desktop sandbox for machine learning and AI.
It currently combines speech, vision, and service-backed assistant skills
in an Avalonia application. Keep experiments and their limits explicit.

------------------------------------------------------------------------

## 1. General Design Philosophy

### Prefer Simple Designs

Follow KISS. Do not introduce abstractions, layers, patterns, or object
types solely to satisfy an architectural diagram. Every abstraction
should solve a concrete problem or protect an intentional boundary.

Avoid speculative architecture for capabilities the application does not
yet require.

### Enforce Separation of Concerns

Code should live with the responsibility it implements.

-   Presentation does not implement business logic.
-   Business does not implement persistence concerns.
-   Data access does not implement business validation.
-   External integration details do not leak into core business
concepts.
-   Persistence structure does not define business structure.
-   A project reference grants technical access, not architectural
    permission.

Only protocols, services, DTOs, and other types intentionally exposed
across a boundary should be consumed by another tier or capability.

### Preserve Locality of Behaviour

Within Business, organize code so behavior related to a capability is
easy to discover and reason about locally. Prefer cohesive
feature/domain organization over scattering related behavior throughout
large technical folders.

Reference: [Locality of
Behaviour](https://htmx.org/essays/locality-of-behaviour/)

### Prefer Modularity

The application should be modular both horizontally through
architectural boundaries and vertically through cohesive business
capabilities.

Modules should expose deliberate contracts rather than internal
implementation details.

------------------------------------------------------------------------

## 2. .NET Platform

The solution currently targets .NET 10, set in `src/Directory.Build.props`.
The repository pins the .NET 10 SDK family and Cake SDK in `global.json`.
Keep projects on the shared target framework unless a concrete dependency or
deployment requirement justifies a difference. Prefer supported .NET LTS
releases when upgrading the solution.

------------------------------------------------------------------------

## 3. Solution Architecture

The solution uses Business as the application center. Business owns assistant
behavior, concepts, and application-facing contracts. Data and Integrations
implement those contracts. Presentation.Desktop owns Avalonia views and view
models. DesktopApp is the executable composition root: it registers concrete
implementations, starts the app, and owns its lifetime. KokoroWorker is a
separate executable for local speech synthesis.

```text
Business <--- Data
    ^       Integrations
    |
Presentation.Desktop

DesktopApp ---> assembles Business, Data, Integrations, Presentation.Desktop,
                and the KokoroWorker process
```

The arrows describe intended dependency direction, not every current project
reference. A project reference grants technical access, not architectural
permission to place behavior outside its responsibility. Keep model and
provider details inside Integrations, and keep UI behavior in Presentation.

------------------------------------------------------------------------

## 4. Presentation Patterns

The current desktop application uses Avalonia and MVVM. Views own visual
structure and presentation behavior; ViewModels own presentation state,
commands, and coordination; presentation models hold data shaped for binding.
Keep business logic and persistence out of Views and ViewModels. Construct
the root presentation objects through application composition rather than
service location in each View.

The project has no web, mobile, or CLI host today. If one is introduced, let
it consume intentionally exposed Business capabilities and choose a
presentation pattern suited to its platform. Do not introduce generalized
navigation or workspace abstractions until a concrete interaction requires
them.

------------------------------------------------------------------------

## 5. Pattern Naming Conventions

Use architectural suffixes only when a class actually performs that
role.

-   View classes end in `View`.
-   Presenter classes end in `Presenter`.
-   ViewModel classes end in `ViewModel`.
-   Controller classes end in `Controller`.
-   Classes that do not perform these roles must not use these suffixes.

------------------------------------------------------------------------

## 6. Business Object Vocabulary

### Persistent Entities / Data Entities

A **persistent entity** or **data entity** is a uniquely identifiable
persisted data representation. Use these qualified terms in
architectural discussion when ambiguity is possible.

Do not call every business object an entity merely because it has
identity.

Persistence structure does not define business structure. Multiple data
entities may support one business concept, and a data entity may exist
only for caching, synchronization, indexing, auditing, integration
state, or other infrastructure bookkeeping.

Persistent entities should generally remain simple data-oriented
objects.

### DTOs

DTOs are lightweight objects intended to transfer data across an
intentional boundary.

DTOs should:

-   Be serializable by design.
-   Be POCOs.
-   Generally be C# records.
-   Contain data rather than complex behavior.
-   Represent intended contract exposure rather than blindly mirror
persistence or provider representations.

Example:

``` csharp
public sealed record FooDto(
    string Name,
    string? Description,
    Guid FooId);
```

External integrations may have provider-specific DTOs in addition to
app-facing DTOs. Do not expose an external provider DTO directly merely
to avoid writing a mapping.

### Domains / Business Concepts

A **domain** or **business concept** represents a real-world or
app-specific concept required by business logic. It is not defined by
whether or how it persists.

A business representation may have one persistent representation,
multiple persistent representations, or none.

Business objects should generally remain straightforward data-oriented
representations. Prefer focused business services/processes for
non-trivial behavior rather than automatically placing complex behavior
on the objects themselves.

Useful DDD ideas may be adopted when they solve a concrete problem, but
DDD terminology or rich domain objects are not required by default.

### Models

A **Model** is a representation intended for presentation consumption.
Models may represent a view of business concepts, DTOs, or composed
information from multiple sources.

In MVVM and MVC, Models are suitable for presentation consumption and
data binding. Presentation Models belong to Presentation when their
shape exists specifically for UI needs.

### Do Not Manufacture One-to-One Representations

Do not automatically create all of the following for every concept:

``` text
FooEntity
Foo
FooDto
FooModel
```

Create a distinct representation only when a real boundary or
transformation requires it. KISS takes precedence over architectural
ceremony.

------------------------------------------------------------------------

## 7. Business Behavior

### Prefer Focused Processes and Services

Generally favor restrained behavior on data-oriented objects.
Non-trivial operations should normally live in focused
services/processes near the capability they implement.

For example:

``` text
Skills/
├── JokeSkill.cs
├── KnowledgeSkill.cs
└── ResponseSkill.cs
```

Exact organization should follow the capability and Locality of
Behaviour rather than a mandatory folder template.

### Validation

Application DTOs should be validated at the appropriate
business/application boundary.

Do not put business validation in Data, depend on UI validation as sole
enforcement, or treat database constraints as a replacement for business
validation. Database constraints may still enforce data integrity
defensively.

------------------------------------------------------------------------

## 8. Integration Contracts

Business defines application-facing capability contracts such as speech
recognition, synthesis, object detection, OCR, and service-backed skills.
Integrations implements those contracts using local models, platform APIs,
or external providers. The current `IntegrationFactory` selects concrete
implementations; it does not configure a general answer provider.

Keep provider-specific types and transport details within Integrations.
App-facing contracts should use application-defined types. Add a new contract
only for a concrete use case, and avoid one interface per hypothetical
provider. The local JSON store owns app counters, not arbitrary provider data.

Do not commit secrets or write them to application JSON or logs. Resolve
credentials through an appropriate infrastructure boundary when a provider
requires them. If future agents or automation gain write operations, make
their authority explicit at the capability boundary.

------------------------------------------------------------------------

## 9. Expected Failures and Exceptions

Expected operational states should generally be represented explicitly
rather than communicated through exceptions, especially for
integrations.

Examples:

``` text
Success
NotFound
Unavailable
Unauthorized
Forbidden
InvalidResponse
```

A result type may be appropriate. Use exceptions for exceptional or
unexpected failures rather than normal control flow.

AtaraxiaAI references FluentValidation, but validation and provider-result
handling should be added at actual application boundaries as they emerge.
Do not assume a common result type is already established.

Reference: [Exception-Handling Antipatterns and API
Design](https://www.infoq.com/articles/Exceptions-API-Design/)

------------------------------------------------------------------------

## 10. C# Coding Conventions

### Namespaces

Prefer file-scoped namespaces.

``` csharp
namespace AtaraxiaAI.Business.Skills;
```

### Local Variable Declarations

Prefer an explicit type with target-typed construction:

``` csharp
Foo foo = new();
```

over:

``` csharp
var foo = new Foo();
```

Use judgment where explicit typing would make code substantially less
readable.

### Constants

Name constants using uppercase snake case.

``` csharp
private const string API_ENDPOINT = "https://rest.example.com/v1";
```

### Private Fields

Use camel case with a leading underscore.

``` csharp
private int _counter;
```

### Public and Internal Members

Use PascalCase.

``` csharp
public required DateTime TimeStamp { get; set; }
```

### Parameters

- Use ordinary constructors for classes rather than primary constructors.
- Very simple POCOs, DTOs, and records may use primary constructors.
- Keep parameter lists on one line where readable.

### Bodies
- **Always use braces for `if`, `else`, loops, and `lock`, even when the  body contains only one statement.**
- Put braces on separate lines (Allman style). Expand method, constructor,
  operator, accessor, try/catch/finally, and lambda blocks. Put each executable
  statement on its own line; never compress several statements into one line.
- Methods, constructors, operators, and local functions use block bodies.
  Simple expression-bodied properties/indexers/accessors are acceptable. Auto-properties
  may remain on one line because they contain no executable statements.

### General C# Style

- Use four spaces, LF line endings, UTF-8, a final newline, and no trailing whitespace.
- Use `string.Empty` for an empty string.

Except where scoped definitions include more specific conventions,
generally follow
[Microsoft C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).

### HTTP Clients

Prefer the existing HTTP request infrastructure registered in DesktopApp
for application and integration code. Add provider-specific HTTP client
configuration at the composition root when a concrete integration needs it.

Create clients when an operation needs them:

``` csharp
HttpClient httpClient = _httpClientFactory.CreateClient();
```

If using `IHttpClientFactory` directly, create clients when an operation
needs them. The factory manages underlying handler lifetime. Named or
typed clients are appropriate for stable provider-specific settings.

A long-lived `HttpClient` is not inherently incorrect. It can be
appropriate when deliberately configured with connection-lifetime
management such as `SocketsHttpHandler.PooledConnectionLifetime`. Prefer
that approach only when its lifetime and handler policy are intentional
and provide a concrete advantage over the application's existing
`IHttpClientFactory` infrastructure.

The composition root owns application-level HTTP infrastructure; the
integration owns provider and protocol behavior.

### Library Design

Libraries should generally follow the [.NET Framework Design
Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
where applicable.

------------------------------------------------------------------------

## 11. Contributor Coding Guidance

When adding or modifying code:

1.  Preserve Data, Business, Integration, and Presentation boundaries.
2.  Do not introduce provider-specific types into Business.
3.  Do not introduce persistent/data entities into Presentation.
4.  Do not implement business validation in Data.
5.  Do not implement business logic in Presentation.
6.  Prefer focused capability-local services/processes over large
    generic managers.
7.  Prefer simple data-oriented business objects unless object behavior
    provides a clear advantage.
8.  Use records for DTOs unless there is a concrete reason not to.
9.  Do not create DTOs, Models, data entities, interfaces, repositories,
    or mappings merely for architectural symmetry.
10. Prefer explicit contracts at project boundaries.
11. Preserve external systems as canonical owners of information the app
does not intrinsically own.
12. Prefer open protocols and provider abstractions over vendor
    coupling.
13. Follow this document's C# naming and formatting conventions.
14. Ask whether an abstraction solves a current problem before adding
    it.
15. If a convention appears inappropriate for a concrete problem,
    explain the tradeoff rather than silently working around it.
16. Give appropriate credit when implementation or design is substantially
    derived from an identifiable external source rather than merely informed
    by general knowledge. When practical, cite the source near the adopted
    implementation, typically in XML documentation such as a `<remarks>`
    element. This applies even to permissively licensed, public-domain, CC0,
    educational, textbook, or article sources. Use judgment: ordinary
    techniques and broadly learned ideas do not require mechanical citations.

### Decision Heuristics

When deciding where new code belongs:

**Is this about storing app-owned information?**\
It probably belongs in `Data`.

**Is this a rule, concept, process, validation, or capability
contract?**\
It probably belongs in `Business`.

**Does it understand an external application's API, protocol, or
representation?**\
It probably belongs in `Integrations`.

**Does its shape or behavior exist specifically because of a user
interface?**\
It probably belongs in `Presentation.Desktop`.

**Does a new class merely duplicate another representation without
protecting a boundary?**\
Do not create it yet.

**Does a database table exist only for infrastructure reasons?**\
Do not invent a matching business concept.

**Does a business concept persist?**\
That does not make the persistence representation the business model.

------------------------------------------------------------------------

## 12. Guiding Architectural Rule

> **Architecture should make intended behavior easy to find, intended
> dependencies obvious, and unintended coupling difficult.**

Favor explicit boundaries and consistent conventions, but simplicity and
clear responsibility take precedence over pattern compliance for its own
sake.
