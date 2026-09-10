using NUnit.Framework;
using Reqnroll;
using SFA.DAS.Payments.EarningEvents.Messages.Events;
using SFA.DAS.Payments.EarningEvents.Messages.External;
using SFA.DAS.Payments.EarningEvents.Messages.External.Commands;
using SFA.DAS.Payments.EarningEvents.Specs.Handlers;
using SFA.DAS.Payments.Messages.Common.Events;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using UUIDNext;
using UUIDNext.Tools;
using CourseType = SFA.DAS.Payments.EarningEvents.Messages.External.CourseType;
using EarningPeriod = SFA.DAS.Payments.EarningEvents.Messages.External.EarningPeriod;
using Learner = SFA.DAS.Payments.EarningEvents.Messages.External.Learner;

namespace SFA.DAS.Payments.EarningEvents.Specs.StepDefinitions
{
    [Binding]
    public class EarningEventsStepDefinitions
    {
        private readonly ScenarioContext scenarioContext;
        private readonly MessagingContext messagingContext;
        private TestSession testSession;
        private CollectionPeriod collectionPeriod;
        private short currentAcademicYear;
        private CollectionPeriod currentPeriod;
        private Guid previousIdentifier;
        private DateTime startDate;
        private EarningType earningType;
        private byte ageAtStartOfTraining;
        private EmployerType employerType;
        private Guid earningsId;
        private List<EarningPeriod> earningPeriods;

