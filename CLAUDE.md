# Claude Code Project Rules — adl-docs-service

1. Use modern C# (C# 12) and .NET 8 idioms.
2. Prioritize clean architecture: Endpoints, Services, Models.
3. Assume Rider as the primary IDE. Use file paths in /src/Api and /tests/Api.Tests.
4. Never introduce Controllers or MVC unless requested. Use Minimal API only.
5. Always default to async methods.
6. Ensure testability: avoid static classes, prefer DI.
7. When creating code files, place them in the correct folder:
    - Endpoints → src/Api/Endpoints
    - Models → src/Api/Models
    - Services → src/Api/Services
8. When modifying multiple files, output a unified diff patch.
9. Respect the project name and namespace:
    - Namespace = Ammons.DataLabs.DocsService
10. When reasoning about features, include:
    - DI registration
    - Endpoint mapping
    - Model validation
    - Corresponding test scaffolding
11. DON'T use emojis or emoticons in project files.
12. DON'T include any contribution details in ANY commits, include any "Co-Authored by Claude" comments.
13. ALWAYS present commit messages for review, NEVER perform a commit without a confirmation from the user.