using Shouldly;

namespace DrivingLessons.Application.Test.Text;

[TestClass]
public class SourceTextTest
{
    private const string SolutionFile = "DrivingLessons.sln";
    private const string SourceFolder = "src";
    private const string SourcePattern = "*.cs";
    private const char EnDash = (char)0x2013;
    private const char EmDash = (char)0x2014;
    private const char Ellipsis = (char)0x2026;
    private static readonly char[] TypographicPunctuation = [EnDash, EmDash, Ellipsis];
    private static readonly string[] BuildFolders = ["bin", "obj"];

    [TestMethod]
    public void Backend_Source_Uses_A_Plain_Hyphen_And_Three_Dots_Only()
    {
        //given
        var sourceRoot = Path.Combine(RepositoryRoot(), SourceFolder);

        //when
        var offending = LinesWithTypographicPunctuation(sourceRoot);

        //then
        offending.ShouldBeEmpty();
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFile)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException(SolutionFile);
    }

    private static List<string> LinesWithTypographicPunctuation(string sourceRoot)
    {
        var files = Directory
                        .EnumerateFiles(sourceRoot, SourcePattern, SearchOption.AllDirectories)
                        .Where(file => !IsBuildOutput(sourceRoot, file));

        return files
                   .SelectMany(file => File
                                           .ReadLines(file)
                                           .Select((line, index) => (Line: line, Number: index + 1))
                                           .Where(x => x.Line.IndexOfAny(TypographicPunctuation) >= 0)
                                           .Select(x => $"{Path.GetRelativePath(sourceRoot, file)}:{x.Number}"))
                   .ToList();
    }

    private static bool IsBuildOutput(string sourceRoot, string file)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, file);
        var folders = relativePath.Split(Path.DirectorySeparatorChar);

        return folders.Any(folder => BuildFolders.Contains(folder));
    }
}
