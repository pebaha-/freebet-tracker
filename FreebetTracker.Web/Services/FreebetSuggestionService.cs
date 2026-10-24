using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public sealed class FreebetSuggestionService(IFreebetRepository repository) : IFreebetSuggestionService
{
    public async Task<FreebetSuggestions> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var freebets = await repository.GetAllAsync(cancellationToken);

        var playerSuggestions = freebets
            .Select(f => f.Player)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accountSuggestions = freebets
            .Select(f => f.Account)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new FreebetSuggestions(playerSuggestions, accountSuggestions);
    }
}
