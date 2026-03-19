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

Console.WriteLine("=== AI Age Progression Demo ===");
Console.WriteLine();

Console.Write("Zadej cestu ke vstupní fotografii: ");
string? inputPath = Console.ReadLine();

if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
{
    Console.WriteLine("Soubor neexistuje nebo cesta není platná.");
    return;
}

Console.WriteLine();
Console.WriteLine("Upravuji fotografii...");

try
{
    byte[] inputImageBytes = await File.ReadAllBytesAsync(inputPath);
    string inputFileName = Path.GetFileName(inputPath);

    /*string prompt = """
Create a photorealistic age-progressed portrait of the same person at approximately 80 years old.
Preserve the person's identity, face shape, skin tone, eye color, and overall likeness.
Keep the same framing, pose, and composition if possible.
Add only natural age-related changes such as realistic wrinkles, softer skin, slight sagging, and greying or thinning hair where plausible.
Do not change gender, ethnicity, facial expression, clothing style, or background unnecessarily.
Do not create a cartoon, painting, or stylized image.
The result must look like a believable real photograph.
""";*/

    string prompt = """
Create a photorealistic gedner change portrait of the same person.
Preserve the person's identity, skin tone, eye color, and overall likeness.
Keep the same framing, pose, and composition if possible.
Add only gender changes.
Do not create a cartoon, painting, or stylized image.
The result must look like a believable real photograph.
""";

    var options = new ImageGenerationOptions
    {
        MediaType = "image/png",
        Count = 3
    };

    ImageGenerationResponse response = await generator.EditImageAsync(
        inputImageBytes,
        inputFileName,
        prompt,
        options);

    var images = response.Contents.OfType<DataContent>().ToList();

    if (images.Count == 0)
    {
        Console.WriteLine("Služba nevrátila žádná data obrázku.");
        return;
    }

    string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    string outputFolder = Path.Combine(picturesFolder, "AiAgedImages");
    Directory.CreateDirectory(outputFolder);

    List<string> savedFiles = new();

    for (int i = 0; i < images.Count; i++)
    {
        string outputPath = Path.Combine(
            outputFolder,
            $"aged-80-{DateTime.Now:yyyyMMdd-HHmmss}-{i + 1}.png");

        await File.WriteAllBytesAsync(outputPath, images[i].Data.ToArray());
        savedFiles.Add(outputPath);
    }

    Console.WriteLine("Hotovo. Obrázky byly uloženy sem:");
    foreach (string file in savedFiles)
    {
        Console.WriteLine(file);
    }

    // Otevře první variantu
    Process.Start(new ProcessStartInfo
    {
        FileName = savedFiles[0],
        UseShellExecute = true
    });
}
catch (Exception ex)
{
    Console.WriteLine("Došlo k chybě při úpravě obrázku:");
    Console.WriteLine(ex.Message);
}

string GetRequiredSetting(string key)
{
    string? value = config[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Chybí nastavení: {key}");
    }

    return value;
}



