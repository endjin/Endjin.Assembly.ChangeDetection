using Endjin.Assembly.ChangeDetection.SemVer;

namespace Endjin.Assembly.ChangeDetection.Specs.Steps
{
    #region Using Directives

    using System.Collections.Generic;
    using Should;

    using TechTalk.SpecFlow;

    #endregion

    [Binding]
    public class SemanticVersioningSteps
    {
        [Given(@"the proposed version number is (.*)")]
        public void GivenTheProposedVersionNumberIs(string proposedVersionNumberVersion)
        {
            ScenarioContext.Current.Set(proposedVersionNumberVersion, "ProposedVersionNumber");
        }

        [When(@"I compare the two assemblies and validate the rules and the version number")]
        public void WhenICompareTheTwoAssembliesAndValidateTheRulesAndTheVersionNumber()
        {
            var previous = ScenarioContext.Current.Get<string>("PreviousAssembly");
            var current = ScenarioContext.Current.Get<string>("NewAssembly");
            var proposedVersionNumber = ScenarioContext.Current.Get<string>("ProposedVersionNumber");

            var semanticVersionAnalyzer = new SemanticVersionAnalyzer();

            var result = semanticVersionAnalyzer.Analyze(previous, current, proposedVersionNumber);

            ScenarioContext.Current.Set(result, "AnalysisResult");
            ScenarioContext.Current.Set(result.BreakingChangesDetected, "Results");
        }

        [Then(@"I should be told that the version number is (.*)")]
        public void ThenIShouldBeToldThatTheVersionNumberIs(string versionNumber)
        {
            var decidedVersionNumber = ScenarioContext.Current.Get<AnalysisResult>("AnalysisResult");

            decidedVersionNumber.VersionNumber.ShouldEqual(versionNumber);
        }
    }
}