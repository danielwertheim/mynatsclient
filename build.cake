#tool "nuget:?package=NUnit.ConsoleRunner"

#load "./buildconfig.cake"

var config = BuildConfig.Create(Context, BuildSystem);

Task("Default")
    .IsDependentOn("InitOutDir")
    .IsDependentOn("NuGet-Restore")
    .IsDependentOn("AssemblyVersion")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("NuGet-Pack");
  
Task("InitOutDir")
    .Does(() => {
        EnsureDirectoryExists(config.OutDir);
        CleanDirectory(config.OutDir);
    });

Task("NuGet-Restore")
    .Does(() => NuGetRestore(config.SolutionPath));

Task("AssemblyVersion").Does(() => {
    var file = config.SrcDir + "GlobalAssemblyVersion.cs";
    var info = ParseAssemblyInfo(file);
    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Version = config.BuildVersion,
        InformationalVersion = config.SemVer
    });
});
    
Task("Build").Does(() => {
    MSBuild(config.SolutionPath, new MSBuildSettings {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2015,
        Configuration = config.BuildProfile,
        PlatformTarget = PlatformTarget.MSIL
    }.WithTarget("Rebuild"));
});

Task("UnitTests").Does(() => {
    NUnit3(config.SrcDir + "**/*.UnitTests/bin/" + config.BuildProfile + "/*.UnitTests.dll", new NUnit3Settings {
        NoResults = true,
        NoHeader = true,
        TeamCity = config.IsTeamCityBuild
    });
});

Task("IntegrationTests").Does(() => {
    NUnit3(config.SrcDir + "**/*.IntegrationTests/bin/" + config.BuildProfile + "/*.IntegrationTests.dll", new NUnit3Settings {
        NoResults = true,
        NoHeader = true,
        TeamCity = config.IsTeamCityBuild
    });
});

Task("NuGet-Pack").Does(() => {
    foreach(var nuspec in GetFiles(config.SrcDir + "*.nuspec")) {
        NuGetPack(nuspec, new NuGetPackSettings {
            Version = config.SemVer,
            BasePath = config.SrcDir,
            OutputDirectory = config.OutDir,
            Properties = new Dictionary<string, string>
            {
                {"Configuration", config.BuildProfile}
            }
        });
    }
});

RunTarget(config.Target);