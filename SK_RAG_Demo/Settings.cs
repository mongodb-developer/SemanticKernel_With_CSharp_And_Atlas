// See https://aka.ms/new-console-template for more information
namespace SK_RAG_Demo;
public class Settings
{
    public string TextEmbeddingModelName { get; set; }
    public string GPT35ModelName { get; set; }
    public string AzureOpenAIEndpoint { get; set; }
    public string AzureOpenAIApiKey { get; set; }

    public string AzureOpenAITextEmbeddingDeploymentName { get; set; }

    public string AzureOpenAIChatCompletionDeploymentName { get; set; }
    public string MongoDBAtlasConnectionstring { get; set; }
public string SearchIndexName { get; set; }

    public string DatabaseName { get; set; }

    public string CollectionName { get; set; }

}