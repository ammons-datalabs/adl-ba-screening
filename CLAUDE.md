# Claude Code Project Rules — adl-ba-screening

1. Use modern C# (C# 12) and .NET 8 idioms.
2. Prioritize clean architecture: Endpoints, Services, Models.
3. Assume Rider as the primary IDE.
4. Never introduce Controllers or MVC unless requested. Use Minimal API only.
5. Always default to async methods.
6. Ensure testability: avoid static classes, prefer DI.
7. When creating code files, place them in the correct folder:
    - Endpoints → src/AmmonsDataLabs.BuyersAgent.Screening.Api/Endpoints
    - Models → src/AmmonsDataLabs.BuyersAgent.Screening.Api/Models
    - Services → src/AmmonsDataLabs.BuyersAgent.Screening.Api/Services
    - Domain → src/AmmonsDataLabs.BuyersAgent.Flood (flood screening domain)
8. Respect the project name and namespace:
    - Namespace = AmmonsDataLabs.BuyersAgent.Screening.Api (for API)
    - Namespace = AmmonsDataLabs.BuyersAgent.Flood (for flood domain)
9. When reasoning about features, include:
    - DI registration
    - Endpoint mapping
    - Model validation
    - Corresponding test scaffolding
10. DON'T use emojis or emoticons in project files.
11. DON'T include any contribution details in ANY commits, include any "Co-Authored by Claude" comments.
12. ALWAYS present commit messages for review, NEVER perform a commit without a confirmation from the user.