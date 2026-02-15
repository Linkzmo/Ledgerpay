using FluentValidation;
using Payments.Api.Contracts;

namespace Payments.Api.Validators;

public sealed class CreatePaymentIntentRequestValidator : AbstractValidator<CreatePaymentIntentRequest>
{
    public CreatePaymentIntentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PayerId).NotEmpty().MaximumLength(80);
        RuleFor(x => x.MerchantId).NotEmpty().MaximumLength(80);
    }
}
