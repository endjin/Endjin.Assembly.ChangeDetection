using Endjin.Assembly.ChangeDetection.Diff;
using Endjin.Assembly.ChangeDetection.Infrastructure;

namespace Endjin.Assembly.ChangeDetection.Specs.Steps
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Should;

    using TechTalk.SpecFlow;

    #endregion

    [Binding]
    public class DifferenceDetectionSteps
    {
        [Given(@"the previous assembly is called ""(.*)""")]
        public void GivenThePreviousAssemblyIsCalled(string previousAssembly)
        {
            ScenarioContext.Current.Set(previousAssembly, "PreviousAssembly");
        }

        [Given(@"the new assembly is called ""(.*)""")]
        public void GivenTheNewAssemblyIsCalled(string newAssembly)
        {
            ScenarioContext.Current.Set(newAssembly.ResolveBaseDirectory(), "NewAssembly");
        }

        [When(@"I compare the two assemblies")]
        public void WhenICompareTheTwoAssemblies()
        {
            var differ = new DiffAssemblies();

            var previous = new FileQuery(ScenarioContext.Current.Get<string>("PreviousAssembly"));
            var newAssembly = new FileQuery(ScenarioContext.Current.Get<string>("NewAssembly"));

            var differences = differ.Execute(new List<FileQuery> { previous }, new List<FileQuery> { newAssembly });

            ScenarioContext.Current.Set(differences, "Results");
        }

        [Then(@"I should be told there is (.*) change")]
        [Then(@"I should be told there are (.*) changes")]
        public void ThenIShouldBeToldThereAreChanges(int numberOfChanges)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");

            results.ChangedTypes.Count.ShouldEqual(numberOfChanges);
        }

        [Then(@"I should be told that the change is (.*) method has been added")]
        public void ThenIShouldBeToldThatTheChangeIsMethodHasBeenAdded(int numberAdded)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.First().Methods.AddedCount.ShouldEqual(numberAdded);
        }

        [Then(@"I should be told that the change is (.*) method has been removed")]
        public void ThenIShouldBeToldThatTheChangeIsMethodHasBeenRemoved(int numberRemoved)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.First().Methods.RemovedCount.ShouldEqual(numberRemoved);
        }
    }
}