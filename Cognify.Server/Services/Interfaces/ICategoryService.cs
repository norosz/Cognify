using Cognify.Server.Dtos.Categories;

namespace Cognify.Server.Services.Interfaces;

public interface ICategoryService
{
    Task<CategorySuggestionResponse?> SuggestModuleCategoriesAsync(Guid moduleId, int maxSuggestions);
    Task<CategorySuggestionResponse?> SuggestQuizCategoriesAsync(Guid quizId, int maxSuggestions);
    Task<bool> SetModuleCategoryAsync(Guid moduleId, string categoryLabel);
    Task<bool> SetQuizCategoryAsync(Guid quizId, string categoryLabel);
    Task<CategoryHistoryResponseDto?> GetModuleCategoryHistoryAsync(Guid moduleId, int take, Guid? cursor);
    Task<CategoryHistoryResponseDto?> GetQuizCategoryHistoryAsync(Guid quizId, int take, Guid? cursor);
}
