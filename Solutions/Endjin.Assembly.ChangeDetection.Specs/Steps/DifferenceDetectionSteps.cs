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

        [Given(@"the previous assemblies are ""(.*)""")]
        public void GivenThePreviousAssembliesAre(string query)
        {
            ScenarioContext.Current.Set(query, "PreviousAssembliesQuery");
        }

        [Given(@"the new assemblies are ""(.*)""")]
        public void GivenTheNewAssembliesAre(string query)
        {
            ScenarioContext.Current.Set(query, "NewAssembliesQuery");
        }

        [When(@"I compare the two sets of assemblies")]
        public void WhenICompareTheTwoSetsOfAssemblies()
        {
            var differ = new DiffAssemblies();

            var previousAssemblies = FileQuery.ParseQueryList(ScenarioContext.Current.Get<string>("PreviousAssembliesQuery"));
            var newAssemblies = FileQuery.ParseQueryList(ScenarioContext.Current.Get<string>("NewAssembliesQuery"));

            var differences = differ.Execute(previousAssemblies, newAssemblies);

            ScenarioContext.Current.Set(differences, "Results");
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
        [Then(@"I should be told that the change is (.*) methods have been added")]
        public void ThenIShouldBeToldThatTheChangeIsMethodHasBeenAdded(int numberAdded)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.First().Methods.AddedCount.ShouldEqual(numberAdded);
        }

        [Then(@"I should be told that the change is (.*) method has been removed")]
        [Then(@"I should be told that the change is (.*) methods have been removed")]
        public void ThenIShouldBeToldThatTheChangeIsMethodHasBeenRemoved(int numberRemoved)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.First().Methods.RemovedCount.ShouldEqual(numberRemoved);
        }

        [Then(@"I should be told that the changes include (.*) method has been added")]
        [Then(@"I should be told that the changes include (.*) methods have been added")]
        public void ThenIShouldBeToldThatTheChangesIncludeMethodsHaveBeenAdded(int numberAdded)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.Sum(diff => diff.Methods.AddedCount).ShouldEqual(numberAdded);
        }

        [Then(@"I should be told that the changes include (.*) method has been removed")]
        [Then(@"I should be told that the changes include (.*) methods have been removed")]
        public void ThenIShouldBeToldThatTheChangeIncludeMethodHasBeenRemoved(int numberRemoved)
        {
            var results = ScenarioContext.Current.Get<AssemblyDiffCollection>("Results");
            results.ChangedTypes.Sum(diff => diff.Methods.RemovedCount).ShouldEqual(numberRemoved);
        }

    }
}