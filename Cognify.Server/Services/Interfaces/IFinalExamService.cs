using Cognify.Server.Dtos.Modules;
using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IFinalExamService
{
    Task<FinalExamDto?> GetFinalExamAsync(Guid moduleId);
    Task<PendingQuiz> RegenerateFinalExamAsync(Guid moduleId, RegenerateFinalExamRequest request);
    Task<FinalExamSaveResultDto> SaveFinalExamAsync(Guid moduleId, Guid pendingQuizId);
}
