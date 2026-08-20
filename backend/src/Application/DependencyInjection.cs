using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Application.KnowledgeBase.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
        services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton<IKnowledgeDocumentChunker, MarkdownKnowledgeDocumentChunker>();
        services.AddScoped<IKnowledgeBaseSearchService, KnowledgeBaseSearchService>();

        return services;
    }
}