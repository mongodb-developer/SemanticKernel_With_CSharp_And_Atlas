# Semantic Kernel with MongoDB Atlas

This repo is a sample console application showing how to use Semantic Kernel, Azure OpenAI and MongoDB Atlas to create a simple movie querying bot.

## Prerequisites

You will need a few things to run this locally:

1. Azure Open AI deployment for both GPT-3.5 and text-embeddings-ada-002.
2. Azure Open AI API Key
3. Azure Open AI Endpoint
4. Azure Open AI Deploy name
5. MongoDB Atlas Cluster with sample dataset loaded
6. MongoDB Atlas Connection
7. MongoDB Vector Search Index created with the name 'default'.

  *Note that this application relies on user secrets. A sample secrets.json file is available for guidance.*


## Things to know

There are a few things to note around names:

1. Your search index name inside Atlas when [creating a Vector Search Index](https://www.mongodb.com/docs/atlas/atlas-vector-search/create-index/) must be named 'default'. The default in the UI is 'vector_index' so be sure to change it at creation.
2. The field that Semantic Kernel uses for embeddings must be called 'embedding'. For this reason, there is a method in ```program.cs``` called ```FetchAndSaveMovieDocuments``` that will grab a custom number of documents from the sample_mflix database movies collection and save them to the memory store. This will generate the embeddings in a field called embedding while also saving them to a new collection in your Atlas cluster.

## Running the application

To run this application:

1. Make sure you have added your details to secrets.json via user secrets.
2. Run ```dotnet build``` using the DotNET SDK or inside an IDE such as Visual Studio.
3. Run ```dotnet run``` to run the application.