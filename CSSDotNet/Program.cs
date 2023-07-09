using System.Diagnostics;
using CSSDotNet.Parser;

var cssText = File.ReadAllText("../../../tailwindcss-v2-cdn.css");

try
{
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.CancelAfter(10000);
    var cancellationToken = cancellationTokenSource.Token;
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    await using var parser = new Parser();
    var stylesheet = parser.ParseStylesheet(cssText, cancellationToken: cancellationToken);
    stopwatch.Stop();
    Console.WriteLine($"Parsed in {stopwatch.ElapsedMilliseconds}ms");
// // TODO add a proper ToString() method to the AST classes
    Console.WriteLine(stylesheet.Value.Count);
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}