<header>
reference System.Linq;
reference System.IO;
reference System.Console;
reference System.IO.FileSystem;
using System.Linq;
using System.Collections.Generic;
using System.IO;
</header>

<code run = "true" >
    // Mock elements are defined in mocks.csv file with structure:
    // ElementId, Element result
    Console.Write("Element with id 'element1' has value : ");
    Console.WriteLine("<result>element1</result>");
    Console.WriteLine("");

    var files = from file 
                in Directory.EnumerateFiles
                (
                    Environment.CurrentDirectory, 
                    "*.*",
                    System.IO.SearchOption.AllDirectories
                )
	            select file;

    EnumerateFiles(files);
</code>

<code>
void EnumerateFiles(IEnumerable<string> files)
{
    foreach (var f in files)
    {
        Console.WriteLine("{0}", f);
    }
}
</code>