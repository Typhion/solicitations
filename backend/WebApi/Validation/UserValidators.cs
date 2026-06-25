using Application.Common;
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
