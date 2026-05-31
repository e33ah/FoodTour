namespace FoodTour.Pipeline;

public partial class Build : NukeBuild {
    public Target PatchWebConfig => _ => _
        .DependsOn(Publish)
        .OnlyWhenDynamic(() => IsLocalDeployEnabled || IsServerBuild)
        .Requires(() => GoogleClientId)
        .Requires(() => GoogleClientSecret)
        .Executes(() => {
            var webConfigPath = PublishDirectory / "web.config";

            if (!File.Exists(webConfigPath)) {
                Log.Error("web.config not found in publish output at {Path}", webConfigPath);
                throw new FileNotFoundException("web.config missing from publish output", webConfigPath);
            }

            var document = XDocument.Load(webConfigPath);
            var aspNetCore = document
                .Descendants("aspNetCore")
                .FirstOrDefault();

            if (aspNetCore == null) {
                Log.Error("Could not find <aspNetCore> element in web.config");
                throw new InvalidOperationException("<aspNetCore> element not found in web.config");
            }

            var environmentVariables = aspNetCore.Element("environmentVariables")
                ?? new XElement("environmentVariables");

            SetOrUpdateVariable(environmentVariables, "Authentication__Google__ClientId", GoogleClientId);
            SetOrUpdateVariable(environmentVariables, "Authentication__Google__ClientSecret", GoogleClientSecret);

            if (aspNetCore.Element("environmentVariables") == null) {
                aspNetCore.Add(environmentVariables);
            }

            document.Save(webConfigPath);
            Log.Information("web.config patched with Google OAuth credentials.");
        });

    static void SetOrUpdateVariable(XElement environmentVariables, string name, string value) {
        var existing = environmentVariables
            .Elements("environmentVariable")
            .FirstOrDefault(e => (string)e.Attribute("name") == name);

        if (existing != null) {
            existing.SetAttributeValue("value", value);
        } else {
            environmentVariables.Add(new XElement("environmentVariable",
                new XAttribute("name", name),
                new XAttribute("value", value)));
        }
    }
}
