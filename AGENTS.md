# AGENTS.md

This file provides guidance for AI coding agents working on the Ancilla project.

## Project Overview

Ancilla is an AI-powered memory assistant that helps users manage todos and knowledge through SMS messaging. It's built with:

- **.NET 10.0** with C# 13
- **.NET Aspire** for cloud-native orchestration
- **Azure Functions** (isolated worker model, v4)
- **Azure Cosmos DB** for data storage
- **Semantic Kernel** for AI/LLM integration
- **Twilio** for SMS messaging
- **OpenAI GPT models** for conversational AI

## Project Structure

```
Ancilla/
├── Ancilla.AppHost/          # .NET Aspire orchestration & infrastructure
├── Ancilla.FunctionApp/      # Azure Functions HTTP endpoints & services
├── Ancilla.ServiceDefaults/  # Shared service configuration
└── *.slnx                    # Solution file
```

## Build & Run Commands

```bash
# Build the solution
dotnet build

# Run locally with Aspire (launches all services + dashboard)
dotnet run --project Ancilla.AppHost

# Run tests (when available)
dotnet test

# Deploy to Azure
azd up
```

## Code Style & Conventions

- Use **file-scoped namespaces**
- Use **primary constructors** for dependency injection
- Use **raw string literals** (`"""`) for multi-line strings
- Prefer **async/await** patterns throughout
- Use **nullable reference types** (enabled project-wide)
- Follow standard C# naming conventions (PascalCase for public members, _camelCase for private fields)
- If an interface has a single implementation then include the interface definition at the top of the implementation file.

## Key Components

### Ancilla.AppHost
- `AppHost.cs` - Aspire distributed application builder, defines all Azure resources
- `FixedNameInfrastructureResolver.cs` - Custom naming for Azure resources
- `aspire-output/` - Generated Bicep templates for infrastructure

### Ancilla.FunctionApp
- `Program.cs` - Function app startup and DI configuration
- `IncomingSms.cs` - HTTP trigger for incoming SMS messages from Twilio
- `CommandInterceptor.cs` - Handles special commands (`hello ancilla`, `goodbye ancilla`)
- `CosmosPlugin.cs` - Semantic Kernel plugin exposing todo/knowledge operations to AI

#### Services (`Services/` folder)
- `ChatService.cs` - AI conversation orchestration with Semantic Kernel
- `TodoService.cs` - CRUD operations for todos in Cosmos DB
- `KnowledgeService.cs` - CRUD operations for knowledge entries
- `SessionService.cs` - User session management
- `HistoryService.cs` - Conversation history persistence
- `SmsService.cs` - Twilio SMS integration

## Dependencies & Packages

Key NuGet packages:
- `Microsoft.Azure.Functions.Worker.*` - Azure Functions isolated worker
- `Aspire.Microsoft.Azure.Cosmos` - Aspire Cosmos DB integration
- `Aspire.OpenAI` - Aspire OpenAI integration
- `Microsoft.SemanticKernel` - AI orchestration framework
- `Twilio` - SMS messaging SDK

## Configuration

### Local Development
User secrets are required in `Ancilla.AppHost`:
```bash
dotnet user-secrets set Parameters:openai-api-key "..."
dotnet user-secrets set Parameters:twilio-account-sid "..."
dotnet user-secrets set Parameters:twilio-auth-token "..."
dotnet user-secrets set Parameters:twilio-phone-number "..."
```

### Azure Deployment
- Secrets are prompted during `azd up`
- Cosmos DB uses role-based access (run `./configure_cosmos.sh` post-deployment)

## Cosmos DB Data Model

The app uses a single Cosmos DB database with containers for:
- **Sessions** - User session state (partitioned by phone number)
- **Todos** - User todo items (partitioned by user phone number)
- **Knowledge** - Knowledge entries (partitioned by user phone number)
- **History** - Conversation history (partitioned by conversation key)

## Important Patterns

### Semantic Kernel Integration
- The `ChatService` builds a Kernel instance per request
- Plugins are registered via `kernel.Plugins.AddFromObject()`
- Function calling is enabled with `FunctionChoiceBehavior.Auto()`
- Context is passed via `kernel.Data[]` dictionary

### Session Flow
1. User texts "hello ancilla" → Creates new session
2. Subsequent messages → Routed to ChatService for AI processing
3. User texts "goodbye ancilla" → Ends session

## Testing Guidelines

When writing tests:
- Use xUnit for unit tests
- Mock external services (Cosmos DB, Twilio, OpenAI)
- Test services in isolation
- Integration tests should use Cosmos DB emulator

## Common Tasks

### Adding a New Service
1. Create service class in `Ancilla.FunctionApp/Services/`
2. Register in `Program.cs` DI container
3. Inject via primary constructor where needed

### Adding a New AI Capability
1. Add method to `CosmosPlugin.cs` with `[KernelFunction]` attribute
2. Update system instructions in `ChatService.cs` if needed

### Modifying Infrastructure
1. Update `AppHost.cs` with new resources
2. Re-run `dotnet run` to regenerate Bicep in `aspire-output/`
3. Deploy with `azd up`
