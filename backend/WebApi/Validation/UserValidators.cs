using Application.Common;
using Application.Users;
using FluentValidation;
using Microsoft.Extensions.Localization;
using WebApi.Api;

namespace WebApi.Validation;

public sealed class GrantRoleRequestValidator : AbstractValidator<GrantRoleRequest>
{
    public GrantRoleRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(Roles.Assignable.Contains)
            .WithMessage(_ => localizer["Role must be one of: {0}.", string.Join(", ", Roles.Assignable)]);
    }
}

public sealed class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
    }
}
