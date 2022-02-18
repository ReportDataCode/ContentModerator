// See https://aka.ms/new-console-template for more information

using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using Newtonsoft.Json;
using System.Text;

// Proof of Content to demonstrate for content moderator solution.

static ContentModeratorClient Authenticate(string key, string endpoint)
{
    ContentModeratorClient client = new(new ApiKeyServiceClientCredentials(key));
    client.Endpoint = endpoint;

    return client;
}

static void ModerateText(ContentModeratorClient client, string inputFile, string outputFile)
{
    Console.WriteLine("--------------------------------------------------------------");
    Console.WriteLine();
    Console.WriteLine("TEXT MODERATION");
    Console.WriteLine();

    // Load the input text.
    string text = File.ReadAllText(inputFile);

    // Remove carriage returns
    text = text.Replace(Environment.NewLine, " ");
    // Convert string to a byte[], then into a stream (for parameter in ScreenText()).
    byte[] textBytes = Encoding.UTF8.GetBytes(text);
    MemoryStream stream = new(textBytes);

    Console.WriteLine("Screening {0}...", inputFile);
    // Format text

    // Save the moderation results to a file.
    using (StreamWriter outputWriter = new(outputFile, false))
    {
        using (client)
        {
            // Screen the input text: check for profanity, classify the text into three categories,
            // do auto-correct text, and check for personally identifying information (PII)
            outputWriter.WriteLine("Auto-correct typos, check for matching terms, PII, and classify.");

            // Moderate the text
            var screenResult = client.TextModeration.ScreenText("text/plain", stream, "eng", true, true, null, true);
            outputWriter.WriteLine(JsonConvert.SerializeObject(screenResult, Formatting.Indented));
        }

        outputWriter.Flush();
        outputWriter.Close();
    }

    Console.WriteLine("Results written to {0}", outputFile);
    Console.WriteLine();
}


/*
 * IMAGE MODERATION
 * This example moderates images from URLs.
 */

static void ModerateImages(ContentModeratorClient client, string urlFile, string outputFile)
{
    Console.WriteLine("--------------------------------------------------------------");
    Console.WriteLine();
    Console.WriteLine("IMAGE MODERATION");
    Console.WriteLine();
    // Create an object to store the image moderation results.
    List<EvaluationData> evaluationData = new();

    using (client)
    {
        // Read image URLs from the input file and evaluate each one.
        using StreamReader inputReader = new(urlFile);
        while (!inputReader.EndOfStream)
        {
            string line = inputReader.ReadLine()!.Trim();
            if (line != String.Empty)
            {
                Console.WriteLine("Evaluating {0}...", Path.GetFileName(line));
                var imageUrl = new BodyModel("URL", line.Trim());
                var imageData = new EvaluationData
                {
                    ImageUrl = imageUrl.Value,

                    // Evaluate for adult and racy content.
                    ImageModerationEvaluate =
                        client.ImageModeration.EvaluateUrlInput("application/json", imageUrl, true)
                };
                Thread.Sleep(1000);

                // Detect and extract text.
                imageData.TextDetectionOcr =
                    client.ImageModeration.OCRUrlInput("eng", "application/json", imageUrl, true);
                Thread.Sleep(1000);

                // Detect faces.
                imageData.FaceDetectionFaces =
                    client.ImageModeration.FindFacesUrlInput("application/json", imageUrl, true);
                Thread.Sleep(1000);

                // Add results to Evaluation object
                evaluationData.Add(imageData);
            }

            // Save the moderation results to a file.
            using (StreamWriter outputWriter = new(outputFile, false))
            {
                outputWriter.WriteLine(JsonConvert.SerializeObject(
                    evaluationData, Formatting.Indented));

                outputWriter.Flush();
                outputWriter.Close();
            }

            Console.WriteLine();
            Console.WriteLine("Image moderation results written to output file: " + outputFile);
            Console.WriteLine();
        }
    }
}


// Name of the file that contains text.
string TextFile = "<name-of-the-text-to-be-moderated.txt";

// The name of the file to contain the output from the evaluation.
string TextOutputFile = "TextModerationOutput.json";


// Text files that contains the list of URLS that you want to monitor for potentially risky or offensive content.
string ImageUrlFile = "<name-of-the-file-that-lists-out-URLS-images-to-be-moderated-.txt";

//The name of the file you want to use to represent the structured data from the Content Moderator AI service,
string ImageOutputFile = "image-moderator-output.json";

// Key and Endpoint
const string subscriptionKey = "-key";
const string endpoint = "-endpoint";

// Authenticate clients for the Text and Image Content Moderation.
ContentModeratorClient clientImageClient = Authenticate(subscriptionKey, endpoint);
ContentModeratorClient clientTextClient = Authenticate(subscriptionKey, endpoint);

ModerateImages(clientImageClient, ImageUrlFile, ImageOutputFile);

ModerateText(clientTextClient, TextFile, TextOutputFile);

class EvaluationData
{
    public string? ImageUrl;

    public Evaluate? ImageModerationEvaluate;

    public OCR? TextDetectionOcr;

    public FoundFaces? FaceDetectionFaces;
}