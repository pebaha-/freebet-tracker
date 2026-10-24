namespace FreebetTracker.Web.Models;

public sealed record FreebetSuggestions(IReadOnlyList<string> PlayerSuggestions, IReadOnlyList<string> AccountSuggestions);
