using FluentValidation;
using Payments.Api.Contracts;

namespace Payments.Api.Validators;

public sealed class ReversePaymentRequestValidator : AbstractValidator<ReversePaymentRequest>
{
    public ReversePaymentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}
