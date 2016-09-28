Feature: Difference Detection
	In order to establish what the semantic version of the build should be
	As the build process
	I want to decide if the current assembly has breaking changes from the previous version

Scenario: New Assembly has a non breaking additive change
Given the previous assembly is called "TestData\Original\Original.dll"
Given the new assembly is called "TestData\NonBreakingAdditiveChange\Original.dll"
When I compare the two assemblies
Then I should be told there is 1 change
And I should be told that the change is 1 method has been added
And I should be told that the change is 0 method has been removed

Scenario: New Assembly has a breaking change due to a public api being modified
Given the previous assembly is called "TestData\Original\Original.dll"
Given the new assembly is called "TestData\BreakingChange\Original.dll"
When I compare the two assemblies
Then I should be told there is 1 change
And I should be told that the change is 1 method has been added
And I should be told that the change is 1 method has been removed

Scenario: Two new assemblies have non breaking additive changes
Given the previous assemblies are "TestData\Original\Original.dll;TestData\Original2\Original2.dll"
Given the new assemblies are "TestData\NonBreakingAdditiveChange\Original.dll;TestData\NonBreakingAdditiveChange2\Original2.dll"
When I compare the two sets of assemblies
Then I should be told there are 2 changes
And I should be told that the changes include 2 methods have been added
And I should be told that the changes include 0 methods have been removed

Scenario: Two new assemblies both have a breaking change due to a public api being modified
Given the previous assemblies are "TestData\Original\Original.dll;TestData\Original2\Original2.dll"
Given the new assemblies are "TestData\BreakingChange\Original.dll;TestData\BreakingChange2\Original2.dll"
When I compare the two sets of assemblies
Then I should be told there are 2 changes
And I should be told that the changes include 2 methods have been added
And I should be told that the changes include 2 methods have been removed