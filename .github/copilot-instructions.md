# Copilot Instructions for Code Suggestions
These rules should be followed by github inline code editor and github copilot chat.

## General Guidelines for Code Suggestions
- **Frameworks:** Always use British English for spelling and grammar in code comments, documentation, commit messages, and any other text, including class, method and variable names.
- **Naming:** Prefer descriptive, explicit variable names for readability.
- **Coding Standards:**
    - Always ensure that new code confirms to the coding style.
    - Ensure that code complies with IDE0008. For built in types, use the explicit type. For all other types use the 'var' keyword.
    - Name the return value from a function 'result'.
    - Where in doubt use the one true brace style.
    - Ensure code adheres to project standards and style, and only change code relevant to the task.
    - Resolve all formatting warnings that are raised on the code you create.
    - Always use the following coding standards where it is not obvious from the context - the code in this next block shows the preferred style.  
    
```csharp
public class ExampleClass{
  private int exampleField;
  public int ExampleProperty { get; set; }

  public void ExampleMethod(){
    int one = 1;
    while (one < two) {
      if (one <2){ 
        one++;
      } else {
        one += 10;
      }
      break;
    }
  }
}
```

- **Modernity:** Use modern technologies and best practices relevant to the programming language and framework in use.
- **Rationale:** If a code suggestion involves a significant change, provide a detailed explanation of the rationale behind the change.
- **Constants:** Avoid using magic numbers; use constants or enums instead. Constants should be named using ALL_CAPS with underscores separating words (e.g., `MAX_SUPPLIER_SELECTIONS`, `DEFAULT_TIMEOUT_SECONDS`).
- **Whitespace:** Don't suggest whitespace changes.
- **Modularity & Maintainability:** Encourage modular design, maintainability, and reusability; follow DRY (Do Not Repeat Yourself) principle.
- **Explicitness:** Only implement changes explicitly requested.
- **Testing:** Suggest or include unit tests for new or modified code, covering edge cases and error handling.
- **Comments:** Use comments sparingly and only when meaningful. Do not remove comments that have previously been added unless the associated functionality has also been removed.
- **Verification:** Always ensure that the code compiles and passes existing tests after your changes.

### Making Edits
- **Change Management:** Focus on one conceptual change at a time and show clear before/after snippets.
- **Relevance:** Only change code that is directly relevant to the task at hand. Do not refactor unrelated code unless it is necessary for the change.

###  Instructions for Code Generation.
- Only generate a plan if you need it for the task at hand. If you do not need a plan then do not generate one.  
- Focus on solving the task at hand. Once you have finished the task, review all the code in any modified files and identify security issues or code quality issues. If you find any issues then suggest a prompt that
would fix the issues. If you do not find any issues then state that no issues were found.
- Only generate a summary document for changes made if you are asked for one.

## Dot Net Project Requirements
- **Language:** Use C# as the programming language.
- **Framework:** Use .NET 10 or later for all new projects.
- **Web:** Use ASP.NET Core for web applications.
- **DI:** Use Dependency Injection for managing dependencies.

