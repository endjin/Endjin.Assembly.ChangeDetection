Feature: SemanticVersioning
	In order to establish what the semantic version of the build should be
	As the build process
	I want to decide what the next public build number should be by comparing the current build with the previous build

Scenario: New Assembly has a non breaking additive change
Given the previous assembly is called "C:\_Projects\endjin\IP\Endjin.Assembly.ChangeDetection\Solutions\Original\bin\Debug\Original.dll"
Given the new assembly is called "C:\_Projects\endjin\IP\Endjin.Assembly.ChangeDetection\Solutions\NonBreakingAdditiveChange\bin\Debug\Original.dll"
Given the proposed version number is 1.0.1
When I compare the two assemblies and validate the rules and the version number
Then I should be told that the rule has not been violated
And I should be told that the version number is 1.0.1

Scenario: New Assembly has a breaking change due to a public api being modified does violate rules
Given the previous assembly is called "C:\_Projects\endjin\IP\Endjin.Assembly.ChangeDetection\Solutions\Original\bin\Debug\Original.dll"
Given the new assembly is called "C:\_Projects\endjin\IP\Endjin.Assembly.ChangeDetection\Solutions\BreakingChange\bin\Debug\Original.dll"
Given the proposed version number is 1.0.1
When I compare the two assemblies and validate the rules and the version number
Then I should be told that the rule has been violated
And I should be told that the version number is 2.0.0