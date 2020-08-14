var TARGET = Argument("t", Argument("target", "ci"));

Task("libs")
	.Does(() =>
{
	MSBuild("./SkiaSharp.Extended.sln", new MSBuildSettings()
		.EnableBinaryLogger("./output/binlogs/libs.binlog")
		.SetConfiguration("Release")
		.WithRestore());
});

Task("nuget")
	.IsDependentOn("libs")
	.Does(() =>
{
	MSBuild("./source/source.sln", new MSBuildSettings()
		.EnableBinaryLogger("./output/binlogs/nuget.binlog")
		.SetConfiguration("Release")
		.WithProperty("PackageOutputPath", MakeAbsolute(new FilePath("./output/")).FullPath)
		.WithTarget("Pack"));
});

Task("tests")
	.IsDependentOn("libs")
	.Does(() =>
{
	var failed = 0;

	foreach (var csproj in GetFiles("./tests/*/*.csproj")) {
		try {
			DotNetCoreTest(csproj.FullPath, new DotNetCoreTestSettings {
				Configuration = "Release",
				Logger = $"trx;LogFileName={csproj.GetFilenameWithoutExtension()}.trx",
			});
		} catch (Exception) {
			failed++;
		}
	}

	var output = $"./output/test-results/";
	EnsureDirectoryExists(output);
	CopyFiles($"./tests/**/TestResults/*.trx", output);

	if (failed > 0)
		throw new Exception($"{failed} tests have failed.");
});

Task("samples")
	.IsDependentOn("nuget")
	.Does(() =>
{
	MSBuild("./SkiaSharp.Extended.sln", new MSBuildSettings()
		.EnableBinaryLogger("./output/binlogs/samples.binlog")
		.SetConfiguration("Release")
		.WithRestore());
});

Task("ci")
	.IsDependentOn("libs")
	.IsDependentOn("nuget")
	.IsDependentOn("tests")
	.IsDependentOn("samples");

RunTarget(TARGET);
