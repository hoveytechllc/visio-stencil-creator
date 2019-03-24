using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace VisioStencilCreator
{
    public class VisioStencilFile
    {
        public static void GenerateStencilFileFromImages(string minimatchPattern,
            string baseDirectoryPath,
            string outputFilename)
        {
            Console.WriteLine($"---> Validating Parameters");

            outputFilename = Path.GetFullPath(outputFilename);
            Console.WriteLine($"Using OutputFilename '{outputFilename}'");

            if (Path.GetExtension(outputFilename) != ".vssx")
                throw new Exception("OutputPath must have 'vssx' extension");
            var outputPath = Path.GetDirectoryName(outputFilename);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var matcher = new Matcher();
            matcher.AddIncludePatterns(minimatchPattern.Split(';'));
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectoryPath)));

            var imageFiles = result.Files
                .Select(x => Path.GetFullPath(Path.Combine(baseDirectoryPath, x.Path)))
                .ToList();

            if (imageFiles.Count == 0)
                throw new Exception("No images found for processing.");
            Console.WriteLine($"---> Processing {imageFiles.Count} images.");
            foreach (var image in imageFiles)
                Console.WriteLine(image);

            var templateStream = Assembly.GetExecutingAssembly()
                      .GetManifestResourceStream("VisioStencilCreator.Resources.Template.vssx");

            using (var packageStream = new MemoryStream())
            {
                templateStream.CopyTo(packageStream);
                packageStream.Seek(0, SeekOrigin.Begin);

                GenerateInternal(imageFiles, packageStream);

                using (var fileStream = File.Create(outputFilename))
                {
                    packageStream.Seek(0, SeekOrigin.Begin);
                    packageStream.CopyTo(fileStream);
                }
            }

        }

        private static void GenerateInternal(IList<string> images,
            Stream packageStream)
        {
            var package = Package.Open(
                packageStream,
                FileMode.Open,
                FileAccess.ReadWrite);

            var masterNames = string.Empty;
            var mastersXmlElements = string.Empty;

            var mastersPart = package.CreatePart(new Uri("/visio/masters/masters.xml", UriKind.Relative), "application/vnd.ms-visio.masters+xml");

            foreach (var image in images)
            {
                var id = images.IndexOf(image) + 1;
                var pngUri = new Uri($"/visio/media/image{id}.png", UriKind.Relative);
                var pngPart = package.CreatePart(pngUri, "image/png");

                using (Stream partStream = pngPart.GetStream(FileMode.Create,
                    FileAccess.ReadWrite))
                {
                    using (var fileStream = File.OpenRead(image))
                    {
                        fileStream.CopyTo(partStream);
                    }
                }

                var masterUri = new Uri($"/visio/masters/master{id}.xml", UriKind.Relative);
                var masterPart = package.CreatePart(masterUri, "application/vnd.ms-visio.master+xml");

                using (Stream partStream = masterPart.GetStream(FileMode.Create,
                    FileAccess.ReadWrite))
                {
                    using (var masterXmlStream = new MemoryStream(Encoding.UTF8.GetBytes(MasterXmlTemplate)))
                    {
                        masterXmlStream.CopyTo(partStream);
                    }
                }

                masterPart.CreateRelationship(new Uri($"../media/image{id}.png", UriKind.Relative),
                    TargetMode.Internal,
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image",
                    "rId1");

                var masterName = Path.GetFileName(image).ToLower().Replace(".png", "");
                masterNames += string.Format(MasterNameXmlTemplate, masterName);

                var imageThumbnail = ConvertImageToBase64Thumbnail(image);

                var masterXml = MastersMasterXmlTemplate
                    .Replace("{id}", id.ToString())
                    .Replace("{name}", masterName)
                    .Replace("{thumbnail}", imageThumbnail)
                    .Replace("{baseId}", $"{{{Guid.NewGuid().ToString()}}}")
                    .Replace("{uniqueId}", $"{{{Guid.NewGuid().ToString()}}}");

                mastersXmlElements += masterXml;


                mastersPart.CreateRelationship(masterUri,
                    TargetMode.Internal,
                    "http://schemas.microsoft.com/visio/2010/relationships/master",
                    $"rId{id}");

            }

            var propertiesPart = package.GetPart(new Uri("/docProps/app.xml", UriKind.Relative));

            using (Stream partStream = propertiesPart.GetStream(FileMode.Create,
                FileAccess.ReadWrite))
            {
                var propertiesXml = PropertiesXmlTemplate
                    .Replace("{masterCount}", images.Count.ToString())
                    .Replace("{partCount}", (images.Count + 1).ToString())
                    .Replace("{masterNames}", masterNames);

                using (var propertiesStream = new MemoryStream(Encoding.UTF8.GetBytes(propertiesXml)))
                {
                    propertiesStream.CopyTo(partStream);
                }
            }

            using (Stream partStream = mastersPart.GetStream(FileMode.Create,
                FileAccess.ReadWrite))
            {
                var propertiesXml = string.Format(MastersXmlTemplate, mastersXmlElements);

                using (var propertiesStream = new MemoryStream(Encoding.UTF8.GetBytes(propertiesXml)))
                {
                    propertiesStream.CopyTo(partStream);
                }
            }

            var documentPart = package.GetPart(new Uri("/visio/document.xml", UriKind.Relative));

            documentPart.CreateRelationship(new Uri("masters/masters.xml", UriKind.Relative),
                TargetMode.Internal,
                "http://schemas.microsoft.com/visio/2010/relationships/masters");

            package.Flush();
            package.Close();
        }

        private static string ConvertImageToBase64Thumbnail(string originalImagePath)
        {
            using (var memoryStream = new MemoryStream())
            {
                var original = Image.FromFile(originalImagePath);
                var destImage = new Bitmap(original, 16, 16);

                destImage.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Position = 0;
                byte[] byteBuffer = memoryStream.ToArray();

                return Convert.ToBase64String(byteBuffer, Base64FormattingOptions.InsertLineBreaks);
            }
        }

        private const string MasterXmlTemplate = @"<?xml version='1.0' encoding='utf-8' ?>
