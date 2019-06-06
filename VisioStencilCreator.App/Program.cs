using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace VisioStencilCreator.App
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args == null || !args.Any())
            {
                Console.WriteLine("Missing arguments.");
                return 1;
            }

            string ParseParameter(string key)
            {
                var param = args.FirstOrDefault(x => x.StartsWith($"--{key}=", StringComparison.InvariantCultureIgnoreCase));
                return param?.Replace($"--{key}=", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            }

            var imagePath = ParseParameter("image-path");
            var imagePattern = ParseParameter("image-pattern");
            var outputFilename = ParseParameter("output-filename");


            if (imagePath == null)
            {
                Console.WriteLine("Parameter 'image-path' is required.");
                return 1;
            }
            if (imagePattern == null)
            {
                Console.WriteLine("Parameter 'image-pattern' is required.");
                return 1;
            }
            if (outputFilename == null)
            {
                Console.WriteLine("Parameter 'output-filename' is required.");
                return 1;
            }
            Console.WriteLine(imagePath);
            if (!Directory.Exists(imagePath))
            {
                Console.WriteLine("Image path does not exist.");
                return 1;
            }
            if (Path.GetDirectoryName(outputFilename) == null ||
                !Directory.Exists(Path.GetDirectoryName(outputFilename)))
            {
                Console.WriteLine("Output folder does not exist.");
                return 1;
            }
            if (Path.GetExtension(outputFilename) != ".vssx")
            {
                Console.WriteLine("Output filename must have 'vssx' extension");
                return 1;
            }
            
            var matcher = new Matcher();
            matcher.AddIncludePatterns(imagePattern.Split(';'));
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(imagePath)));

            var imageFiles = result.Files
                .Select(x => Path.GetFullPath(Path.Combine(imagePath, x.Path)))
                .ToList();

            if (imageFiles.Count == 0)
            {
                Console.WriteLine("No images found for processing.");
                return 1;
            }

            var request = new VisioStencilRequest();

            Console.WriteLine($"---> Processing {imageFiles.Count} images.");
            foreach (var image in imageFiles)
            {
                Console.WriteLine(image);
                request.ImageFilePaths.Add(image);
            }

            using (var fileStream = File.Create(outputFilename))
            {
               var stream = VisioStencilFile.GenerateStencilFileFromImages(request);

               stream.CopyTo(fileStream);
            }

            return 0;
        }
    }
}
