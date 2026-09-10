using System.Collections.Concurrent;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Specs.Models;

namespace SFA.DAS.Payments.EarningEvents.Specs.Handlers;

public class GSLApprenticeshipEarningsEventHandler : IHandleMessages<GSLApprenticeshipEarningsEvent>
{
    public static ConcurrentBag<GSLApprenticeshipEarningsEvent> ReceivedEvents { get; } =
        new ConcurrentBag<GSLApprenticeshipEarningsEvent>();

    public async Task Handle(GSLApprenticeshipEarningsEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Received apprenticeship earnings event: {message.Ukprn}, uln: {message.Learner.Uln}, return: {message.CollectionPeriod.AcademicYear}-{message.CollectionPeriod.Period}, Course: {message.LearningAim.LearningType} - {message.LearningAim.CourseCode}");
        ReceivedEvents.Add(message);
    }

    public static IEnumerable<GSLApprenticeshipEarningsEvent> GetEvents(Learner learner) => ReceivedEvents.Where(receivedEvent =>
        receivedEvent.Learner.Uln == learner.Uln
        && receivedEvent.Ukprn == learner.Ukprn
        && receivedEvent.Learner.ReferenceNumber == learner.LearnRefNumber);
}
