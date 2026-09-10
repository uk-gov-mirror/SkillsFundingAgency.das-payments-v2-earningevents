using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Reqnroll;
using SFA.DAS.Payments.EarningEvents.Data;
using SFA.DAS.Payments.EarningEvents.Messages;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Specs.Handlers;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.OnProgramme;
using UUIDNext;
using CourseType = SFA.DAS.Payments.EarningEvents.Messages.External.CourseType;
using EarningPeriod = SFA.DAS.Payments.EarningEvents.Messages.External.EarningPeriod;
using EarningType = SFA.DAS.Payments.EarningEvents.Messages.External.EarningType;
using EmployerType = SFA.DAS.Payments.EarningEvents.Messages.External.EmployerType;
using Learner = SFA.DAS.Payments.EarningEvents.Messages.External.Learner;
using LearningType = SFA.DAS.Payments.EarningEvents.Messages.External.LearningType;

namespace SFA.DAS.Payments.EarningEvents.Specs.StepDefinitions
{
    [Binding]
    public class GSLApprenticeshipEarningsStepDefinitions
    {
        private TestSession testSession;
        private EarningsDataContext earningsDataContext;
        private CalculateGrowthAndSkillsPayments message;
        private List<EarningEvent> lastReceivedEvents = new();

        [BeforeScenario("gslApprenticeship")]
        public async Task BeforeScenario()
        {
            testSession = new TestSession();
            await testSession.DataContext.ClearCollectionPeriodsData();
            earningsDataContext = new EarningsDataContext(TestRunBindings.Config["ConnectionStrings:PaymentsConnectionString"]);
        }

        [AfterScenario("gslApprenticeship")]
        public async Task AfterScenario()
        {
            var ukprn = testSession.Provider.Ukprn;

            await earningsDataContext.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE FROM Payments2.GrowthAndSkillsEarningPricePeriod
                WHERE GrowthAndSkillsEarningsId IN (SELECT EarningsId FROM Payments2.GrowthAndSkillsEarning WHERE UKPRN = {ukprn})");

            await earningsDataContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Payments2.GrowthAndSkillsEarning WHERE UKPRN = {ukprn}");
        }

        [Given("the payments service receives a CalculateGrowthAndSkillsPayments event for apprenticeship payments")]
        public void GivenThePaymentsServiceReceivesACalculateGrowthAndSkillsPaymentsEventForApprenticeshipPayments()
        {
            message = BuildMessage(LearningType.Apprenticeship, CourseType.Apprenticeship);
        }

        [Given("the payments service receives a CalculateGrowthAndSkillsPayments event for apprenticeship unit payments")]
        public void GivenThePaymentsServiceReceivesACalculateGrowthAndSkillsPaymentsEventForApprenticeshipUnitPayments()
        {
            message = BuildMessage(LearningType.ApprenticeshipUnit, CourseType.ShortCourse);
        }

        private CalculateGrowthAndSkillsPayments BuildMessage(LearningType learningType, CourseType courseType)
        {
            return new CalculateGrowthAndSkillsPayments
            {
                EarningsId = Uuid.NewDatabaseFriendly(Database.SqlServer),
                UKPRN = testSession.Provider.Ukprn,
                EmployerContribution = 0m,
                Learner = new Learner
                {
                    ULN = testSession.Learner.Uln,
                    LearnerKey = testSession.Learner.LearnerIdentifier,
                    Reference = testSession.Learner.LearnRefNumber
                },
                Training = new Training
                {
                    CourseCode = "APPR001",
                    CourseReference = "APPR001",
                    CourseType = courseType,
                    LearningType = learningType,
                    AgeAtStartOfTraining = 25,
                    StartDate = DateTime.Today.AddMonths(-6),
                    PlannedEndDate = DateTime.Today.AddMonths(18),
                    TrainingStatus = TrainingStatus.Continuing,
                    LearningKey = Uuid.NewDatabaseFriendly(Database.SqlServer)
                },
                Earnings = new List<Earnings>()
            };
        }

