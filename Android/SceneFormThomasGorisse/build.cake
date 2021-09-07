#addin nuget:?package=SharpZipLib

var TARGET = Argument ("t", Argument ("target", "ci"));

var SF_VERSION = "1.19.5";

var NUGET_VERSION = "1.19.5";

var SCENEFORM_JAR_URL = $"https://search.maven.org/remotecontent?filepath=com/gorisse/thomas/sceneform/sceneform/{SF_VERSION}/sceneform-{SF_VERSION}.aar";
var UX_JAR_URL = $"https://search.maven.org/remotecontent?filepath=com/gorisse/thomas/sceneform/ux/{SF_VERSION}/ux-{SF_VERSION}.aar";
var CORE_JAR_URL = $"https://search.maven.org/remotecontent?filepath=com/gorisse/thomas/sceneform/core/{SF_VERSION}/core-{SF_VERSION}.aar";

Task ("externals")
	.WithCriteria (!FileExists ("./externals/sceneform.aar"))
	.Does (() =>
{
	EnsureDirectoryExists ("./externals/");

	// Download Dependencies
	Information ("Core Path: {0}", CORE_JAR_URL);
	DownloadFile (CORE_JAR_URL, "./externals/core.aar");
	Information ("UX Path: {0}", UX_JAR_URL);
	DownloadFile (UX_JAR_URL, "./externals/ux.aar");
	Information ("Sceneform Path: {0}", SCENEFORM_JAR_URL);
	DownloadFile (SCENEFORM_JAR_URL, "./externals/sceneform.aar");

	// Update .csproj nuget versions
	XmlPoke("./source/Core/Core.csproj", "/Project/PropertyGroup/PackageVersion", NUGET_VERSION);
	XmlPoke("./source/UX/UX.csproj", "/Project/PropertyGroup/PackageVersion", NUGET_VERSION);
	XmlPoke("./source/SceneForm/SceneForm.csproj", "/Project/PropertyGroup/PackageVersion", NUGET_VERSION);
});


Task("libs")
	.IsDependentOn("externals")
	.Does(() =>
{
	MSBuild("./SceneForm.sln", c => {
		c.Configuration = "Release";
		c.Targets.Clear();
		c.Targets.Add("Restore");
		c.Targets.Add("Build");
		c.Properties.Add("DesignTimeBuild", new [] { "false" });
	});
});

Task("nuget")
	.IsDependentOn("libs")
	.Does(() =>
{
	MSBuild ("./SceneForm.sln", c => {
		c.Configuration = "Release";
		c.Targets.Clear();
		c.Targets.Add("Pack");
		c.Properties.Add("PackageOutputPath", new [] { MakeAbsolute(new FilePath("./output")).FullPath });
		c.Properties.Add("PackageRequireLicenseAcceptance", new [] { "true" });
		c.Properties.Add("DesignTimeBuild", new [] { "false" });
	});
});

Task("samples")
	.IsDependentOn("nuget")
	.Does (() =>
{
	MSBuild("./samples/HelloSceneform.sln", c => {
		c.Configuration = "Release";
		c.Targets.Clear();
		c.Targets.Add("Restore");
		c.Targets.Add("Build");		
	});
});

Task ("clean")
	.Does (() =>
			{
				if (DirectoryExists ("./externals/"))
					DeleteDirectory 
						("./externals", new DeleteDirectorySettings 
												{
													Recursive = true,
													Force = true
												}
						);
			}
	);
	
Task ("ci")
	.IsDependentOn("libs")
	.IsDependentOn("nuget")
	.IsDependentOn("samples")
	.Does 
	(
		() =>
		{
		}
	);

RunTarget (TARGET);
