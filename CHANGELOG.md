## v1.0.5 (patch)

Changes since v1.0.4:

- Update project configuration and dependencies ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.4 (patch)

Changes since v1.0.3:

- Enhance project documentation and metadata ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix using directive placement in NavigationStackTests.cs for consistency with coding standards ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.3 (patch)

Changes since v1.0.2:

- Update project configuration and dependencies ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.2 (patch)

Changes since v1.0.1:

- Refactor navigation-related classes for clarity and performance ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.1 (patch)

Changes since v1.0.0:

- Refactor navigation services and contracts to enhance code quality. Updated accessibility modifiers to public for several members, moved using directives inside namespace declarations, and renamed NavigationStack to Navigation for consistency. Improved structure in InMemoryPersistenceProvider, JsonFilePersistenceProvider, and other service files. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor navigation interfaces and models to improve code quality and adhere to coding standards. Updated INavigationStack to INavigation, modified event handlers to use EventHandler<T> pattern, and ensured public accessibility modifiers are applied. Moved using directives inside namespace declarations to comply with IDE0065 rules across multiple files. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor NavigationStackTests to use INavigation interface instead of INavigationStack. Updated test methods to reflect changes in navigation service implementation, ensuring consistency and improved readability. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0 (major)

- Initial commit for Navigation ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance coding standards in derived cursor rules and add testing discussion documentation. Updated derived cursor rules to enforce namespace usage for using directives, require explicit accessibility modifiers, and adhere to .NET naming conventions. Included common build error guidelines to improve code quality and maintainability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Directory.Packages.props and global.json for centralized package management and SDK configuration. Update Navigation.Core project to handle nullability with default(T?) in NavigationStack. Modify .specstory to include derived cursor rules backup and add testing procedures discussion documentation. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add initial implementation of Navigation Stack Library with core features including navigation management, undo/redo support, and persistence. Introduced contracts, models, and services architecture, along with comprehensive documentation and examples. ([@matt-edmondson](https://github.com/matt-edmondson))
