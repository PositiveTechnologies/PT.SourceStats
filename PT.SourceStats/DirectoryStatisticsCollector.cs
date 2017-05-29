using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PT.PM.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PT.SourceStats
{
    public class DirectoryStatisticsCollector
    {
        public bool Multithreading { get; set; }

        public ILogger Logger { get; set; }

        public Dictionary<string, string> CSharpGuidTypes = new Dictionary<string, string>
        {
            ["{603C0E0B-DB56-11DC-BE95-000D561079B0}"] = "ASP.NET MVC 1",
            ["{F85E285D-A4E0-4152-9332-AB1D724D3325}"] = "ASP.NET MVC 2",
            ["{E53F8FEA-EAE0-44A6-8774-FFD645390401}"] = "ASP.NET MVC 3",
            ["{E3E379DF-F4C6-4180-9B81-6769533ABE47}"] = "ASP.NET MVC 4",
            ["{349C5851-65DF-11DA-9384-00065B846F21}"] = "ASP.NET MVC 5",
            ["{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"] = "C#",
            ["{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"] = "C++",
            ["{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}"] = "Database",
            ["{4F174C21-8C12-11D0-8340-0000F80270F8}"] = "Database (other project types)",
            ["{3EA9E505-35AC-4774-B492-AD1749C4943A}"] = "Deployment Cab",
            ["{06A35CCD-C46D-44D5-987B-CF40FF872267}"] = "Deployment Merge Module",
            ["{978C614F-708E-4E1A-B201-565925725DBA}"] = "Deployment Setup",
            ["{AB322303-2255-48EF-A496-5904EB18DA55}"] = "Deployment Smart Device Cab",
            ["{F135691A-BF7E-435D-8960-F99683D2D49C}"] = "Distributed System",
            ["{BF6F8E12-879D-49E7-ADF0-5503146B24B8}"] = "Dynamics 2012 AX C# in AOT",
            ["{F2A71F9B-5D33-465A-A702-920D77279786}"] = "F#",
            ["{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}"] = "J#",
            ["{20D4826A-C6FA-45DB-90F4-C717570B9F32}"] = "Legacy (2003) Smart Device (C#)",
            ["{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}"] = "Legacy (2003) Smart Device (VB.NET)",
            ["{F85E285D-A4E0-4152-9332-AB1D724D3325}"] = "Model-View-Controller v2 (MVC 2)",
            ["{E53F8FEA-EAE0-44A6-8774-FFD645390401}"] = "Model-View-Controller v3 (MVC 3)",
            ["{E3E379DF-F4C6-4180-9B81-6769533ABE47}"] = "Model-View-Controller v4 (MVC 4)",
            ["{349C5851-65DF-11DA-9384-00065B846F21}"] = "Model-View-Controller v5 (MVC 5)",
            ["{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"] = "Mono for Android",
            ["{6BC8ED88-2882-458C-8E55-DFD12B67127B}"] = "MonoTouch",
            ["{F5B4F3BC-B597-4E2B-B552-EF5D8A32436F}"] = "MonoTouch Binding",
            ["{786C830F-07A1-408B-BD7F-6EE04809D6DB}"] = "Portable Class Library",
            ["{593B0543-81F6-4436-BA1E-4747859CAAE2}"] = "SharePoint (C#)",
            ["{EC05E597-79D4-47f3-ADA0-324C4F7C7484}"] = "SharePoint (VB.NET)",
            ["{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}"] = "SharePoint Workflow",
            ["{A1591282-1198-4647-A2B1-27E5FF5F6F3B}"] = "Silverlight",
            ["{4D628B5B-2FBC-4AA6-8C16-197242AEB884}"] = "Smart Device (C#)",
            ["{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}"] = "Smart Device (VB.NET)",
            ["{2150E333-8FDC-42A3-9474-1A3956D46DE8}"] = "Solution Folder",
            ["{3AC096D0-A1C2-E12C-1390-A8335801FDAB}"] = "Test",
            ["{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"] = "VB.NET",
            ["{C252FEB5-A946-4202-B1D4-9916A0590387}"] = "Visual Database Tools",
            ["{A860303F-1F3F-4691-B57E-529FC101A107}"] = "Visual Studio Tools for Applications (VSTA)",
            ["{BAA0C2D2-18E2-41B9-852F-F413020CAA33}"] = "Visual Studio Tools for Office (VSTO)",
            ["{349C5851-65DF-11DA-9384-00065B846F21}"] = "Web Application",
            ["{E24C65DC-7377-472B-9ABA-BC803B73C61A}"] = "Web Site",
            ["{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"] = "Windows (C#)",
            ["{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"] = "Windows (VB.NET)",
            ["{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"] = "Windows (Visual C++)",
            ["{3D9AD99F-2412-4246-B90B-4EAA41C64699}"] = "Windows Communication Foundation (WCF)",
            ["{76F1466A-8B6D-4E39-A767-685A06062A39}"] = "Windows Phone 8/8.1 Blank/Hub/Webview App",
            ["{C089C8C0-30E0-4E22-80C0-CE093F111A43}"] = "Windows Phone 8/8.1 App (C#)",
            ["{DB03555F-0C8B-43BE-9FF9-57896B3C5E56}"] = "Windows Phone 8/8.1 App (VB.NET)",
            ["{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}"] = "Windows Presentation Foundation (WPF)",
            ["{BC8A1FFA-BEE3-4634-8014-F334798102B3}"] = "Windows Store (Metro) Apps & Components",
            ["{14822709-B5A1-4724-98CA-57A101D1B079}"] = "Workflow (C#)",
            ["{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}"] = "Workflow (VB.NET)",
            ["{32F31D43-81CC-4C15-9DE6-3FC5453562B6}"] = "Workflow Foundation",
            ["{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"] = "Xamarin.Android",
            ["{6BC8ED88-2882-458C-8E55-DFD12B67127B}"] = "Xamarin.iOS",
            ["{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}"] = "XNA (Windows)",
            ["{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}"] = "XNA (XBox)",
            ["{D399B71A-8929-442a-A9AC-8BEC78BB2433}"] = "XNA (Zune)"
        };

        public DirectoryStatisticsCollector()
        {
        }

        public StatisticsMessage CollectStatistics(string directoryPath, int startInd = 0, int length = 0)
        {
            IEnumerable<string> fileNames = Enumerable.Empty<string>();
            if (Directory.Exists(directoryPath))
            {
                fileNames = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                if (startInd != 0)
                {
                    fileNames = fileNames.Skip(startInd);
                }
                if (length != 0)
                {
                    fileNames = fileNames.Take(length);
                }
            }
            else
            {
                fileNames = new string[] { directoryPath };
            }
            int totalFilesCount = fileNames.Count();

            var phpStatistics = new PhpStatistics();
            var javaStatistics = new JavaStatistics();
            var csharpStatistics = new CSharpStatistics();
            
            int processedCount = 0;

            if (!Multithreading)
            {
                foreach (var filePath in fileNames)
                {
                    CollectStatistics(phpStatistics, javaStatistics, csharpStatistics, filePath, totalFilesCount, ref processedCount);
                }
            }
            else
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                Parallel.ForEach(fileNames, options, filePath =>
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    CollectStatistics(phpStatistics, javaStatistics, csharpStatistics, filePath, totalFilesCount, ref processedCount);
                });
            }
            
            var result = new StatisticsMessage
            {
                Id = Guid.NewGuid().ToString(),
                ErrorCount = Logger?.ErrorCount ?? 0,
                LanguageStatistics = new List<LanguageStatistics>()
                {
                    phpStatistics,
                    javaStatistics,
                    csharpStatistics
                }
            };

            return result;
        }

        private void CollectStatistics(PhpStatistics phpStatistics, JavaStatistics javaStatistics, CSharpStatistics csharpStatistics, string filePath, int totalFilesCount, ref int processedCount)
        {
            CollectPhpStatistics(phpStatistics, filePath);
            CollectJavaStatistics(javaStatistics, filePath);
            CollectCSharpStatistics(csharpStatistics, filePath);
            Interlocked.Increment(ref processedCount);
            Logger?.LogInfo(new ProgressMessage(processedCount, totalFilesCount) { LastFileName = filePath });
        }

        private void CollectPhpStatistics(PhpStatistics phpStatistics, string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                if (fileName == "composer.json" || fileName == "composer.lock" || fileName == "php.ini")
                {
                    var lines = File.ReadAllLines(filePath);
                    string content;
                    if (fileName == "php.ini")
                    {
                        content = string.Join(Environment.NewLine, lines.Where(line =>
                            line.StartsWith("extension") || line.StartsWith("zend_extension")));
                    }
                    else
                    {
                        content = string.Join(Environment.NewLine, lines.Where(line =>
                            line.StartsWith("require") || line.StartsWith("require-dev") ||
                            line.StartsWith("packages") || line.StartsWith("packages-dev")));
                    }
                    phpStatistics.FilesContent[fileName] = content;
                }
                else if (filePath.ToLowerInvariant().EndsWith(".htaccess"))
                {
                    var lines = File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("php_value") || line.StartsWith("php_flag") || line.StartsWith("RewriteCond") || line.StartsWith("RewriteRule"))
                        {
                            phpStatistics.HtaccessStrings.Add(line);
                        }
                    }
                }
                else if (filePath.EndsWith(".php"))
                {
                    var fileCollector = new PhpInfoCollectorVisitor();
                    fileCollector.Logger = Logger;
                    FileStatistics fileStat = fileCollector.CollectInfo(filePath);
                    foreach (var classUsing in fileStat.ClassUsings)
                    {
                        int count = 0;
                        phpStatistics.ClassUsings.TryGetValue(classUsing.Key, out count);
                        phpStatistics.ClassUsings[classUsing.Key] = count + classUsing.Value;
                    }
                    foreach (var invoke in fileStat.MethodInvocations)
                    {
                        int count = 0;
                        phpStatistics.MethodInvocations.TryGetValue(invoke.Key, out count);
                        phpStatistics.MethodInvocations[invoke.Key] = count + invoke.Value;
                    }
                    foreach (var include in fileStat.Includes)
                    {
                        int count = 0;
                        phpStatistics.Includes.TryGetValue(include.Key, out count);
                        phpStatistics.Includes[include.Key] = count + include.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogInfo(new ErrorMessage(ex.ToString()));
            }
        }

        private void CollectJavaStatistics(JavaStatistics javaStatistics, string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (extension == ".xhtml")
                {
                    javaStatistics.XHtmlFileCount++;
                }
                else if (extension == ".java" || extension == ".class" || extension == ".jsp")
                {
                    var sourceSize = new FileInfo(filePath).Length;
                    javaStatistics.SourceFilesCount++;
                    javaStatistics.SourceCodeLinesCount += sourceSize;
                    switch (extension)
                    {
                        case ".java":
                            javaStatistics.JavaFilesCount++;
                            javaStatistics.JavaSourceSize += sourceSize;
                            break;
                        case ".class":
                            javaStatistics.ClassFilesCount++;
                            javaStatistics.ClassSourceSize += sourceSize;
                            break;
                        case ".jsp":
                            javaStatistics.JspFilesCount++;
                            javaStatistics.JspSourceSize += sourceSize;
                            break;
                    }
                }
                else if (extension == ".war" || extension == ".ear")
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), fileName);
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    ZipFile.ExtractToDirectory(filePath, tempDir);
                    var fileNames = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                    foreach (var fileName2 in fileNames)
                    {
                        CollectJavaStatistics(javaStatistics, fileName2);
                    }
                    Directory.Delete(tempDir, true);
                }
                else if (extension == ".jar")
                {
                    javaStatistics.Dependencies.Add(Path.GetFileNameWithoutExtension(fileName));
                }
                else if (fileName == "pom.xml")
                {
                    var doc = XDocument.Load(filePath);
                    var ns = doc.Root.Name.NamespaceName == "" ? "" : "{" + doc.Root.Name.NamespaceName + "}";

                    IEnumerable<string> repositories = doc.Element(XName.Get($"{ns}project"))
                        ?.Element(XName.Get($"{ns}repositories"))
                        ?.Elements()
                        .Select(elem =>
                            elem.Element(XName.Get($"{ns}id")).Value + " " + elem.Element(XName.Get($"{ns}url")).Value);
                    if (repositories != null)
                    {
                        foreach (var repository in repositories)
                        {
                            var buildTool = ExtractBuildTool(repository.Trim());
                            if (buildTool != null)
                            {
                                javaStatistics.DependencyManagers.Add(buildTool);
                            }
                            javaStatistics.Repositories.Add(repository.Trim());
                        }
                    }

                    var build = doc.Element(XName.Get($"{ns}project"))?.Element(XName.Get($"{ns}build"));
                    var plugins = build?.Element(XName.Get($"{ns}plugins"))?.Elements();
                    IEnumerable<string> buildTools = plugins.Select(elem => elem.Element(XName.Get($"{ns}artifactId")).Value);
                    if (buildTools != null)
                    {
                        foreach (var buildTool in buildTools)
                        {
                            var tool = ExtractBuildTool(buildTool.Trim());
                            if (tool != null)
                            {
                                javaStatistics.BuildTools.Add(tool);
                            }
                            javaStatistics.BuildToolsPlugins.Add(buildTool.Trim());
                        }
                    }

                    plugins = build?.Element(XName.Get($"{ns}pluginManagement"))?.Element(XName.Get($"{ns}plugins"))?.Elements();
                    buildTools = plugins?.Select(elem => elem.Element(XName.Get($"{ns}artifactId")).Value);
                    if (buildTools != null)
                    {
                        foreach (var buildTool in buildTools)
                        {
                            javaStatistics.BuildTools.RemoveWhere(t => buildTool.Contains(t));
                            var tool = ExtractBuildTool(buildTool.Trim());
                            if (tool != null)
                            {
                                javaStatistics.BuildTools.Add(tool);
                            }
                            javaStatistics.BuildToolsPlugins.Add(buildTool.Trim());
                        }
                    }

                    var pluginDependencies = plugins?.SelectMany(plugin =>
                        plugin.Element(XName.Get($"{ns}dependencies"))?.Elements() ?? new XElement[0])
                        .Select(elem =>
                           elem.Element(XName.Get($"{ns}artifactId")).Value + "-" + elem.Element(XName.Get($"{ns}version")).Value);
                    if (pluginDependencies != null)
                    {
                        foreach (var dependency in pluginDependencies)
                        {
                            javaStatistics.Dependencies.Add(dependency);
                        }
                    }

                    IEnumerable<string> dependencies = doc.Element(XName.Get($"{ns}project"))
                        ?.Element(XName.Get($"{ns}dependencies"))
                        ?.Elements()
                        .Select(elem =>
                           elem.Element(XName.Get($"{ns}artifactId")).Value + "-" + elem.Element(XName.Get($"{ns}version")).Value);
                    if (dependencies != null)
                    {
                        foreach (var dependency in dependencies)
                        {
                            javaStatistics.Dependencies.Add(dependency);
                        }
                    }
                }
                else if (fileName == "build.xml" || fileName == "ivy.xml" || fileName == "build.grandle")
                {

                }
                javaStatistics.FilesCount++;
            }
            catch (Exception ex)
            {
                Logger?.LogInfo(new ErrorMessage(ex.ToString()));
            }
        }

        private string ExtractBuildTool(string buildTool)
        {
            var buildToolLower = buildTool.ToLowerInvariant();
            var names = new[] { "maven", "grandle", "ant", "ivy" };

            foreach (var name in names)
            {
                if (buildToolLower.Contains(name))
                {
                    return name;
                }
            }

            return null;
        }

        private void CollectCSharpStatistics(CSharpStatistics csharpStatistics, string filePath)
        {
            try
            {
                var fileData = File.ReadAllText(filePath);
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (extension == ".sln")
                {
                    var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(filePath);
                    var solution = new CSharpSolution();
                    csharpStatistics.Solutions.Add(solution);
                    solution.Name = Path.GetFileNameWithoutExtension(filePath);
                    foreach (var project in solutionFile.ProjectsInOrder)
                    {
                        CSharpProject csharpProject = new CSharpProject();
                        csharpProject.Name = project.ProjectName;
                        csharpProject.GUID = project.ProjectGuid;
                        csharpProject.RelativePath = project.RelativePath;

                        var guidInd = fileData.IndexOf(csharpProject.GUID);
                        var lineInd = fileData.LastIndexOfAny(new char[] { '\r', '\n' }, guidInd);
                        if (lineInd == -1)
                        {
                            lineInd = 0;
                        }
                        fileData.IndexOf(')', lineInd);
                        int projectTypeGuidStartInd = lineInd + "Project(\"".Length + 1;
                        int projectTypeGuidEndInd = fileData.IndexOf("\")", lineInd);
                        string projectTypeGuid = fileData.Substring(projectTypeGuidStartInd, projectTypeGuidEndInd - projectTypeGuidStartInd);

                        string projectType;
                        if (CSharpGuidTypes.TryGetValue(projectTypeGuid, out projectType))
                        {
                            csharpProject.ProjectType = projectType;
                        }

                        var projectPath = Path.Combine(Path.GetDirectoryName(filePath), csharpProject.RelativePath);
                        CollectCSharpProjectStatistics(csharpStatistics, csharpProject, projectPath);
                        solution.Projects.Add(csharpProject);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogInfo(new ErrorMessage(ex.ToString()));
            }
        }

        private void CollectCSharpProjectStatistics(CSharpStatistics csharpStatistics, CSharpProject csharpProject, string projectPath)
        {
            try
            {
                var doc = XDocument.Load(projectPath);
                var ns = doc.Root.Name.NamespaceName == "" ? "" : "{" + doc.Root.Name.NamespaceName + "}";

                var frameworkVersions = doc.Element(XName.Get($"{ns}Project"))?.Elements(XName.Get($"{ns}PropertyGroup"))
                    .SelectMany(elem => elem.Elements(XName.Get($"{ns}TargetFrameworkVersion"))
                    .Select(elem2 => elem2.Value));

                if (frameworkVersions != null)
                {
                    csharpProject.FrameworkVersion = frameworkVersions.First();
                }

                var references = doc.Element(XName.Get($"{ns}Project"))?.Elements(XName.Get($"{ns}ItemGroup"))
                    .SelectMany(elem => elem.Elements(XName.Get($"{ns}Reference"))
                    .Select(elem2 => elem2.Attribute(XName.Get($"Include"))?.Value));

                if (references != null)
                {
                    csharpProject.References = references.ToList();
                }

                var projectFiles = new List<string>();
                projectFiles.AddRange(GetProjectFiles(doc, "Compile"));
                projectFiles.AddRange(GetProjectFiles(doc, "None"));
                projectFiles.AddRange(GetProjectFiles(doc, "Content"));
                projectFiles.AddRange(GetProjectFiles(doc, "EmbeddedResource"));
                projectFiles.AddRange(GetProjectFiles(doc, "Page"));
                projectFiles.AddRange(GetProjectFiles(doc, "Resource"));

                csharpProject.FilesCount = projectFiles.Count;
                csharpStatistics.FilesCount += projectFiles.Count;
                string fileDir = Path.GetDirectoryName(projectPath);
                foreach (var projectFile in projectFiles)
                {
                    CollectCSharpFileStatistics(csharpStatistics, csharpProject, Path.Combine(fileDir, projectFile));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.ToString());
            }
        }

        private void CollectCSharpFileStatistics(CSharpStatistics csharpStatistics, CSharpProject csharpProject, string filePath)
        {
            try
            {
                var fileData = File.ReadAllText(filePath);
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (extension == ".cs" || extension == ".aspx" || extension == ".cshtml" || extension == ".ashx" || extension == ".ascx")
                {
                    int linesCount = 0;
                    int filesCount = 0;
                    switch (extension)
                    {
                        case ".cs":
                            linesCount = CalculateCSharpLinesCount(fileData);
                            filesCount = 1;
                            csharpStatistics.CsFilesCount += 1;
                            csharpStatistics.CsLinesCount += linesCount;
                            csharpProject.CsFilesCount += 1;
                            csharpProject.CsLinesCount += linesCount;
                            break;
                        case ".aspx":
                            linesCount = CalculateXmlLinesCount(fileData);
                            filesCount = 1;
                            csharpStatistics.AspxFilesCount += 1;
                            csharpStatistics.AspxLinesCount += linesCount;
                            csharpProject.AspxFilesCount += 1;
                            csharpProject.AspxLinesCount += linesCount;
                            break;
                        case ".cshtml":
                            linesCount = CalculateXmlLinesCount(fileData);
                            filesCount = 1;
                            csharpStatistics.CsHtmlFilesCount += 1;
                            csharpStatistics.CsLinesCount += linesCount;
                            csharpProject.CsHtmlFilesCount += 1;
                            csharpProject.CsHtmlLinesCount += linesCount;
                            break;
                        case ".ashx":
                            linesCount = CalculateXmlLinesCount(fileData);
                            filesCount = 1;
                            csharpStatistics.AshxFilesCount += 1;
                            csharpStatistics.AshxLinesCount += linesCount;
                            csharpProject.AshxFilesCount += 1;
                            csharpProject.AshxLinesCount += linesCount;
                            break;
                        case ".ascx":
                            linesCount = CalculateXmlLinesCount(fileData);
                            filesCount = 1;
                            csharpStatistics.AscxFilesCount += 1;
                            csharpStatistics.AscxLinesCount += linesCount;
                            csharpProject.AscxFilesCount += 1;
                            csharpProject.AscxLinesCount += linesCount;
                            break;
                    }
                    csharpProject.SourceFilesCount += filesCount;
                    csharpStatistics.LinesCount += linesCount;
                    csharpStatistics.SourceFilesCount += filesCount;
                }
                else if (fileName == "packages.config")
                {
                    var doc = XDocument.Load(filePath);
                    var ns = doc.Root.Name.NamespaceName == "" ? "" : "{" + doc.Root.Name.NamespaceName + "}";
                    var dependencies = doc.Element(XName.Get($"{ns}packages"))?.Elements()
                        .Select(elem =>
                                elem.Attribute(XName.Get($"{ns}id"))?.Value + "-" + elem.Attribute(XName.Get($"{ns}version"))?.Value);
                    if (dependencies != null)
                    {
                        csharpProject.Dependencies = dependencies.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.ToString());
            }
        }

        private int CalculateCSharpLinesCount(string fileData)
        {
            var tree = CSharpSyntaxTree.ParseText(fileData);
            var root = tree.GetRoot();

            var commentTrivias = root.DescendantTrivia().Where(node =>
            {
                SyntaxKind kind = node.Kind();
                return kind == SyntaxKind.SingleLineCommentTrivia ||
                       kind == SyntaxKind.MultiLineCommentTrivia ||
                       kind == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                       kind == SyntaxKind.MultiLineDocumentationCommentTrivia ||
                       kind == SyntaxKind.DocumentationCommentExteriorTrivia ||
                       kind == SyntaxKind.XmlComment;
            });
            
            var treeWithoutComments = root.ReplaceTrivia(commentTrivias, (t1, t2) => default(SyntaxTrivia));
            var sourceText = treeWithoutComments.GetText();

            int linesCount = 0;
            foreach (var line in sourceText.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line.ToString()))
                {
                    linesCount++;
                }
            }

            return linesCount;
        }

        private int CalculateXmlLinesCount(string fileData)
        {
            fileData = fileData.Replace("\r\n", "\n").Replace("\r", "\n");
            fileData = RemoveComments(fileData, "<!--", "-->");

            var linesCount = fileData.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return linesCount;
        }

        private string RemoveComments(string fileData, string startMarker, string endMarker)
        {
            int startCommentInd = 0;
            int endCommentInd;
            startCommentInd = fileData.IndexOf(startMarker);
            while (startCommentInd != -1)
            {
                endCommentInd = fileData.IndexOf(endMarker, startCommentInd);
                if (endCommentInd == -1)
                {
                    endCommentInd = fileData.Length - 1;
                }
                fileData = fileData.Remove(startCommentInd, endCommentInd + startMarker.Length - startCommentInd);
                startCommentInd = fileData.IndexOf(startMarker, startCommentInd);
            }
            return fileData;
        }

        private string[] GetProjectFiles(XDocument doc, string buildAction)
        {
            var ns = doc.Root.Name.NamespaceName == "" ? "" : "{" + doc.Root.Name.NamespaceName + "}";
            var result = doc.Element(XName.Get($"{ns}Project"))?.Elements(XName.Get($"{ns}ItemGroup"))
                        .SelectMany(elem => elem.Elements(XName.Get($"{ns}{buildAction}"))
                        .Select(elem2 => elem2.Attribute(XName.Get($"Include"))?.Value));
            if (result == null)
            {
                return new string[0];
            }
            else
            {
                return result.ToArray();
            }
        }
    }
}
