using Cognify.Server.Data;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using OpenAI;

namespace Cognify.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.AddSqlServerDbContext<ApplicationDbContext>("sqldata");
        
        // Configure Azure Blob Storage with credentials for SAS token generation
        // When using Azurite emulator, this will use the default connection string
        // which includes the account key necessary for GenerateSasUri()
        builder.AddAzureBlobServiceClient("blobs");

        // Register Application Services
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IModuleService, ModuleService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContextService, UserContextService>();
        builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<INoteService, NoteService>();
        builder.Services.AddScoped<IDecayPredictionService, DecayPredictionService>();
        builder.Services.AddScoped<IMistakeAnalysisService, MistakeAnalysisService>();
        builder.Services.AddScoped<IKnowledgeStateService, KnowledgeStateService>();
        builder.Services.AddScoped<ILearningAnalyticsComputationService, LearningAnalyticsComputationService>();
        builder.Services.AddScoped<ILearningAnalyticsService, LearningAnalyticsService>();
        builder.Services.AddScoped<IAdaptiveQuizService, AdaptiveQuizService>();
        
        // Register AI
        var openAiKey = builder.Configuration["OpenAI:ApiKey"];
        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddScoped<IAiService, NullAiService>();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(openAiKey))
            {
                throw new InvalidOperationException("OpenAI API Key is missing");
            }

            builder.Services.AddSingleton(_ => new OpenAIClient(openAiKey));
            builder.Services.AddScoped<IAiService, AiService>();
        }
        builder.Services.AddScoped<IQuizService, QuizService>();
        builder.Services.AddScoped<IAttemptService, AttemptService>();
        builder.Services.AddScoped<IExamAttemptService, ExamAttemptService>();
        builder.Services.AddScoped<IAttemptReviewService, AttemptReviewService>();
        builder.Services.AddScoped<IStatsService, StatsService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IConceptClusteringService, ConceptClusteringService>();
        builder.Services.AddScoped<IExtractedContentService, ExtractedContentService>();
        builder.Services.AddScoped<IPendingQuizService, PendingQuizService>();
        builder.Services.AddScoped<IFinalExamService, FinalExamService>();
        builder.Services.AddScoped<IAgentRunService, AgentRunService>();
        builder.Services.AddScoped<IMaterialService, MaterialService>();
        builder.Services.AddScoped<IMaterialExtractionService, MaterialExtractionService>();
        builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
        builder.Services.AddSingleton<IPdfImageExtractor, PdfImageExtractor>();
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<AiBackgroundWorker>();
        }
        builder.Services.AddHostedService<LearningAnalyticsBackgroundWorker>();

        // Register Authentication
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                };
            });

        builder.Services.AddAuthorization();
        
        // Enable CORS for frontend development
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options => 
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }
        }

        app.UseDefaultFiles();
        app.MapStaticAssets();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        // Add global exception handler to catch errors before crash
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                if (exceptionFeature != null)
                {
                    logger.LogError(exceptionFeature.Error, "Unhandled exception occurred: {Message}", exceptionFeature.Error.Message);
                }
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred processing your request.",
                    Detail = "An unexpected error occurred. Please try again later."
                };
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        app.UseAuthentication();
        app.UseCors("AllowFrontend");
        app.UseAuthorization();


        app.MapControllers();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
