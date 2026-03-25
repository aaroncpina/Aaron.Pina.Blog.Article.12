using static System.StringSplitOptions;

namespace Aaron.Pina.Blog.Article._12.Server;

public static class ScopeParser
{
    public static bool TryExtractValues(string scope, out string audience, out string[] scopes)
    {
        scopes = [];
        audience = string.Empty;
        if (string.IsNullOrEmpty(scope)) return false;
        scopes = scope.Split(' ', RemoveEmptyEntries | TrimEntries);
        var audiences = scopes.Select(s => s.Split('.', 2))
                              .Where(p => p.Length == 2)
                              .Select(p => p.First())
                              .Distinct(StringComparer.Ordinal)
                              .ToList();
        if (audiences.Count != 1) return false;
        audience = audiences.Single();
        return true;
    }
    
    public static string[] ExtractScopes(string scope) =>
        scope.Split(' ', RemoveEmptyEntries | TrimEntries);
}
