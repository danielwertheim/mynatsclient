#tool "nuget:?package=NUnit.ConsoleRunner"

#load "./buildconfig.cake"

var config = BuildConfig.Create(Context, BuildSystem);
Func<string, string> projectOutDir = project => string.Format("{0}{1}.v{2}", config.OutDir, project, config.SemVer);

Task("Default")
    .IsDependentOn("InitOutDir")
    .IsDependentOn("NuGet-Restore")
    .IsDependentOn("AssemblyVersion")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("Copy")
    .IsDependentOn("NuGet-Pack"); //assinfo, nuget-pack
  
Task("InitOutDir")
    .Does(() => {
        EnsureDirectoryExists(config.OutDir);
        CleanDirectory(config.OutDir);
    });

Task("NuGet-Restore")
    .Does(() => NuGetRestore(config.SolutionPath));

Task("AssemblyVersion").Does(() =>
{
    var file = config.SrcDir + "GlobalAssemblyVersion.cs";
    var info = ParseAssemblyInfo(file);
    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Version = config.BuildVersion,
        InformationalVersion = config.SemVer
    });
});
    
Task("Build").Does(() =>
{
    MSBuild(config.SolutionPath, new MSBuildSettings {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2015,
        Configuration = config.BuildProfile,
        PlatformTarget = PlatformTarget.MSIL
    }.WithTarget("Rebuild"));
});

Task("UnitTests").Does(() =>
{
    NUnit3(config.SrcDir + "**/*.UnitTests/bin/" + config.BuildProfile + "/*.UnitTests.dll", new NUnit3Settings {
        NoResults = true,
        NoHeader = true,
        TeamCity = config.IsTeamCityBuild
    });
});

Task("IntegrationTests").Does(() =>
{
    NUnit3(config.SrcDir + "**/*.IntegrationTests/bin/" + config.BuildProfile + "/*.IntegrationTests.dll", new NUnit3Settings {
        NoResults = true,
        NoHeader = true,
        TeamCity = config.IsTeamCityBuild
    });
});
  
Task("Copy").Does(() =>
{
    foreach(var project in config.Projects){
        var trg = projectOutDir(project);
        CreateDirectory(trg);
        CopyFiles(
            string.Format("{0}{1}/bin/{2}/{1}.*", config.SrcDir, project, config.BuildProfile),
            trg);
    }    
});

Task("NuGet-Pack").Does(() => {
    foreach(var project in config.Projects){
        NuGetPack(config.SrcDir + project + ".nuspec", new NuGetPackSettings {
            Version = config.SemVer,
            BasePath = projectOutDir(project),
            OutputDirectory = config.OutDir
        });
    }
});

RunTarget(config.Target);