using BlazorWebApp.Client.Models;
using System.Text.RegularExpressions;

namespace BlazorWebApp.Client.Services
{
    public interface IQuestionDetectionService
    {
        Task<List<string>> DetectQuestionsAsync(string text);
        bool IsQuestion(string sentence);
    }

    public class QuestionDetectionService : IQuestionDetectionService
    {
        private readonly List<string> _questionStarters = new()
        {
            "what", "how", "when", "where", "why", "who", "which", "whose", "whom",
            "can", "could", "would", "should", "will", "shall", "may", "might",
            "do", "does", "did", "have", "has", "had", "are", "is", "was", "were",
            "please describe", "please explain", "please provide", "please list",
            "describe", "explain", "provide", "list", "identify", "specify",
            "outline", "detail", "clarify", "elaborate"
        };

        private readonly List<string> _rfpKeywords = new()
        {
            "requirement", "requirements", "specification", "specifications",
            "proposal", "solution", "approach", "methodology", "timeline",
            "cost", "pricing", "budget", "experience", "qualification",
            "capability", "deliverable", "scope", "objective", "goal"
        };

        public async Task<List<string>> DetectQuestionsAsync(string text)
        {
            return await Task.Run(() =>
            {
                var questions = new List<string>();
                
                if (string.IsNullOrWhiteSpace(text))
                    return questions;

                // Clean up the text
                text = Regex.Replace(text, @"\s+", " ").Trim();
                
                // Split into sentences
                var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();

                foreach (var sentence in sentences)
                {
                    if (IsQuestion(sentence))
                    {
                        questions.Add(sentence);
                    }
                }

                // Also look for numbered or bulleted requirements that might be implicit questions
                var implicitQuestions = ExtractImplicitQuestions(text);
                questions.AddRange(implicitQuestions);

                return questions.Distinct().ToList();
            });
        }

        public bool IsQuestion(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return false;

            var cleanSentence = sentence.Trim().ToLowerInvariant();
            
            // Direct question indicators
            if (cleanSentence.EndsWith("?"))
                return true;

            // Check for question starters
            foreach (var starter in _questionStarters)
            {
                if (cleanSentence.StartsWith(starter + " ") || 
                    cleanSentence.StartsWith(starter + "'"))
                {
                    return true;
                }
            }

            // Check for imperative statements that are actually requests (common in RFPs)
            if (ContainsRfpKeywords(cleanSentence) && 
                (cleanSentence.StartsWith("describe ") ||
                 cleanSentence.StartsWith("explain ") ||
                 cleanSentence.StartsWith("provide ") ||
                 cleanSentence.StartsWith("list ") ||
                 cleanSentence.StartsWith("outline ") ||
                 cleanSentence.StartsWith("detail ") ||
                 cleanSentence.StartsWith("specify ") ||
                 cleanSentence.StartsWith("identify ")))
            {
                return true;
            }

            return false;
        }

        private bool ContainsRfpKeywords(string sentence)
        {
            return _rfpKeywords.Any(keyword => sentence.Contains(keyword));
        }

        private List<string> ExtractImplicitQuestions(string text)
        {
            var implicitQuestions = new List<string>();
            
            // Look for numbered requirements/sections
            var numberedPattern = @"(?:^|\n)\s*(\d+\.?\s+.*?)(?=\n\s*\d+\.|\n\s*[A-Z]|\n\s*$|$)";
            var numberedMatches = Regex.Matches(text, numberedPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            
            foreach (Match match in numberedMatches)
            {
                var requirement = match.Groups[1].Value.Trim();
                if (ContainsRfpKeywords(requirement.ToLowerInvariant()) && requirement.Length > 20)
                {
                    // Convert requirement statements to questions
                    var question = ConvertRequirementToQuestion(requirement);
                    if (!string.IsNullOrEmpty(question))
                    {
                        implicitQuestions.Add(question);
                    }
                }
            }

            // Look for bulleted requirements
            var bulletPattern = @"(?:^|\n)\s*[•\-\*]\s+(.*?)(?=\n\s*[•\-\*]|\n\s*[A-Z]|\n\s*$|$)";
            var bulletMatches = Regex.Matches(text, bulletPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            
            foreach (Match match in bulletMatches)
            {
                var requirement = match.Groups[1].Value.Trim();
                if (ContainsRfpKeywords(requirement.ToLowerInvariant()) && requirement.Length > 20)
                {
                    var question = ConvertRequirementToQuestion(requirement);
                    if (!string.IsNullOrEmpty(question))
                    {
                        implicitQuestions.Add(question);
                    }
                }
            }

            return implicitQuestions;
        }

        private string ConvertRequirementToQuestion(string requirement)
        {
            var cleaned = requirement.Trim();
            
            // Remove leading numbers or bullets
            cleaned = Regex.Replace(cleaned, @"^\d+\.?\s*", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^[•\-\*]\s*", "").Trim();
            
            if (string.IsNullOrEmpty(cleaned) || cleaned.Length < 10)
                return string.Empty;

            // Convert imperative statements to questions
            if (cleaned.ToLowerInvariant().StartsWith("provide "))
            {
                return "What " + cleaned.Substring(8).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("describe "))
            {
                return "How would you describe " + cleaned.Substring(9).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("explain "))
            {
                return "How would you explain " + cleaned.Substring(8).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("list "))
            {
                return "What are " + cleaned.Substring(5).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("outline "))
            {
                return "How would you outline " + cleaned.Substring(8).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("detail "))
            {
                return "What are the details of " + cleaned.Substring(7).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("specify "))
            {
                return "What would you specify for " + cleaned.Substring(8).ToLowerInvariant() + "?";
            }
            else if (cleaned.ToLowerInvariant().StartsWith("identify "))
            {
                return "What would you identify as " + cleaned.Substring(9).ToLowerInvariant() + "?";
            }
            else
            {
                // For other requirements, create a generic question
                return "How do you address this requirement: " + cleaned + "?";
            }
        }
    }
}