## Test Project Requirements
- **Integration:** .ITest projects are Integration Tests.  Tests which require external resources, such as databases or file systems, should be placed in an .ITest project.
- **Unit:** .Test projects are Unit Tests.  Use XUnit.v3 for all unit tests.
- **NoComments:** Do not include comments for Arrange, Act, Assert sections but prefer to adopt that style of test.  Ensure that the primary subject of the test is named 'sut'. 
- **Naming:** When creating new unit tests, use the following naming convention: `MethodUnderTest_ExpectedBehavior_WhenCondition`. For example, `AddMeta_ReturnsOk_WhenMetaAddedSuccessfully`. This convention clearly communicates what is being tested, the expected outcome, and the scenario or condition.
- **Spacing:** Leave a single blank new line between the Arrange, Act, and Assert sections.  Prefer to use intermediary variables to help keep the arrange, act and assert sections separate.
- **Shouldly:** Use Shouldly for assertions.  When updating test files check other methods in the same file to ensure that they are using Shouldly for consistency.  If they are not then update them to use Shouldly.
- **Exploratory:** Use the ExploratoryTests class for new exploratory unit tests.
- **Fact:** Use the [Fact] attribute for unit tests.
- **Theory:** Use the [Theory] attribute for parameterised tests to reduce repetition and improve maintainability.
- **InlineData:**
    - Use the [InlineData] attribute for providing parameters to parameterised tests.
    - Try and use parameterised tests where possible to include edge cases and reduce test duplication.
    - Ensure that a wide variety of data is used to cover different scenarios.
    - If there are more than 5 parameters in the inline data then use a test class instead. If there are more than 5 rows of inline data then either reuse or create a TestData class to hold the data and use methods or properties to retrieve the data.
- **NoDisk:** Avoid creating unit tests that hit the disk, that hit a database, or that require external resources.  If the test requires external resources, it should be placed in an .ITest project.
- **EdgeCases:** Make suggestions for unit tests that cover edge cases and error handling.

## Repo Specific Requirements

### FoodTour Project Architecture
- **Pattern:** This is an ASP.NET Core MVC application (not Razor Pages despite workspace detection).
- **Database:** Use Entity Framework Core with SQL Server via `FoodTourContext` DbContext.
- **Connection Strings:** Connection strings are defined in `appsettings.json` with key `FoodTourContext`.
- **Global Usings:** The project uses a `Usings.cs` file for global using directives. When adding common namespaces, add them to `Usings.cs` rather than individual files.

### Database and Entity Guidelines
- **DbContext:** All database entities should be added as `DbSet` properties in `FoodTourContext` class.
- **Entities:** Place entity classes in the `Models` namespace/folder.
- **Migrations:** Use Entity Framework migrations. Migration files should be placed in the `Migrations` folder.
- **Validation:** Use data annotation attributes for validation (e.g., `[EmailAddress]`, `[Required]`).

### NUKE Build Pipeline
- **Project:** `FoodTour.Pipeline` is a NUKE build automation project and should not be included in solution builds.
- **Targets:** NUKE build targets should be defined as partial classes in the `Targets` folder.
- **Usings:** The pipeline has its own `Usings.cs` file with NUKE-specific global usings.
- **Execution:** The build pipeline must never attempt to rebuild itself during execution to avoid file locking issues.
- **CI/CD:** GitHub Actions workflows are auto-generated by NUKE. Do not manually edit `foodtour-ci.yml` unless necessary.

### Project Structure
- **Controllers:** Place MVC controllers in the `Controllers` folder, inheriting from `Controller`.
- **Models:** Place view models and entities in the `Models` folder.
- **Data:** Place DbContext and data access classes in the `Data` folder.
- **Views:** Follow the standard MVC structure: `Views/{ControllerName}/{ActionName}.cshtml`.
- **Tests:** Unit tests go in `FoodTour.Test` project (if integration tests are needed, create `FoodTour.ITest`).

### Namespace Conventions
- Main application uses `FoodTour` as the root namespace.
- Controllers use `FoodTour.Controllers` namespace.
- Models use `FoodTour.Models` namespace.
- Pipeline uses `FoodTour.Pipeline` namespace.

### Configuration
- **Environment-specific:** Use `appsettings.Development.json` for development-specific settings.
- **Secrets:** Never commit connection strings with real credentials; use user secrets or environment variables.

### Documentation and Release Notes
- **Changelog Updates:** For requests to update the changelog or prepare release notes, use the `update-changelog` skill.
- **Changelog File:** The repository changelog to update is `_Dependencies\Publish_Support\CHANGELOG.md`.
- **Changelog Process:** Review all commits since the last tag first, provide that review to the user, ask before continuing if there are significant concerns, and otherwise assume the next version is a patch release.
