public class BuildConfig
{
    public readonly string SrcDir = "./src/";
    public readonly string OutDir = "./build/";    
    public readonly string SolutionName = "MyNatsClient.sln";
    public readonly string[] Projects = new [] { "MyNatsClient" };
    
    public string Target { get; private set; }
    public string Version { get; private set; }
    public string SemVer { get; private set; }
    public string BuildVersion { get; private set; }
    public string BuildProfile { get; private set; }
    public bool IsTeamCityBuild { get; private set; }
    public string SolutionPath { get { return SrcDir + SolutionName; } }
    
    public static BuildConfig Create(
        ICakeContext context,
        BuildSystem buildSystem)
    {
        if (context == null)
            throw new ArgumentNullException("context");

        var target = context.Argument("target", "Default");
        var version = "0.1.1";

        return new BuildConfig
        {
            Target = target,
            Version = version,
            SemVer = version, //suffix e.g. 'rc-1',
            BuildVersion = version + "." + context.Argument("buildrevision", "0"),
            BuildProfile = context.Argument("configuration", "Release"),
            IsTeamCityBuild = buildSystem.TeamCity.IsRunningOnTeamCity
        };
    }
}