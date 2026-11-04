// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

[TestClass]
public class DocsTests
{
	/// <summary>
	/// API names the docs once used that the library has never had, so a snippet using one doesn't compile
	/// </summary>
	private static readonly string[] RetiredNames =
	[
		"NavigationFactory",
		"CreateNavigation<",
		"CreateBasicNavigation<",
		"FromNavigation(",
		"NavigationEventHandler",
	];

	[TestMethod]
	public void Docs_UseOnlyApiNamesTheLibraryHas()
	{
		// Arrange
		DirectoryInfo? root = new(AppContext.BaseDirectory);
		while (root != null && !File.Exists(Path.Combine(root.FullName, "Navigation.sln")))
		{
			root = root.Parent;
		}

		if (root == null)
		{
			Assert.Inconclusive("The test is not running inside the repository, so there is no docs folder to check.");
		}

		// Act
		List<string> found =
		[
			.. Directory.EnumerateFiles(Path.Combine(root.FullName, "docs"), "*.md")
				.SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
				.SelectMany(entry => RetiredNames
					.Where(name => entry.line.Contains(name, StringComparison.Ordinal))
					.Select(name => $"{Path.GetFileName(entry.file)}:{entry.index + 1} uses {name}")),
		];

		// Assert
		Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
	}
}
