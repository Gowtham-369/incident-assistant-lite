using System.Text.RegularExpressions;
using IncidentAssistant.Metadata.Responses;

namespace IncidentAssistant.Api.Rag;

public static partial class QueryText
{
    private const int MaxLength = 1000;

    public static string FromIncident(IncidentDto incident)
    {
        var parts = new List<string> { incident.Title, incident.Description };
        if (incident.ErrorSignatures.Count > 0)
            parts.Add("Error signatures: " + string.Join("; ", incident.ErrorSignatures));

        var text = string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    // Strip characters that the simple query syntax treats as operators: + | - " * ( ) ~ \ and friends
    public static string ForKeywordSearch(string text) => Operators().Replace(text, " ");

    [GeneratedRegex(@"[+\-&|!(){}\[\]^""~*?:\\/]")]
    private static partial Regex Operators();
}