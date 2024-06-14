using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;
using SemanticKernel_CSharp_Atlas.Models;
using Spectre.Console;
using Kernel = Microsoft.SemanticKernel.Kernel;

namespace SemanticKernel_CSharp_Atlas;

// Features are still experimental
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
    static string MongoDBAtlasConnectionString;
    static string SearchIndexName;
    static string DatabaseName;
    static string CollectionName;
    static MemoryBuilder memoryBuilder;


    public static async Task Main(string[] args)
    {
        #region

        FetchUserSecrets();
        
        memoryBuilder = new MemoryBuilder();

        memoryBuilder.WithAzureOpenAITextEmbeddingGeneration(
            AzureOpenAITextEmbeddingDeploymentName,
            AzureOpenAIEndpoint,
            AzureOpenAIAPIKey,
            TextEmbeddingModelName);
              

        var mongoDBMemoryStore = new MongoDBMemoryStore(MongoDBAtlasConnectionString, DatabaseName, SearchIndexName);
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);       
        var memory = memoryBuilder.Build();
      
        //await FetchAndSaveMovieDocuments(memory, 1500);

        #endregion

        var interactions = new List<string>();

        AnsiConsole.MarkupLine("[bold lime]Welcome to the Movie Recommendation System![/]");
        AnsiConsole.MarkupLine("[bold white]Type 'x' and press Enter to exit.[/]");
        AnsiConsole.MarkupLine("[bold lime]============================================[/]");
        Console.WriteLine("");        
        
        
        while(true)
        {
            AnsiConsole.MarkupLine("[bold lime] Tell me what sort of film you want to watch..[/]");
            Console.WriteLine("");

            Console.Write("> ");

            var userInput = Console.ReadLine();

            if(userInput.ToLower() == "x")
            {
                Console.WriteLine("Exiting application..");
                break;
            }

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
            
        }
       
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
        MongoDBAtlasConnectionString = config.GetValue<string>("MongoDBAtlasConnectionString");
        SearchIndexName = config.GetValue<string>("SearchIndexName");
        DatabaseName = config.GetValue<string>("DatabaseName");
        CollectionName = config.GetValue<string>("CollectionName");
    }

    private static async Task FetchAndSaveMovieDocuments(ISemanticTextMemory memory, int limitSize)
    {
        /*
         * 1. Create MongoClient with connection details
         * 2. Find any documents. Limit to limitSize parameter
         * 3. For each document, save in memory using save reference
         */
        MongoClient mongoClient = new MongoClient(MongoDBAtlasConnectionString);
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

