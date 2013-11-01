GetOpt
======

getopt lib for parsing 



Example
-------

In your main method, just specify which option you support. 
Each option has a delegate that is called when the option i s found, use it to set your local variables.

```csharp
var opts = new GetOpt("Sample application that sorts input rows based on a delimeted field", 
	new[]
		{
			new CommandLineOption('s', "separator", "Field separator", 
				ParameterType.String, o => separator = (string)o),
			new CommandLineOption('v', "verbose", "Show more info about found files", 
				ParameterType.None, o => verbose = true),
			new CommandLineOption('n', "numeric", "sort numerically", 
				ParameterType.None, o => numeric = true),
			new CommandLineOption('f',"field","Which field to sort by. Default = 0", 
				ParameterType.Integer, o => field = (int)o),
			new CommandLineOption("file", ParameterType.String, o => file = (string)o, true),
		});
```



The lib has a built-in ShowUsage function that gives you this output if you call it (or call the program with the build-in "-h" option):

```
Sample application that sorts input rows based on a delimeted field
Usage: SampleApp -vnh -s <String> -f <Integer> [file]
Options:
 -s --separator <String> : Field separator
 -v --verbose            : Show more info about found files
 -n --numeric            : sort numerically
 -f --field <Integer>    : Which field to sort by. Default = 0
 -h --help               : This help
```
 
