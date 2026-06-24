using Application.Solicitations;
using FluentValidation;

namespace WebApi.Validation;

public sealed class CreateSolicitationRequestValidator : AbstractValidator<CreateSolicitationRequest>
{
    public CreateSolicitationRequestValidator()
    {
        RuleFor(x => x.JobName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotNull().SetValidator(new LocationDtoValidator());
        RuleFor(x => x.Website).NotNull().SetValidator(new WebsiteDtoValidator());
        RuleFor(x => x.Contact).NotNull().SetValidator(new ContactDtoValidator());
    }
}

public sealed class UpdateSolicitationRequestValidator : AbstractValidator<UpdateSolicitationRequest>
{
    public UpdateSolicitationRequestValidator()
    {
        RuleFor(x => x.JobName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Location).NotNull().SetValidator(new LocationDtoValidator());
        RuleFor(x => x.Website).NotNull().SetValidator(new WebsiteDtoValidator());
        RuleFor(x => x.Contact).NotNull().SetValidator(new ContactDtoValidator());
    }
}

public sealed class LocationDtoValidator : AbstractValidator<LocationDto>
{
    public LocationDtoValidator()
    {
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StreetNumber).NotEmpty().MaximumLength(20);
    }
}

public sealed class WebsiteDtoValidator : AbstractValidator<WebsiteDto>
{
    public WebsiteDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Link).NotEmpty().MaximumLength(500);
    }
}

public sealed class ContactDtoValidator : AbstractValidator<ContactDto>
{
    public ContactDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}