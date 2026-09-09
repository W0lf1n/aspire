# @aspire/contracts

The API's wire types, as TypeScript. This package exists so the client and
the server agree on a shape in one place rather than two. `apps/web` imports
it directly; `apps/api` mirrors it in C# in `src/Aspire.Api/Contracts.cs`.

It ships types and one constant. No runtime code, no build step, no published
artifact: `apps/web` consumes the source through the workspace link.

## Changing it

A field added here has to be added in `Contracts.cs` in the same change, with
the same name. The JSON is camelCase on both sides and enums travel as
kebab-case strings, so `DreamStatus` reads `in-progress` in both languages.
