#addin "Cake.FileHelpers"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var srcDir          = Directory("./src");
var solution        = "./src/SqlStreamStore.sln";
var buildNumber     = string.IsNullOrWhiteSpace(EnvironmentVariable("BUILD_NUMBER")) ? "0" : EnvironmentVariable("BUILD_NUMBER");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
});

Task("RestorePackages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(solution);

    InstallMySqlD("5.6.37");
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration
    };

    DotNetCoreBuild(solution, settings);
});

Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProjects = new string[] {
        "SqlStreamStore.Tests",
        "SqlStreamStore.MsSql.Tests",
        "SqlStreamStore.MySql.Tests"
    };

    foreach(var testProject in testProjects)
    {
        var projectDir = "./src/" + testProject + "/";
        var projectFile = testProject + ".csproj";
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration,
            WorkingDirectory = projectDir
        };
        DotNetCoreTest(projectFile, settings);
    }
});

Task("DotNetPack")
    .IsDependentOn("Build")
    .Does(() =>
{
    var versionSuffix = "build" + buildNumber.ToString().PadLeft(5, '0');

    var dotNetCorePackSettings   = new DotNetCorePackSettings
    {
        OutputDirectory = artifactsDir,
        NoBuild = true,
        Configuration = configuration,
        VersionSuffix = versionSuffix
    };
    
    DotNetCorePack("./src/SqlStreamStore", dotNetCorePackSettings);
    DotNetCorePack("./src/SqlStreamStore.MsSql", dotNetCorePackSettings);
    DotNetCorePack("./src/SqlStreamStore.MySql", dotNetCorePackSettings);
});

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("DotNetPack");

RunTarget(target);

void InstallMySqlD(string v) {
    var installDir     = srcDir + Directory("SqlStreamStore.MySql.Tests/.mysql");
    var mysqlDir       = installDir + Directory($"mysql-{v}-winx64/bin");
    var mysqldPath     = mysqlDir + File("mysqld.exe");
    var installPath    = installDir + File("mysql.zip");
    var version        = Version.Parse(v);

    EnsureDirectoryExists(mysqlDir);

    if (FileExists(mysqldPath)) {
        Information("Checking MySQL version...");
        using (var process = StartAndReturnProcess(mysqldPath, new ProcessSettings {
            Arguments = "--version",
            RedirectStandardOutput = true
        })) {
            process.WaitForExit();

            var stdout = process.GetStandardOutput().FirstOrDefault();

            Information(stdout);

            if ((stdout ?? string.Empty).Contains(v)) {
                Information($"MySQL {v} found; skipping installation.");

                return;
            }

            DeleteDirectory(mysqlDir, true);
        }
    }

    var mysqld = $"https://dev.mysql.com/get/Downloads/MySQL-{version.Major}.{version.Minor}/mysql-{version.Major}.{version.Minor}.{version.Build}-winx64.zip";

    DownloadFile(mysqld, installPath);

    Unzip(installPath, installDir);
}