# Changelog

## 0.1.5

- Expand README with array, custom separator, and unflatten examples
- Add LangVersion and TreatWarningsAsErrors to csproj

## 0.1.4

- Add Development section to README
- Add GenerateDocumentationFile and RepositoryType to .csproj

## 0.1.1 (2026-03-10)

- Fix README path in csproj so README displays on nuget.org

## 0.1.0 (2026-03-10)

- Initial release
- `JsonFlattener.Flatten` — flatten nested JSON into dot-notation key-value pairs
- `JsonFlattener.Unflatten` — reconstruct a JSON string from a flat dictionary
- Configurable separator (default: `.`)
- Array indices become numeric keys
