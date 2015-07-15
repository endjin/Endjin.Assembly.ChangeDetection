namespace Endjin.Assembly.ChangeDetection.Specs.Steps
{
    #region Using Directives

    using System.Collections.Generic;

    using AssemblyDifferences;
    using AssemblyDifferences.Infrastructure;
    using AssemblyDifferences.Rules;

    using Should;

    using TechTalk.SpecFlow;

    #endregion

    [Binding]
    public class ChangeRulesSteps
    {
        [When(@"I compare the two assemblies and validate the rules")]
        public void WhenICompareTheTwoAssembliesAndValidateTheRules()
        {
            var differ = new DiffAssemblies();

            var previous = new FileQuery(ScenarioContext.Current.Get<string>("PreviousAssembly"));
            var newAssembly = new FileQuery(ScenarioContext.Current.Get<string>("NewAssembly"));

            var differences = differ.Execute(new List<FileQuery> { previous }, new List<FileQuery> { newAssembly });
            var rule = new BreakingChangeRule();

            var result = rule.Detect(differences);

            ScenarioContext.Current.Set(result, "Results");
        }

        [Then(@"I should be told that the rule has been violated")]
        public void ThenIShouldBeToldThatTheRuleHasBeenViolated()
        {
            var results = ScenarioContext.Current.Get<bool>("Results");

            results.ShouldBeFalse();
        }

        [Then(@"I should be told that the rule has not been violated")]
        public void ThenIShouldBeToldThatTheRuleHasNotBeenViolated()
        {
            var results = ScenarioContext.Current.Get<bool>("Results");

            results.ShouldBeTrue();
        }
    }
}