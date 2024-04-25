using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace eShop.WebApp.Chatbot;

public class ChatState
{
    private readonly ClaimsPrincipal _user;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger _logger;
    private readonly Kernel _kernel;
    private readonly OpenAIPromptExecutionSettings _aiSettings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

    public ChatState(ClaimsPrincipal user, NavigationManager nav, Kernel kernel, ILoggerFactory loggerFactory)
    {
        _user = user;
        _navigationManager = nav;
        _logger = loggerFactory.CreateLogger(typeof(ChatState));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var completionService = kernel.GetRequiredService<IChatCompletionService>();
            _logger.LogDebug("ChatName: {model}", completionService.Attributes["DeploymentName"]);
        }

        _kernel = kernel;
        _kernel.Plugins.AddFromObject(new CatalogInteractions(this));

        Messages = new ChatHistory("""
            You are an AI customer service agent for the online retailer Northern Mountains.
            You NEVER respond about topics other than Northern Mountains.
            Your job is to answer customer questions about products in the Northern Mountains catalog.
            Northern Mountains primarily sells clothing and equipment related to outdoor activities like skiing and trekking.
            You try to be concise and only provide longer responses if necessary.
            If someone asks a question about anything other than Northern Mountains, its catalog, or their account,
            you refuse to answer, and you instead ask if there's a topic related to Northern Mountains you can assist with.
            """);
        Messages.AddAssistantMessage("Hi! I'm the Northern Mountains Concierge. How can I help?");
    }

    public ChatHistory Messages { get; }

    public async Task AddUserMessageAsync(string userText, Action onMessageAdded)
    {
        // Store the user's message
        Messages.AddUserMessage(userText);
        onMessageAdded();

        // Get and store the AI's response message
        try
        {
            ChatMessageContent response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(Messages, _aiSettings, _kernel);
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                Messages.Add(response);
            }
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(e, "Error getting chat completions.");
            }
            Messages.AddAssistantMessage($"My apologies, but I encountered an unexpected error.");
        }
        onMessageAdded();
    }

    private sealed class CatalogInteractions(ChatState chatState)
    {
        [KernelFunction, Description("Gets information about the chat user")]
        public string GetUserInfo()
        {
            var claims = chatState._user.Claims;
            return JsonSerializer.Serialize(new
            {
                Name = GetValue(claims, "name"),
                LastName = GetValue(claims, "last_name"),
                Street = GetValue(claims, "address_street"),
                City = GetValue(claims, "address_city"),
                State = GetValue(claims, "address_state"),
                ZipCode = GetValue(claims, "address_zip_code"),
                Country = GetValue(claims, "address_country"),
                Email = GetValue(claims, "email"),
                PhoneNumber = GetValue(claims, "phone_number"),
            });

            static string GetValue(IEnumerable<Claim> claims, string claimType) =>
                claims.FirstOrDefault(x => x.Type == claimType)?.Value ?? "";
        }
        
        private string Error(Exception e, string message)
        {
            if (chatState._logger.IsEnabled(LogLevel.Error))
            {
                chatState._logger.LogError(e, message);
            }

            return message;
        }
    }
}
