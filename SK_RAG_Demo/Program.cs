// See https://aka.ms/new-console-template for more information
using Microsoft.SemanticKernel;
using Kernel = Microsoft.SemanticKernel.Kernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.Extensions.Configuration;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using SK_RAG_Demo.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;

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
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
        var memory = memoryBuilder.Build();

        kernel.ImportPluginFromObject(new TextMemoryPlugin(memory));
        //await FetchAndSaveMovieDocuments(memory, 1500);

        #endregion

        AnsiConsole.MarkupLine("[bold lime]Welcome to the Movie Recommendation System![/]");
        AnsiConsole.MarkupLine("[bold lime]============================================[/]");
        Console.WriteLine("");        
        AnsiConsole.MarkupLine("[bold lime] Tell me what sort of film you want to watch..[/]");
        Console.WriteLine("");

        Console.Write("> ");

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 2000,
            Temperature = 0.7,
            TopP = 0.5
        };

        var chatFunction = kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);

        var history = "";
        var arguments = new KernelArguments()
        {
            ["history"] = history
        };      

        Console.WriteLine("Ask me about movies");

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 2000,
            Temperature = 0.7,
            TopP = 0.5
        };

        var chatFunction = kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);

        var history = "";
        var arguments = new KernelArguments()
        {
            ["history"] = history
        };      

        Console.WriteLine("Ask me about movies");
        var userInput = Console.ReadLine();
        Console.WriteLine();

        var memories = memory.SearchAsync(CollectionName, userInput, limit: 3, minRelevanceScore: 0.6);

        var table = new Table();
        table.Border = TableBorder.HeavyHead;
        table.ShowRowSeparators = true;

        table.AddColumn("[bold red]Title[/]").Centered();
        table.AddColumn("[bold green]Plot[/]").Centered();
        table.AddColumn("[bold blue]Year[/]").Centered();
        table.AddColumn("[bold yellow]Relevance (0 - 1)[/]").Centered();      


        var i = 0;
        await foreach (var mem in memories)
        {
            // Add content row 
            table.AddRow(new Text[]{
    new Text(mem.Metadata.Id).Centered(),
    new Text(mem.Metadata.Description).Centered(),
    new Text(mem.Metadata.AdditionalMetadata).Centered(),
    new Text(mem.Relevance.ToString()).Centered()});          
        }

        AnsiConsole.Write(table);
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

    private static async Task FetchAndSaveMovieDocuments(ISemanticTextMemory memory, int limitSize)
    {
        /*
         * 1. Create MongoClient with connection details
         * 2. Find any documents. Limit to 100
         * 3. For each document, save in memory using save reference
         */
        MongoClient mongoClient = new MongoClient(MongoDBAtasConnectionString);
        var movieDB = mongoClient.GetDatabase("sample_mflix");
        var movieCollection = movieDB.GetCollection<Movie>("movies");
        List<Movie> movieDocuments;

        Console.WriteLine("Fetching documents from MongoDB...");

        movieDocuments = movieCollection.Find(m => true).Limit(limitSize).ToList();

        movieDocuments.ForEach(movie =>
        {
            if (movie.Plot == null)
            {
                movie.Plot = "UNKNOWN";
            }
        });

        foreach (var movie in movieDocuments)
        {
            try
            {
                await memory.SaveReferenceAsync(
                collection: CollectionName,
                description: movie.Plot,
                text: movie.Plot,
                externalId: movie.Title,
                externalSourceName: "Sample_Mflix_Movies",
                additionalMetadata: movie.Year.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }
    }
}

