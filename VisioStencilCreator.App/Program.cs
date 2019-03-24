using System;

namespace VisioStencilCreator.App
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args == null || args.Length != 3)
            {
                Console.WriteLine("Invalid number of arguments, expecting 3.");
                return 1;
            }

            var imagePattern = args[0];
            var basePath = args[1];
            var outputPath = args[2];

            VisioStencilFile.GenerateStencilFileFromImages(
              imagePattern,
              basePath,
              outputPath);

            return 0;
        }
    }
}
