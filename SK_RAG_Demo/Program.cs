// See https://aka.ms/new-console-template for more information
using Microsoft.SemanticKernel;
using Kernel = Microsoft.SemanticKernel.Kernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SK_RAG_Demo;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050
public static partial class Program
{
    static IKernelBuilder builder;
    static IConfiguration config;
    static Kernel kernel;

    static string AzureOpenAIAPIKey;
    static string TextEmbeddingModelName;
    static string GPT35ModelName;
    static string AzureOpenAIEndpoint;
    static string AzureOpenAITextEmbeddingDeploymentName;
    static string AzureOpenAIChatCompletionDeploymentName;
    static string MongoDBAtasConnectionString;
    static string SearchIndexName;
    static string DatabaseName;
    static string CollectionName;
    static MemoryBuilder memoryBuilder;


    public static async Task Main(string[] args)
    {
        #region

        FetchUserSecrets();


        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            AzureOpenAIChatCompletionDeploymentName,
            AzureOpenAIEndpoint,
            AzureOpenAIAPIKey,            
            TextEmbeddingModelName);

        builder.AddAzureOpenAITextEmbeddingGeneration(
            AzureOpenAITextEmbeddingDeploymentName,
            AzureOpenAIEndpoint,
            AzureOpenAIAPIKey,
            TextEmbeddingModelName
            );

        builder.AddAzureOpenAITextGeneration(
            AzureOpenAITextEmbeddingDeploymentName,
            AzureOpenAIEndpoint,
            AzureOpenAIAPIKey);

        
        kernel = builder.Build();

        memoryBuilder = new MemoryBuilder();

        memoryBuilder.WithAzureOpenAITextEmbeddingGeneration(
            AzureOpenAITextEmbeddingDeploymentName,
            AzureOpenAIEndpoint,
            AzureOpenAIAPIKey,
            TextEmbeddingModelName);

        var mongoDBMemoryStore = new MongoDBMemoryStore(MongoDBAtasConnectionString, DatabaseName, SearchIndexName);

        // Swapping this line with mongoDBMemoryStore causes issues.
        // It can save documents to the collection just fine but line 114 doesn't find any results.
        memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
        var memory = memoryBuilder.Build();

        kernel.ImportPluginFromObject(new TextMemoryPlugin(memory));

        #endregion       

        var githubFiles = new Dictionary<string, string>()
        {
            ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
        = "README: Installation, getting started, and how to contribute",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
        = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/00-getting-started.ipynb"]
        = "Jupyter notebook describing how to get started with the Semantic Kernel",
            ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
        = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Memory/VolatileMemoryStore.cs"]
        = "C# class that defines a volatile embedding store",
        };       

        Console.WriteLine("Adding some GitHub file URLs and their descriptions to Chroma Semantic Memory.");
        var i = 0;
        foreach (var entry in githubFiles)
        {
            await memory.SaveReferenceAsync(
                collection: CollectionName,
                description: entry.Value,
                text: entry.Value,
                externalId: entry.Key,
                externalSourceName: "GitHub"
            );
            Console.WriteLine($"  URL {++i} saved");
        }

        string ask = "I love Jupyter notebooks, how should I get started?";
        Console.WriteLine("===========================\n" +
            "Query: " + ask + "\n");

        var memories = memory.SearchAsync(CollectionName, ask, limit: 5, minRelevanceScore: 0.6);
               
        i = 0;
        await foreach(var mem in memories)
        {
            Console.WriteLine($"Result {++i}:");
            Console.WriteLine("  URL:     : " + mem.Metadata.Id);
            Console.WriteLine("  Title    : " + mem.Metadata.Description);
            Console.WriteLine("  Relevance: " + mem.Relevance);
            Console.WriteLine();
        }

        Console.WriteLine("Press any key to continue..");
        Console.ReadLine();
    }

    private static void FetchUserSecrets()
    {
        // Get user secrets
        config = new ConfigurationBuilder()
            .AddUserSecrets<Settings>()
            .Build();
        TextEmbeddingModelName = config.GetValue<string>("TextEmbeddingModelName");
        AzureOpenAIEndpoint = config.GetValue<string>("AzureOpenAIEndpoint");
        AzureOpenAIAPIKey = config.GetValue<string>("AzureOpenAIAPIKey");
        GPT35ModelName = config.GetValue<string>("GPT35ModelName");
        AzureOpenAITextEmbeddingDeploymentName = config.GetValue<string>("AzureOpenAITextEmbeddingDeploymentName");
        AzureOpenAIChatCompletionDeploymentName = config.GetValue<string>("AzureOpenAIChatCompletionDeploymentName");
        MongoDBAtasConnectionString = config.GetValue<string>("MongoDBAtasConnectionString");
        SearchIndexName = config.GetValue<string>("SearchIndexName");
        DatabaseName = config.GetValue<string>("DatabaseName");
        CollectionName = config.GetValue<string>("CollectionName");
    }

}