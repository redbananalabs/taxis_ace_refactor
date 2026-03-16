# DTO Structure Rules

This repository uses the following DTO conventions for maintainability:

1. Use one top-level public type per DTO file.
2. Keep DTO namespaces aligned to their domain/feature folders.
3. Remove declaration-only DTO types (types that are not referenced beyond their declaration).

## Enforcement

The test project includes guardrails in `DtoStructureTests`:

1. `DtoFiles_ShouldContainSingleTopLevelPublicType`
2. `DtoTypes_ShouldBeReferencedBeyondTheirDeclaration`

These tests run as part of `dotnet test TaxiDispatch.sln`.
