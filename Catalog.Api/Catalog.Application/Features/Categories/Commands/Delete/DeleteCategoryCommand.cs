using MediatR;

namespace Catalog.Application.Features.Categories.Commands.Delete;

public record DeleteCategoryCommand(Guid Id) : IRequest<bool>;