        public EarningEventsStepDefinitions(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        protected void SetCurrentCollectionYear()
        {
            currentAcademicYear = new CollectionPeriodBuilder().WithDate(DateTime.Today).Build().AcademicYear;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            testSession = new TestSession();
            SetCurrentCollectionYear();
            startDate = DateTime.Today;
            earningType = EarningType.Milestone1;
            ageAtStartOfTraining = 21;
            employerType = EmployerType.Levy;
            earningsId = Guid.Empty;
            earningPeriods = new List<EarningPeriod>();
            Console.WriteLine($"UKPRN : {testSession.Provider.Ukprn}, ULN: {testSession.Learner.Uln}, collection year: {currentAcademicYear}");
        }

        [AfterScenario]
        public void AfterScenario()
        {
        }

        [Given("a CalculatedRequiredLevyAmount message is received for a Levy employer with a GSO learner")]
        [Given("the Employer has insufficient funds")]
        [Given("the employer has insufficient levy balance for the full amount of the payment")]
        [Given("the employers remaining balance will be used first and co-investment used for the remainder")]
        [Given("the employer has no levy balance available")]
        public void BlankStep()
        {
        }

        [Given("a message is received for a Levy employer with a GSO learner")]
        public void MessageIsReceivedForLevyEmployer()
        {
            employerType = EmployerType.Levy;
        }

        [Given("the collection period has opened recently")]
        [Given("that the collection period has opened recently")]
        public async Task GivenThatTheCollectionPeriodHasOpenedRecently()
        {
            currentPeriod = new CollectionPeriodBuilder().WithDate(DateTime.Today).Build();
            testSession.DataContext.CollectionPeriods.Add(new CollectionPeriodModel
            {
                AcademicYear = currentPeriod.AcademicYear,
                CompletionDate = DateTime.Today,
                EndDateTime = null,
                Period = currentPeriod.Period,
                ReferenceDataValidationDate = null,
                StartDateTime = DateTime.Today,
                Status = CollectionPeriodStatus.Open
            });
            await testSession.DataContext.SaveChangesAsync();
        }

        [Given("a Learner changes from a Levy to a Non-levy employer")]
        public void ALearnerChangesFromALevyToANonLevyEmployer()
        {
            employerType = EmployerType.NonLevy;
            earningPeriods = new List<EarningPeriod>
            {
                new EarningPeriod
                {
                    Amount = 300,
                    DeliveryPeriod = 1,
                    EarningType = EarningType.Learning,
                    Employer = new Employer
                    {
                        AccountId = 123456,
                        EmployerType = EmployerType.Levy,
                        FundingAccountId = 123456
                    },
                    LearningId = 12345
                },
                new EarningPeriod
                {
                    Amount = 300,
                    DeliveryPeriod = 1,
                    EarningType = EarningType.Learning,
                    Employer = new Employer
                    {
                        AccountId = 123456,
                        EmployerType = EmployerType.NonLevy,
                        FundingAccountId = 123456
                    },
                    LearningId = 12345
                }
            };
        }

        [Given("a Learner changes from a Non-Levy to a Levy employer")]
        public void ALearnerChangesFromANonLevyToALevyEmployer()
        {
            employerType = EmployerType.Levy;
            earningPeriods = new List<EarningPeriod>
            {
                new EarningPeriod
                {
                    Amount = 300,
                    DeliveryPeriod = 1,
                    EarningType = EarningType.Learning,
                    Employer = new Employer
                    {
                        AccountId = 123456,
                        EmployerType = EmployerType.NonLevy,
                        FundingAccountId = 123456
                    },
                    LearningId = 12345
                },
                new EarningPeriod
                {
                    Amount = 300,
                    DeliveryPeriod = 1,
                    EarningType = EarningType.Learning,
                    Employer = new Employer
                    {
                        AccountId = 123456,
                        EmployerType = EmployerType.Levy,
                        FundingAccountId = 123456
                    },
                    LearningId = 12345
                }
            };
        }

        [Given("an employer has already approved the initial funding a learner on an Apprenticeship Unit course")]
        public void GivenAnEmployerHasAlreadyApprovedTheInitialFundingALearnerOnAnApprenticeshipUnitCourse()
        {
            throw new PendingStepException();
        }

        [Given("the earnings were persisted")]
        public void GivenTheEarningsWerePersisted()
        {
            throw new PendingStepException();
        }

        [Given("the provider and employer have agreed a change to the delivery of training for the course within the same collection period as the previous earnings")]
        public void GivenTheProviderAndEmployerHaveAgreedAChangeToTheDeliveryOfTrainingForTheCourseWithinTheSameCollectionPeriodAsThePreviousEarnings()
        {
            throw new PendingStepException();
        }

        [Given("the change has resulted in new earnings generated for the training")]
        public void GivenTheChangeHasResultedInNewEarningsGeneratedForTheTraining()
        {
            throw new PendingStepException();
        }

        [Given("the Payments system has already recorded the payments and associated earnings for the most recent Earnings for the training")]
        public void GivenThePaymentsSystemHasAlreadyRecordedThePaymentsAndAssociatedEarningsForTheMostRecentEarningsForTheTraining()
        {
            throw new PendingStepException();
        }

        [Given("there was an issue in the DAS Earnings system resulting in an older set of earnings being sent to the Payments system")]
        public void GivenThereWasAnIssueInTheDASEarningsSystemResultingInAnOlderSetOfEarningsBeingSentToThePaymentsSystem()
        {
            throw new PendingStepException();
        }


        [Given("the Payments system has already recorded the payments and associated earnings transactions for earnings that were approved today")]
        public void GivenThePaymentsSystemHasAlreadyRecordedThePaymentsAndAssociatedEarningsTransactionsForEarningsThatWereApprovedToday()
        {
            throw new PendingStepException();
        }

        [Given("there was an issue in the DAS Earnings system resulting in the previous set of earnings being resent to the Payments system")]
        public void GivenThereWasAnIssueInTheDASEarningsSystemResultingInThePreviousSetOfEarningsBeingResentToThePaymentsSystem()
        {
            throw new PendingStepException();
        }

        [Given("an employer has approved funding for a short course training")]
        public void GivenAnEmployerHasApprovedFundingForAShortCourseTraining()
        {
            throw new PendingStepException();
        }

        [Given("the earnings for the initial verion of the training delivery were not sent to the payments system")]
        public void GivenTheEarningsForTheInitialVerionOfTheTrainingDeliveryWereNotSentToThePaymentsSystem()
        {
            throw new PendingStepException();
        }

        [Given("the employer approves funding for a change to the earnings delivery")]
        public void GivenTheEmployerApprovesFundingForAChangeToTheEarningsDelivery()
        {
            throw new PendingStepException();
        }


        [Given("a previous set of earnings were recorded for the short course")]
        public void GivenAPreviousSetOfEarningsWereRecordedForTheShortCourse()
        {
            previousIdentifier = Uuid.NewDatabaseFriendly(Database.SqlServer);
            Console.WriteLine($"Previous id is: {previousIdentifier}");
        }

        [Given("the learning start date is on or after 1 August 2026")]
        public void GivenTheLearningStartDateIsOnOrAfter1stAugust2026()
        {
            startDate = new DateTime(2026, 8, 1);
        }

        [Given("the learning start date is before 1 August 2026")]
        public void GivenTheLearningStartDateIsBefore1StAugust2026()
        {
            startDate = new DateTime(2026, 7, 31);
        }
        [Given("the transaction type is a {word} payment")]
        public void GivenTheTransactionTypeIsAPayment(string transactionType)
        {
            if (!Enum.TryParse(transactionType, true, out EarningType parsedEarningType) ||
                parsedEarningType is not (EarningType.Learning
                    or EarningType.Completion
                    or EarningType.Milestone1))
            {
                Assert.Fail($"Unsupported transaction type: {transactionType}");
            }

            earningType = parsedEarningType;
        }

        [Given("the learner is aged under 25 on the start date")]
        public void GivenTheLearnerIsAgedUnder25OnTheStartDate()
        {
            ageAtStartOfTraining = 24;
        }

        [Given("the learner is 25 or older")]
        public void GivenTheLearnerIsAged25OrOlderOnTheStartDate()
        {
            ageAtStartOfTraining = 25;
        }

        [When("new changes are approved and the resultant earnings are sent to the Payments system")]
        [When("the payments are generated")]
        public async Task WhenPaymentsAreGenerated()
        {
            earningsId = Uuid.NewDatabaseFriendly(Database.SqlServer);
            if (earningPeriods.Count == 0)
            {
                earningPeriods = new List<EarningPeriod>
                {
                    new EarningPeriod
                    {
                        Amount = 300,
                        DeliveryPeriod = 1,
                        EarningType = earningType,
                        Employer = new Employer
                        {
                            AccountId = 123456,
                            EmployerType = employerType,
                            FundingAccountId = 123456
                        },
                        LearningId = 12345
                    }
                };
            }
            var earnings = new CalculateGrowthAndSkillsPayments
            {
                EarningsId = earningsId,
                UKPRN = testSession.Provider.Ukprn,
                EmployerContribution = 1,
                Learner = new Learner
                {
                    ULN = testSession.Learner.Uln,
                    LearnerKey = testSession.Learner.LearnerIdentifier,
                    Reference = testSession.Learner.LearnRefNumber,
                },
                Training = new Training
                {
                    AgeAtStartOfTraining = ageAtStartOfTraining,
                    CourseCode = "ZSC00001",
                    CourseReference = "ZSC00001",
                    CourseType = CourseType.ShortCourse,
                    LearningType = Messages.External.LearningType.ApprenticeshipUnit,
                    PlannedEndDate = DateTime.Today.AddMonths(1),
                    StartDate = startDate,
                    TrainingStatus = TrainingStatus.Continuing,
                    LearningKey = Uuid.NewDatabaseFriendly(Database.SqlServer)
                },
                Earnings = new List<Earnings>
                {
                    new Earnings
                    {
                        AcademicYear = currentAcademicYear,
                        PricePeriods =                 
                        [
                            new PricePeriod
                            {
                                StartDate = DateTime.Now,
                                CompletionAmount = 700,
                                InstalmentAmount = 300,
                                NumberOfInstalments = 1,
                                Price = 1000,
                                Periods = earningPeriods
                            }
                        ]
                    }

                }
            };
            await testSession.DASMessageContext.Send<CalculateGrowthAndSkillsPayments>(earnings);
        }

        [When("the Payments Earnings Bridge component receives the older, now invalid earnings")]
        public void WhenThePaymentsEarningsBridgeComponentReceivesTheOlderNowInvalidEarnings()
        {
            throw new PendingStepException();
        }


        [When("the Payments Earnings Bridge component receives the duplicate earnings")]
        public void WhenThePaymentsEarningsBridgeComponentReceivesTheDuplicateEarnings()
        {
            throw new PendingStepException();
        }

        [When("the Payments Earnings Bridge component receives the DAS Earnings")]
        public void WhenThePaymentsEarningsBridgeComponentReceivesTheDASEarnings()
        {
            throw new PendingStepException();
        }

        [Then("it should discard the earnings")]
        public void ThenItShouldDiscardTheEarnings()
        {
            throw new PendingStepException();
        }

        [Then("it should convert them to a ShortCourseEarnings event")]
        public void ThenItShouldConvertThemToAShortCourseEarningsEvent()
        {
            throw new PendingStepException();
        }

        [Then("the earnings should use an identifier that is higher or later than the identifier used in the previous earnings")]
        public void ThenTheEarningsShouldUseAnIdentifierThatIsHigherOrLaterThanTheIdentifierUsedInThePreviousEarnings()
        {
            throw new PendingStepException();
        }

        [Then("the new earnings should have identifiers that indicate they are later than the previous earnings")]
        public async Task ThenTheNewEarningsShouldHaveIdentifiersThatIndicateTheyAreLaterThanThePreviousEarnings()
        {
            await testSession.WaitForIt(() => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner)
                .Any(earning => IsLaterThan(previousIdentifier, earning.EventId)),"Failed to find the short course earning event");
        }