<MasterContents xmlns='http://schemas.microsoft.com/office/visio/2012/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xml:space='preserve'><Shapes><Shape ID='5' Type='Foreign' LineStyle='2' FillStyle='2' TextStyle='2'><Cell N='PinX' V='3.49999985328088'/><Cell N='PinY' V='6.49999974324154'/><Cell N='Width' V='0.6666666666666666'/><Cell N='Height' V='0.6666666666666666'/><Cell N='LocPinX' V='0.3333333333333333' F='Width*0.5'/><Cell N='LocPinY' V='0.3333333333333333' F='Height*0.5'/><Cell N='Angle' V='0'/><Cell N='FlipX' V='0'/><Cell N='FlipY' V='0'/><Cell N='ResizeMode' V='0'/><Cell N='ImgOffsetX' V='0' F='ImgWidth*0'/><Cell N='ImgOffsetY' V='0' F='ImgHeight*0'/><Cell N='ImgWidth' V='0.6666666666666666' F='Width*1'/><Cell N='ImgHeight' V='0.6666666666666666' F='Height*1'/><Cell N='ClippingPath' V='' E='#N/A'/><Cell N='TxtPinX' V='0.3333333333333333' F='Width*0.5'/><Cell N='TxtPinY' V='0' F='Height*0'/><Cell N='TxtWidth' V='0.6666666666666666' F='Width*1'/><Cell N='TxtHeight' V='0' F='Height*0'/><Cell N='TxtLocPinX' V='0.3333333333333333' F='TxtWidth*0.5'/><Cell N='TxtLocPinY' V='0' F='TxtHeight*0.5'/><Cell N='TxtAngle' V='0'/><Cell N='VerticalAlign' V='0'/><Section N='Geometry' IX='0'><Cell N='NoFill' V='0'/><Cell N='NoLine' V='0'/><Cell N='NoShow' V='0'/><Cell N='NoSnap' V='0'/><Cell N='NoQuickDrag' V='0'/><Row T='RelMoveTo' IX='1'><Cell N='X' V='0'/><Cell N='Y' V='0'/></Row><Row T='RelLineTo' IX='2'><Cell N='X' V='1'/><Cell N='Y' V='0'/></Row><Row T='RelLineTo' IX='3'><Cell N='X' V='1'/><Cell N='Y' V='1'/></Row><Row T='RelLineTo' IX='4'><Cell N='X' V='0'/><Cell N='Y' V='1'/></Row><Row T='RelLineTo' IX='5'><Cell N='X' V='0'/><Cell N='Y' V='0'/></Row></Section><ForeignData ForeignType='Bitmap' CompressionType='PNG'><Rel r:id='rId1'/></ForeignData></Shape></Shapes></MasterContents>";

        private const string MasterNameXmlTemplate = @"<vt:lpstr>{0}</vt:lpstr>";

        private const string PropertiesXmlTemplate = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Properties xmlns=""http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"" 
    xmlns:vt=""http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"">
    <Template></Template>
    <Application>Microsoft Visio</Application>
    <ScaleCrop>false</ScaleCrop>
    <HeadingPairs>
        <vt:vector size=""4"" baseType=""variant"">
            <vt:variant>
                <vt:lpstr>Pages</vt:lpstr>
            </vt:variant>
            <vt:variant>
                <vt:i4>1</vt:i4>
            </vt:variant>
            <vt:variant>
                <vt:lpstr>Masters</vt:lpstr>
            </vt:variant>
            <vt:variant>
                <vt:i4>{masterCount}</vt:i4>
            </vt:variant>
        </vt:vector>
    </HeadingPairs>
    <TitlesOfParts>
        <vt:vector size=""{partCount}"" baseType=""lpstr"">
            <vt:lpstr>Page-1</vt:lpstr>
            {masterNames}
        </vt:vector>
    </TitlesOfParts>
    <Manager></Manager>
    <Company></Company>
    <LinksUpToDate>false</LinksUpToDate>
    <SharedDoc>false</SharedDoc>
    <HyperlinkBase></HyperlinkBase>
    <HyperlinksChanged>false</HyperlinksChanged>
    <AppVersion>16.0000</AppVersion>
