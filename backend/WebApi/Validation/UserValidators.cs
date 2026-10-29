using Application.Common;
using Application.Users;
using FluentValidation;
using WebApi.Api;

namespace WebApi.Validation;

public sealed class GrantRoleRequestValidator : AbstractValidator<GrantRoleRequest>
{
    public GrantRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(Roles.Assignable.Contains)
            .WithMessage("Role must be one of: " + string.Join(", ", Roles.Assignable));
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
