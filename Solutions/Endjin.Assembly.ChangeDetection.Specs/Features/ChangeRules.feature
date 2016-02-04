Feature: Change Rules
	In order to establish what the semantic version of the build should be
	As the build process
	I want to decide if the current assembly has breaking changes from the previous version

Scenario: New Assembly has a non breaking additive change does not violate any rules
Given the previous assembly is called "TestData\Original\Original.dll"
Given the new assembly is called "TestData\NonBreakingAdditiveChange\Original.dll"
When I compare the two assemblies and validate the rules
Then I should be told that the rule has not been violated

Scenario: New Assembly has a breaking change due to a public api being modified does violate rules
Given the previous assembly is called "TestData\Original\Original.dll"
Given the new assembly is called "TestData\BreakingChange\Original.dll"
When I compare the two assemblies and validate the rules
Then I should be told that the rule has been violated