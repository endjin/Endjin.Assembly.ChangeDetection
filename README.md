# Endjin.Assembly.ChangeDetection

This is an experiment to see if it is possibly to build a tool to: 

- automatically detect breaking changes in a .NET assembly 
- determine the what the next valid SemanticVersion should be
- use ILMerge to re-write the assembly with the new suggested Semantic Version number.

For the purpose of automating the generation & versioning of NuGet packages.

## Breaking Changes rules: ##

A breaking change is defined as the modification of any public type (either changing of a signature or the removal of a public type from the assembly). The addition of new types to an assembly is not deemed a breaking change. 

## Thoughts ##

Next steps, I would like to turn this tool into a TeamCity Meta-Runner, where the new Semantic Version number is echoed up, via a Service Message, to set the new version number for the branch.

Ideally I would like the concepts laid out here to become a 1st class feature in TeamCity; to provide much better support for Semantic Versioning, Assembly breaking changes and the ability for the CI server to control the version number of the assemblies.


##Notes##

To get this running locally, you may need to change the hard coded file paths for detecting the assemblies to compare... I really should get around to making that resolvable, but it wasn't what I was trying to prove!

##Comments##

Please ping me via @[HowardvRooijen](http://twitter.com/HowardvRooijen)

## Acknowledgements: ##

Code from the CodePlex project [APIChange](https://apichange.codeplex.com/) by [Alois Kraus](http://geekswithblogs.net/akraus1/archive/2010/06/03/140207.aspx) have been used.
As have modification made by [@GrahamTheCoder](http://twitter.com/grahamthecoder) who added new Mono.Cecil integration

