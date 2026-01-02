# Ancela

Ancela is an AI assistant that helps users manage their todos, calendar, emails, and contacts through SMS messaging. Built with .NET Aspire, Azure Functions, Azure Cosmos DB, and Microsoft Graph, it provides an intelligent, conversational interface for managing personal information and productivity.

## Features

- 💬 **SMS Interface** - Interact with your AI assistant via text messages using Twilio
- 🧠 **AI-Powered Conversations** - Powered by OpenAI GPT models with Semantic Kernel
- 📝 **Todo Management** - Save, retrieve, and delete todos using natural language
- � **Calendar Access** - Query your upcoming events and appointments
- 📧 **Email Integration** - Read recent emails and send new emails
- 👥 **Contact Management** - Access and search your contacts
- �🔐 **Session Management** - Secure, per-user sessions with simple commands
- 📜 **Conversation History** - Maintains context across multiple interactions
- ☁️ **Cloud Native** - Built with .NET Aspire for easy deployment to Azure
- 🗄️ **Scalable Storage** - Azure Cosmos DB for reliable, distributed data storage

## Architecture

Ancela is built using modern cloud-native patterns:

- **Ancela.AppHost** - .NET Aspire orchestration and infrastructure
- **Ancela.FunctionApp** - Azure Functions for serverless HTTP endpoints
- **Ancela.ServiceDefaults** - Shared service configuration and defaults

### Key Components

- **CommandInterceptor** - Handles special commands (`hello ancela`, `goodbye ancela`)
- **ChatService** - AI conversation management with OpenAI integration
- **TodoService** - CRUD operations for user todos in Cosmos DB
- **KnowledgeService** - Manages persistent knowledge entries
- **GraphService** - Microsoft Graph API integration for calendar, email, and contacts
- **GraphPlugin** - Semantic Kernel plugin exposing Graph capabilities to AI
- **SessionService** - User session lifecycle management
- **HistoryService** - Conversation history persistence and retrieval
- **SmsService** - Twilio integration for SMS messaging

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Azure subscription](https://azure.microsoft.com/free/) (for deployment)
- [Twilio account](https://www.twilio.com/) (for SMS functionality)
- [OpenAI API key](https://platform.openai.com/) or Azure OpenAI service

## Getting Started

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/tombly/ancela.git
   cd ancela
   ```
2. **Configure Graph Access**

- Navigate to your organization's Entra admin center [https://aad.portal.azure.com/] and login with a Global administrator account.

- Select a directory and then select App registrations under Manage.

- Select New registration. Enter a name for your application, for example, Ancela AI.

- Set Supported account types to Accounts in this organizational directory only.

- Leave Redirect URI empty.

- Select Register. On the application's Overview page, copy the value of the Application (client) ID and Directory (tenant) ID and save them, you will need these values in the next step.

- Select API permissions under Manage.

- Remove the default User.Read permission under Configured permissions by selecting the ellipses (...) in its row and selecting Remove permission.

- Perform the following steps for each of the permissions `User.Read.All`, `Calendars.ReadWrite`, `Contacts.Read`, `Mail.Read`, and `Mail.Send`:

  - Select Add a permission, then Microsoft Graph.

  - Select Application permissions.

  - Select User.Read.All, then select Add permissions.

- Select Grant admin consent for..., then select Yes to provide admin consent for the selected permission.

- Select Certificates and secrets under Manage, then select New client secret.

- Enter a description, choose a duration, and select Add.

- Copy the secret from the Value column, you will need it in the next step.

3. **Configure user secrets**
   
   Navigate to the AppHost project folder and set your secrets:
   ```bash
   cd Ancela.AppHost
   dotnet user-secrets set Parameters:openai-api-key "your-openai-api-key"
   dotnet user-secrets set Parameters:twilio-account-sid "your-twilio-account-sid"
   dotnet user-secrets set Parameters:twilio-auth-token "your-twilio-auth-token"
   dotnet user-secrets set Parameters:twilio-phone-number "your-twilio-phone-number"
   dotnet user-secrets set Parameters:graph-user-id "your-entra-user-id"
   dotnet user-secrets set Parameters:graph-tenant-id "your-entra-tenant-id"
   dotnet user-secrets set Parameters:graph-client-id "your-graph-app-client-id"
   dotnet user-secrets set Parameters:graph-client-secret "your-graph-app-client-secret"
   dotnet user-secrets set Parameters:ynab-access-token "your-ynab-access-token"
   ```

4. **Update the resource group name**
   
   Edit `FixedNameInfrastructureResolver.cs` and`configure_cosmos.sh` to set your resource group name and cosmos resource name.

5. **Run locally**
   ```bash
   dotnet run --project Ancela.AppHost
   ```

   The Aspire dashboard will launch, showing all running services and their endpoints.

### Deployment to Azure

1. **Configure infrastructure**
   
   The project uses .NET Aspire for infrastructure-as-code. Bicep templates generated by Aspire are generated in `Ancela.AppHost/aspire-output/`.

2. **Deploy using Aspire**
   ```bash
   azd init
   azd up
   ```

   Aspire will prompt you for required secrets during deployment. You can easily grab them from the user secrets:
   ```bash
   cd Ancela.AppHost
   dotnet user-secrets list | grep Parameters
   ```

3. **Post-deployment configuration**

   After deploying, grant your Entra principal the Contributor role for Cosmos DB so you can access Data Explorer and create the Cosmos DB database and containers:
   ```bash
   ./configure_cosmos.sh
   ```

## Usage

### Starting a Session

Send an SMS to your Twilio number:
```
hello ancela
```

### Knowledge

```
Remember that my favorite color is blue
```

### Todos

```
Save a todo: buy milk, eggs, and bread
List my todos
Remove the grocery list
```

### Calendar

```
What's on my calendar today?
Do I have any meetings tomorrow?
Create a calendar event for lunch with Sarah next Friday at 1 PM
```

### Emails

```
Do I have any new emails?
Email Sarah and let her know the meeting is rescheduled
```

### Contacts

```
What's John's email address?
```

### Ending a Session

```
goodbye ancela
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Made with ❤️ using .NET Aspire**
