using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DebugGrammarCheck
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string LanguageToolApiUrl = "https://api.languagetool.org/v2/check";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Debug Grammar Check Test");
            Console.WriteLine("========================");

            var testText = "This is a test with grammer mistakes and speling erors.";
            Console.WriteLine($"Testing: \"{testText}\"");

            try
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", testText),
                    new KeyValuePair<string, string>("language", "en-US")
                });

                Console.WriteLine("\nSending request to LanguageTool API...");
                var response = await _httpClient.PostAsync(LanguageToolApiUrl, formData);
                Console.WriteLine($"Response Status: {response.StatusCode}");
                
                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"\nRaw JSON Response:");
                Console.WriteLine(jsonResponse);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<JsonDocument>(jsonResponse);
                    Console.WriteLine($"\nParsed JSON successfully. Root element: {apiResponse?.RootElement.GetRawText()}");

                    if (apiResponse?.RootElement.TryGetProperty("matches", out var matchesElement) == true)
                    {
                        Console.WriteLine($"Matches array length: {matchesElement.GetArrayLength()}");
                        foreach (var match in matchesElement.EnumerateArray())
                        {
                            if (match.TryGetProperty("message", out var messageElement))
                            {
                                Console.WriteLine($"Match message: {messageElement.GetString()}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"API Error: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}