        [Given("the event contains the following earnings")]
        public void GivenTheEventContainsTheFollowingEarnings(Table table)
        {
            var rows = table.Rows.Select(row => new
            {
                AcademicYear = short.Parse(row["Academic Year"]),
                DeliveryPeriod = byte.Parse(row["Delivery Period"]),
                EarningType = Enum.Parse<EarningType>(row["Earning Type"]),
                Amount = decimal.Parse(row["Amount"])
            });

            message.Earnings = rows
                .GroupBy(row => row.AcademicYear)
                .Select(group => new Earnings
                {
                    AcademicYear = group.Key,
                    PricePeriods = new List<PricePeriod>
                    {
                        new PricePeriod
                        {
                            StartDate = message.Training.StartDate,
                            Price = group.Sum(row => row.Amount),
                            Periods = group.Select(row => new EarningPeriod
                            {
                                DeliveryPeriod = row.DeliveryPeriod,
                                EarningType = row.EarningType,
                                Amount = row.Amount,
                                Employer = new Employer
                                {
                                    EmployerType = EmployerType.Levy,
                                    AccountId = testSession.Provider.Ukprn,
                                    FundingAccountId = testSession.Provider.Ukprn
                                },
                                LearningId = testSession.JobId
                            }).ToList()
                        }
                    }
                }).ToList();
        }

        [Given("the following collection periods are open")]
        public async Task GivenTheFollowingCollectionPeriodsAreOpen(Table table)
        {
            foreach (var row in table.Rows)
            {
                var period = byte.Parse(row["Collection Period"].TrimStart('R', 'r'));
                var academicYear = short.Parse(row["Academic Year"]);

                testSession.DataContext.CollectionPeriods.Add(new CollectionPeriodModel
                {
                    AcademicYear = academicYear,
                    Period = period,
                    CompletionDate = DateTime.Today,
                    EndDateTime = null,
                    ReferenceDataValidationDate = null,
                    StartDateTime = DateTime.Today,
                    Status = CollectionPeriodStatus.Open
                });
            }

            await testSession.DataContext.SaveChangesAsync();
        }

        [When("the event is processed")]
        public async Task WhenTheEventIsProcessed()
        {
            await testSession.DASMessageContext.Send<CalculateGrowthAndSkillsPayments>(message);
        }

        [Then("the following outgoing GSL Apprenticeship Earnings Event is published")]
        [Then("the following outgoing GSL Apprenticeship Earnings Events are published")]
        public async Task ThenTheFollowingOutgoingGSLApprenticeshipEarningsEventsArePublished(Table table)
        {
            var expectedGroups = table.Rows.GroupBy(row => (CollectionPeriod: row["Collection Period"], AcademicYear: row["Academic Year"])).ToList();

            await testSession.WaitForIt(
                () => GSLApprenticeshipEarningsEventHandler.GetEvents(testSession.Learner).Count() >= expectedGroups.Count,
                "Timed out waiting for the expected GSL Apprenticeship Earnings Event(s) to be published");

            var receivedEvents = GSLApprenticeshipEarningsEventHandler.GetEvents(testSession.Learner).ToList();
            lastReceivedEvents = receivedEvents.Cast<EarningEvent>().ToList();

            foreach (var group in expectedGroups)
            {
                var period = byte.Parse(group.Key.CollectionPeriod.TrimStart('R', 'r'));
                var academicYear = short.Parse(group.Key.AcademicYear);

                var matchingEvent = receivedEvents.SingleOrDefault(e =>
                    e.CollectionPeriod.AcademicYear == academicYear && e.CollectionPeriod.Period == period);

                Assert.That(matchingEvent, Is.Not.Null,
                    $"Expected a GSL Apprenticeship Earnings Event for collection period {group.Key.CollectionPeriod} {academicYear}");

                var learningEarnings = matchingEvent.OnProgrammeEarnings.SingleOrDefault(o => o.Type == OnProgrammeEarningType.Learning);
                Assert.That(learningEarnings, Is.Not.Null,
                    $"Expected Learning earnings on the event for collection period {group.Key.CollectionPeriod} {academicYear}");

                foreach (var row in group)
                {
                    var deliveryPeriod = byte.Parse(row["Delivery Period"]);
                    var amount = decimal.Parse(row["Amount"]);

                    Assert.That(learningEarnings.Periods.Any(p => p.Period == deliveryPeriod && p.Amount == amount), Is.True,
                        $"Expected a Learning earning of {amount} for delivery period {deliveryPeriod} in collection period {group.Key.CollectionPeriod} {academicYear}");
                }
            }
        }