        [Then(@"the payment is fully funded by SFA \(100%\)")]
        public async Task PaymentLineIsGeneratedFor100Investment()
        {
            await testSession.WaitForIt(() => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner)
                .Any(earning => earning.ExternalEarningsId == earningsId), "Failed to find the short course earning event");

            var earningEvents = GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).ToList();

            Assert.That(earningEvents.Count, Is.EqualTo(1));

            var gslShortCourseEvent = earningEvents.Single();
            CheckSfaContributionPercentage(gslShortCourseEvent, 1m);

        }

        [Then(@"the payment funding is split between 'SFA co-investment' \(95%\) and 'Employer co-investment' \(5%\)")]
        public async Task ThenPaymentLinesAreGenerated95SplitBetweenSfaCoInvestmentAndEmployerCoInvestment()
        {

            await testSession.WaitForIt(() => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner)
                .Any(earning => earning.ExternalEarningsId == earningsId), "Failed to find the short course earning event");

            var earningEvents = GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).ToList();
            Assert.That(earningEvents.Count, Is.EqualTo(1));

            var gslShortCourseEvent = earningEvents.Single();
            CheckSfaContributionPercentage(gslShortCourseEvent, 0.95m);
        }

        [Then(@"the payment funding is split between 'SFA co-investment' \(75%\) and 'Employer co-investment' \(25%\)")]
        public async Task ThenPaymentLinesAreGenerated75SplitBetweenSfaCoInvestmentAndEmployerCoInvestment()
        {

            await testSession.WaitForIt(() => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner)
                .Any(earning => earning.ExternalEarningsId == earningsId), "Failed to find the short course earning event");

            var earningEvents = GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).ToList();
            Assert.That(earningEvents.Count, Is.EqualTo(1));

            var gslShortCourseEvent = earningEvents.Single();
            CheckSfaContributionPercentage(gslShortCourseEvent, 0.75m);
        }

        [Then("the payment funding percentage is set to Non-Levy: {decimal} and Levy: {decimal}")]
        public async Task ThenLevyAndNonLevySfaPercentagesAreSet(decimal nonLevyPercentage, decimal levyPercentage)
        {

            await testSession.WaitForIt(() => GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner)
                .Any(earning => earning.ExternalEarningsId == earningsId), "Failed to find the short course earning event");

            var earningEvents = GSLShortCourseEarningsEventHandler.GetEvents(testSession.Learner).ToList();
            Assert.That(earningEvents.Count, Is.EqualTo(1));
            
            var gslShortCourseEvent = earningEvents.Single();
            CheckEmployerTypeChangeContribution(gslShortCourseEvent, nonLevyPercentage, levyPercentage);
        }

        private void CheckSfaContributionPercentage(GSLShortCourseEarningsEvent gslShortCourseEvent, decimal sfaContributionPercentage)
        {
            var shortCourseEarnings = gslShortCourseEvent.Earnings;
            if (shortCourseEarnings != null)
            {
                var courseEarnings = shortCourseEarnings.ToList();

                Assert.That(courseEarnings.Count, Is.EqualTo(1));
                foreach (var periods in courseEarnings)
                {
                    foreach (var period in periods.Periods)
                    {
                        Assert.That(period.SfaContributionPercentage, Is.EqualTo(sfaContributionPercentage));
                    }
                }
            }
            else
            {
                throw new ReqnrollException("Short course earnings not found");
            }
        }
        private void CheckEmployerTypeChangeContribution(GSLShortCourseEarningsEvent gslShortCourseEvent, decimal nonLevyPercentage, decimal levyPercentage)
        {
            var shortCourseEarnings = gslShortCourseEvent.Earnings;
            if (shortCourseEarnings != null)
            {
                var courseEarnings = shortCourseEarnings.ToList();

                Assert.That(courseEarnings.Count, Is.EqualTo(2));

                foreach (var periods in courseEarnings)
                {
                    foreach (var period in periods.Periods)
                    {
                        if (period.ApprenticeshipEmployerType == ApprenticeshipEmployerType.NonLevy)
                        {
                            Assert.That(period.SfaContributionPercentage, Is.EqualTo(nonLevyPercentage));
                        }
                        else if (period.ApprenticeshipEmployerType == ApprenticeshipEmployerType.Levy)
                        {
                            Assert.That(period.SfaContributionPercentage, Is.EqualTo(levyPercentage));
                        }
                    }
                }
            }
            else
            {
                throw new ReqnrollException("Short course earnings not found");
            }
        }

        private bool IsLaterThan(Guid previousEventId, Guid newEventId)
        {
            Console.WriteLine($"Comparing previous guid: {previousEventId} to new guid: {newEventId}");
            
            var firstEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(previousEventId, out var firstEventDateTime);
            var secondEventIdDecodesToTimestamp = UuidDecoder.TryDecodeTimestamp(newEventId, out var secondEventDateTime);
            if (firstEventIdDecodesToTimestamp && secondEventIdDecodesToTimestamp)
            {
                if (firstEventDateTime >= secondEventDateTime)
                {
                    return false;
                }

                if (secondEventDateTime > firstEventDateTime)
                {
                    return true;
                }
            }

            return false;
        }
    }
}