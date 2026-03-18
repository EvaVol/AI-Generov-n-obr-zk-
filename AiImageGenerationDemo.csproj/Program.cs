using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string endpoint = GetRequiredSetting("AZURE_OPENAI_ENDPOINT");
string apiKey = GetRequiredSetting("AZURE_OPENAI_API_KEY");
string deploymentName = GetRequiredSetting("AZURE_OPENAI_IMAGE_DEPLOYMENT");

AzureOpenAIClient azureClient = new(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey));

var imageClient = azureClient.GetImageClient(deploymentName);

#pragma warning disable MEAI001
IImageGenerator generator = imageClient.AsIImageGenerator();
#pragma warning restore MEAI001

Console.WriteLine("=== AI Image Generation Demo ===");
Console.WriteLine();
Console.Write("Zadej textový prompt: ");
string? prompt = Console.ReadLine();

if (string.IsNullOrWhiteSpace(prompt))
{
    Console.WriteLine("Prompt nesmí být prázdný.");
    return;
}

Console.WriteLine();
Console.WriteLine("Generuji obrázek...");

try
{
    var options = new ImageGenerationOptions
    {
        MediaType = "image/png"
    };

    ImageGenerationResponse response = await generator.GenerateImagesAsync(prompt, options);

    DataContent? imageData = response.Contents.OfType<DataContent>().FirstOrDefault();

    if (imageData is null)
    {
        Console.WriteLine("Služba nevrátila žádná binární data obrázku.");
        return;
    }

    string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    string outputFolder = Path.Combine(picturesFolder, "AiGeneratedImages");
    Directory.CreateDirectory(outputFolder);

    string safeFileName = $"image-{DateTime.Now:yyyyMMdd-HHmmss}.png";
    string outputPath = Path.Combine(outputFolder, safeFileName);

    await File.WriteAllBytesAsync(outputPath, imageData.Data.ToArray());

    Console.WriteLine($"Hotovo. Obrázek byl uložen sem:");
    Console.WriteLine(outputPath);

    Process.Start(new ProcessStartInfo
    {
        FileName = outputPath,
        UseShellExecute = true
    });
}
catch (Exception ex)
{
    Console.WriteLine("Došlo k chybě při generování obrázku:");
    Console.WriteLine(ex.Message);
}

string GetRequiredSetting(string key)
{
    string? value = config[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Chybí nastavení '{key}'. Přidej ho přes dotnet user-secrets.");
    }

    return value;
} 