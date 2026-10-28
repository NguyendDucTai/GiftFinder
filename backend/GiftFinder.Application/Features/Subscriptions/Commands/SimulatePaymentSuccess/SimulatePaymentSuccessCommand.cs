using GiftFinder.Application.Features.Subscriptions.Commands.ProcessPaymentWebhook;
using GiftFinder.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Subscriptions.Commands.SimulatePaymentSuccess;

public class SimulatePaymentSuccessCommand : IRequest<PaymentWebhookResultDto>
{
    public Guid TransactionId { get; set; }
}

public class SimulatePaymentSuccessCommandHandler : IRequestHandler<SimulatePaymentSuccessCommand, PaymentWebhookResultDto>
{
    private readonly IMediator _mediator;

    public SimulatePaymentSuccessCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<PaymentWebhookResultDto> Handle(SimulatePaymentSuccessCommand request, CancellationToken cancellationToken)
    {
        var webhookCommand = new ProcessPaymentWebhookCommand
        {
            Payload = new PaymentWebhookPayload
            {
                TransactionId = request.TransactionId,
                Status = "PAID",
                GatewayTransactionId = $"MOCK_TEST_{Guid.NewGuid():N}"[..18]
            },
            RawJson = "{ \"mock\": true }"
        };

        return await _mediator.Send(webhookCommand, cancellationToken);
    }
}
