using System.ComponentModel;
using Ancilla.FunctionApp.Services;
using Microsoft.SemanticKernel;

namespace Ancilla.FunctionApp;

public class CosmosPlugin(TodoService _todoService)
{
    [KernelFunction("save_todo")]
    [Description("Saves a todo to the database")]
    public async Task SaveTodoAsync(Kernel kernel, string content)
    {
        var agentPhoneNumber = kernel.Data["agentPhoneNumber"]?.ToString()!;
        var userPhoneNumber = kernel.Data["userPhoneNumber"]?.ToString()!;
        await _todoService.SaveTodoAsync(agentPhoneNumber, userPhoneNumber, content);
    }

    [KernelFunction("get_todos")]
    [Description("Retrieves todos from the database for the current agent")]
    public async Task<TodoEntry[]> GetTodosAsync(Kernel kernel)
    {
        var agentPhoneNumber = kernel.Data["agentPhoneNumber"]?.ToString()!;
        return await _todoService.GetTodosAsync(agentPhoneNumber);
    }

    [KernelFunction("delete_todo")]
    [Description("Deletes a todo from the database given its ID which is a GUID. Use the get_todos function to retrieve todo IDs.")]
    public async Task DeleteTodoAsync(Kernel kernel, Guid id)
    {
        var agentPhoneNumber = kernel.Data["agentPhoneNumber"]?.ToString()!;
        await _todoService.DeleteTodoAsync(id, agentPhoneNumber);
    }
}