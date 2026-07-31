using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos.Ai;
using Linkedin.Core.Dtos.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Linkedin.Business.Services.Concrete
{
    public class AiService : IAiService
    {
        private const string GeminiEndpoint =
            "https://generativelanguage.googleapis.com/v1beta/interactions";

        private const string DefaultGeminiModel =
            "gemini-3.1-flash-lite-preview";

        private const string BioSystemInstruction =
            "You improve short profile biographies for a professional networking platform. " +
            "You are an expert LinkedIn profile writer. " +

            "LANGUAGE RULE: Detect the language of the user's input and return the rewritten text in EXACTLY the same language. " +
            "Never translate the text into English or any other language. " +
            "If the input is Azerbaijani, return Azerbaijani. " +
            "If the input is Turkish, return Turkish. " +
            "If the input is English, return English. " +

            "FACTUAL ACCURACY: Preserve the user's original meaning. " +
            "Do not invent facts, companies, education, skills, years of experience, " +
            "achievements, links, hashtags, emojis, or contact details. " +

            "WRITING STYLE: Improve grammar, clarity, readability, and professional tone. " +
            "Use clear, natural, confident, and recruiter-friendly language. " +
            "Avoid robotic wording, exaggerated claims, and generic filler. " +

            "FORMAT: Return one concise professional biography paragraph. " +
            "Do not make it much longer than the original unless needed for clarity. " +

            "OUTPUT RULE: Return only the improved biography text. " +
            "Do not include explanations, labels, quotation marks, markdown, or titles.";

        private const string PostModerationSystemInstruction =
            "You are an AI content moderation assistant for a professional networking platform. " +

            "Analyze the user's post text for platform safety and professionalism. " +

            "Flag the post only when it contains clear harmful or inappropriate content, such as: " +
            "direct insults, harassment, bullying, threats, violent intent, hate or discrimination, " +
            "sexual or inappropriate content, spam, scam, suspicious promotion, or exposure of private personal data. " +

            "Do not flag normal criticism, disagreement, negative feedback, or ordinary complaints " +
            "unless they include insults, threats, harassment, discrimination, or clearly inappropriate wording. " +

            "Be careful with false positives. If the text is only mildly informal, unclear, or critical, do not flag it. " +

            "Return JSON only. Do not include markdown, code fences, explanations, or extra text. " +

            "Use this exact JSON schema: " +
            "{ " +
            "\"isFlagged\": true or false, " +
            "\"riskLevel\": \"none\" | \"low\" | \"medium\" | \"high\", " +
            "\"categories\": [\"Harassment\", \"Threat\", \"Hate\", \"Sexual\", \"Spam\", \"PrivateData\", \"OffensiveLanguage\"], " +
            "\"reason\": \"short reason in the same language as the post\", " +
            "\"suggestedAction\": \"Published\" | \"PendingReview\" " +
            "}. " +

            "Set isFlagged=true and suggestedAction=PendingReview only for medium or high risk. " +
            "For none or low risk, use isFlagged=false and suggestedAction=Published.";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiService> _logger;
        private readonly IAiRateLimiterService _rateLimiter;

        public AiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AiService> logger,
            IAiRateLimiterService rateLimiter)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _rateLimiter = rateLimiter;
        }

        public async Task<ServiceResult> ImproveProfessionalTextAsync(
            ImproveTextRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
                return ServiceResult.Failure("Bio text is required.");

            var text = dto.Text.Trim();

            if (text.Length > 1000)
                return ServiceResult.Failure("Bio can be maximum 1000 characters.");

            var apiKey = _configuration["Gemini:BioApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini Bio API key is missing.");

                return ServiceResult.Failure("AI service is not configured.");
            }

            // Buradan sonra request cəhdi artıq limitə düşür.
            // Gemini 200 qaytarsa da, 400/404/429/500 qaytarsa da limit sayılır.
            var limitResult = await _rateLimiter.TryAcquireAsync("GeminiBio");

            if (!limitResult.Allowed)
                return ServiceResult.Failure(limitResult.Message);

            var model = _configuration["Gemini:Model"] ?? DefaultGeminiModel;

            var requestBody = new
            {
                model = model,

                store = false,

                system_instruction = BioSystemInstruction,

                input =
                    "Rewrite the following biography professionally without changing its meaning. " +
                    "Return the result in EXACTLY the same language as the original text. " +
                    "Never translate it into English or any other language. " +
                    "Do not add facts that are not already present in the original text.\n\n" +
                    "---\n" +
                    text +
                    "\n---",

                generation_config = new
                {
                    temperature = 0.2,
                    max_output_tokens = 400
                }
            };

            try
            {
                var requestJson = JsonSerializer.Serialize(requestBody);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    GeminiEndpoint);

                request.Headers.Add("x-goog-api-key", apiKey);

                request.Content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Gemini bio request failed. Status: {StatusCode}. Response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseJson);

                    return ServiceResult.Failure(
                        "AI could not improve the bio right now. Please try again.");
                }

                var improvedText = ExtractGeneratedText(responseJson);

                if (string.IsNullOrWhiteSpace(improvedText))
                {
                    _logger.LogWarning("Gemini returned an empty bio-improvement result.");

                    return ServiceResult.Failure(
                        "AI returned an empty result. Please try again.");
                }

                return ServiceResult.SuccessResult(
                    "Bio improved successfully.",
                    new ImproveTextResponseDto
                    {
                        ImprovedText = improvedText
                    });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to Gemini API for bio improvement.");

                return ServiceResult.Failure(
                    "AI service is currently unavailable. Please try again.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not parse Gemini bio response.");

                return ServiceResult.Failure(
                    "AI returned an invalid response. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected AI bio-improvement error.");

                return ServiceResult.Failure(
                    "An unexpected error occurred while improving the bio.");
            }
        }

        public async Task<ServiceResult> ModeratePostAsync(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ServiceResult.SuccessResult(
                    "Post has no text to moderate.",
                    new PostModerationResultDto
                    {
                        IsFlagged = false,
                        RiskLevel = "none",
                        Categories = new List<string>(),
                        Reason = "",
                        SuggestedAction = "Published"
                    });
            }

            text = text.Trim();

            if (text.Length > 2000)
                return ServiceResult.Failure("Post text is too long for AI moderation.");

            var apiKey = _configuration["Gemini:ModerationApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini Moderation API key is missing.");

                return ServiceResult.Failure(
                    "AI moderation service is not configured.");
            }

            // Buradan sonra post moderation cəhdi limitə düşür.
            // Gemini error versə belə 10 saniyə / 1 dəqiqə / 24 saat limiti qorunur.
            var limitResult = await _rateLimiter.TryAcquireAsync("GeminiModeration");

            if (!limitResult.Allowed)
                return ServiceResult.Failure(limitResult.Message);

            var model = _configuration["Gemini:Model"] ?? DefaultGeminiModel;

            var requestBody = new
            {
                model = model,

                store = false,

                system_instruction = PostModerationSystemInstruction,

                input =
                    "Moderate the following post text for a professional networking platform. " +
                    "Return JSON only.\n\n" +
                    "---\n" +
                    text +
                    "\n---",

                generation_config = new
                {
                    temperature = 0.0,
                    max_output_tokens = 500
                }
            };

            try
            {
                var requestJson = JsonSerializer.Serialize(requestBody);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    GeminiEndpoint);

                request.Headers.Add("x-goog-api-key", apiKey);

                request.Content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Gemini post moderation request failed. Status: {StatusCode}. Response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseJson);

                    return ServiceResult.Failure(
                        "AI could not check the post right now.");
                }

                var generatedText = ExtractGeneratedText(responseJson);

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogWarning("Gemini returned empty moderation response.");

                    return ServiceResult.Failure(
                        "AI returned an empty moderation result.");
                }

                var cleanedJson = CleanJson(generatedText);

                var moderation = JsonSerializer.Deserialize<PostModerationResultDto>(
                    cleanedJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (moderation == null)
                {
                    return ServiceResult.Failure(
                        "AI returned an invalid moderation result.");
                }

                moderation.RiskLevel = string.IsNullOrWhiteSpace(moderation.RiskLevel)
                    ? "none"
                    : moderation.RiskLevel.Trim().ToLower();

                moderation.SuggestedAction =
                    string.Equals(
                        moderation.SuggestedAction,
                        "PendingReview",
                        StringComparison.OrdinalIgnoreCase)
                        ? "PendingReview"
                        : "Published";

                moderation.IsFlagged =
                    moderation.SuggestedAction == "PendingReview" &&
                    (moderation.RiskLevel == "medium" ||
                     moderation.RiskLevel == "high");

                moderation.Categories ??= new List<string>();

                return ServiceResult.SuccessResult(
                    "Post moderation completed.",
                    moderation);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not parse Gemini moderation response.");

                return ServiceResult.Failure(
                    "AI returned an invalid moderation response.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to Gemini moderation API.");

                return ServiceResult.Failure(
                    "AI moderation service is currently unavailable.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected AI post moderation error.");

                return ServiceResult.Failure(
                    "An unexpected error occurred while checking the post.");
            }
        }

        private static string? ExtractGeneratedText(string responseJson)
        {
            using var document = JsonDocument.Parse(responseJson);

            if (!document.RootElement.TryGetProperty("steps", out var steps))
                return null;

            var output = new StringBuilder();

            foreach (var step in steps.EnumerateArray())
            {
                if (!step.TryGetProperty("type", out var stepType) ||
                    stepType.GetString() != "model_output")
                {
                    continue;
                }

                if (!step.TryGetProperty("content", out var content))
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (!part.TryGetProperty("type", out var partType) ||
                        partType.GetString() != "text")
                    {
                        continue;
                    }

                    if (part.TryGetProperty("text", out var text))
                    {
                        output.Append(text.GetString());
                    }
                }
            }

            return output.ToString().Trim();
        }

        private static string CleanJson(string text)
        {
            var cleaned = text.Trim();

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Replace("```json", "", StringComparison.OrdinalIgnoreCase);
                cleaned = cleaned.Replace("```", "");
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Replace("```", "");
            }

            cleaned = cleaned.Trim();

            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return cleaned;
        }
    }
}