namespace FreebetTracker.Web.Services;

using FreebetTracker.Web.Models;

public interface IFreebetSuggestionService
{
    Task<FreebetSuggestions> GetSuggestionsAsync(CancellationToken cancellationToken = default);
}