        [Then("the following outgoing GSL Short Course Earnings Event is published")]
        [Then("the following outgoing GSL Short Course Earnings Events are published")]
        public async Task ThenTheFollowingOutgoingGSLShortCourseEarningsEventsArePublished(Table table)
        {
            var expectedGroups = table.Rows.GroupBy(row => (CollectionPeriod: row["Collection Period"], AcademicYear: row["Academic Year"])).ToList();

            await testSession.WaitForIt(
                () => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).Count() >= expectedGroups.Count,
                "Timed out waiting for the expected GSL Short Course Earnings Event(s) to be published");

            var receivedEvents = GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).ToList();
            lastReceivedEvents = receivedEvents.Cast<EarningEvent>().ToList();

            foreach (var group in expectedGroups)
            {
                var period = byte.Parse(group.Key.CollectionPeriod.TrimStart('R', 'r'));
                var academicYear = short.Parse(group.Key.AcademicYear);

                var matchingEvent = receivedEvents.SingleOrDefault(e =>
                    e.CollectionPeriod.AcademicYear == academicYear && e.CollectionPeriod.Period == period);

                Assert.That(matchingEvent, Is.Not.Null,
                    $"Expected a GSL Short Course Earnings Event for collection period {group.Key.CollectionPeriod} {academicYear}");

                foreach (var row in group)
                {
                    var deliveryPeriod = byte.Parse(row["Delivery Period"]);
                    var transactionType = Enum.Parse<ShortCourseEarningType>(row["Transaction Type"]);
                    var amount = decimal.Parse(row["Amount"]);

                    var matchingEarning = matchingEvent.Earnings.SingleOrDefault(e => e.Type == transactionType);
                    Assert.That(matchingEarning, Is.Not.Null,
                        $"Expected {transactionType} earnings on the event for collection period {group.Key.CollectionPeriod} {academicYear}");

                    Assert.That(matchingEarning.Periods.Any(p => p.Period == deliveryPeriod && p.Amount == amount), Is.True,
                        $"Expected a {transactionType} earning of {amount} for delivery period {deliveryPeriod} in collection period {group.Key.CollectionPeriod} {academicYear}");
                }
            }
        }

        [Then("all learner details, training details and payment details match the values on the incoming message")]
        public void ThenAllLearnerDetailsTrainingDetailsAndPaymentDetailsMatchTheValuesOnTheIncomingMessage()
        {
            foreach (var receivedEvent in lastReceivedEvents)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(receivedEvent.Ukprn, Is.EqualTo(message.UKPRN));
                    Assert.That(receivedEvent.Learner.Uln, Is.EqualTo(message.Learner.ULN));
                    Assert.That(receivedEvent.Learner.ReferenceNumber, Is.EqualTo(message.Learner.Reference));
                    Assert.That(receivedEvent.LearningAim.CourseCode, Is.EqualTo(message.Training.CourseCode));
                    Assert.That(receivedEvent.LearningAim.Reference, Is.EqualTo(message.Training.CourseReference));
                });
            }
        }

        [Then("the incoming earnings have been saved in the Earnings Bridge cache tables")]
        public async Task ThenTheIncomingEarningsHaveBeenSavedInTheEarningsBridgeCacheTables()
        {
            await testSession.WaitForIt(
                async () => await earningsDataContext.GrowthAndSkillsEarnings.AnyAsync(x => x.EarningsId == message.EarningsId),
                "Timed out waiting for the incoming earnings to be saved to the Earnings Bridge cache tables");

            var savedEarnings = await earningsDataContext.GrowthAndSkillsEarnings
                .Include(x => x.PricePeriods)
                .SingleAsync(x => x.EarningsId == message.EarningsId);

            var expectedPricePeriodCount = message.Earnings.Sum(e => e.PricePeriods.Sum(p => p.Periods.Count()));
            Assert.That(savedEarnings.PricePeriods, Has.Count.EqualTo(expectedPricePeriodCount));
        }
    }
}