</Properties>";

        const string MastersXmlTemplate = @"<?xml version='1.0' encoding='utf-8' ?>
<Masters xmlns='http://schemas.microsoft.com/office/visio/2012/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xml:space='preserve'>{0}</Masters>";

        private const string MastersMasterXmlTemplate = @"<Master ID='{id}' NameU='{name}' IsCustomNameU='1' Name='{name}' IsCustomName='1' Prompt='' IconSize='1' AlignName='2' MatchByName='0' IconUpdate='1' UniqueID='{uniqueId}' BaseID='{baseId}' PatternFlags='0' Hidden='0' MasterType='2'><PageSheet LineStyle='0' FillStyle='0' TextStyle='0'><Cell N='PageWidth' V='8.5'/><Cell N='PageHeight' V='11'/><Cell N='ShdwOffsetX' V='0.125'/><Cell N='ShdwOffsetY' V='-0.125'/><Cell N='PageScale' V='1' U='IN_F'/><Cell N='DrawingScale' V='1' U='IN_F'/><Cell N='DrawingSizeType' V='0'/><Cell N='DrawingScaleType' V='0'/><Cell N='InhibitSnap' V='0'/><Cell N='PageLockReplace' V='0' U='BOOL'/><Cell N='PageLockDuplicate' V='0' U='BOOL'/><Cell N='UIVisibility' V='0'/><Cell N='ShdwType' V='0'/><Cell N='ShdwObliqueAngle' V='0'/><Cell N='ShdwScaleFactor' V='1'/><Cell N='DrawingResizeType' V='1'/></PageSheet><Icon>
{thumbnail}</Icon><Rel r:id='rId{id}'/></Master>";
    }
}
