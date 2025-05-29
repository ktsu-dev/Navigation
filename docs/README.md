# Navigation Stack Library Documentation

A comprehensive .NET library for managing navigation state with undo/redo support and persistence capabilities.

## Overview

The Navigation Stack Library provides a robust, extensible solution for managing navigation within applications. It follows SOLID principles and implements a clean Contracts, Models, Services (CMS) architecture.

## Key Features

-   **Navigation Stack Management**: Navigate forward, backward, and maintain history
-   **Undo/Redo Integration**: Seamless integration with external undo/redo providers
-   **Persistence Support**: Save and restore navigation state across sessions
-   **Event-Driven Architecture**: React to navigation changes with comprehensive events
-   **Extensible Design**: Plugin architecture for custom persistence and undo/redo providers
-   **Type Safety**: Strongly typed navigation items with metadata support

## Quick Start

```csharp
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

// Create a navigation stack
var navigationStack = new NavigationStack<NavigationItem>();

// Create navigation items
var homeItem = new NavigationItem("home", "Home Page");
var aboutItem = new NavigationItem("about", "About Page");

// Navigate
navigationStack.NavigateTo(homeItem);
navigationStack.NavigateTo(aboutItem);

// Go back
var previous = navigationStack.GoBack(); // Returns to Home Page

// Check navigation state
Console.WriteLine($"Can go back: {navigationStack.CanGoBack}");
Console.WriteLine($"Can go forward: {navigationStack.CanGoForward}");
Console.WriteLine($"Current: {navigationStack.Current?.DisplayName}");
```

## Documentation Structure

-   **[Architecture](Architecture.md)** - Detailed architecture and design patterns
-   **[API Reference](API.md)** - Complete API documentation
-   **[Usage Guide](Usage.md)** - Common usage patterns and best practices
-   **[Integration Guide](Integration.md)** - How to integrate with external systems
-   **[Examples](Examples.md)** - Comprehensive code examples
-   **[Design Decisions](Design-Decisions.md)** - Rationale behind architectural choices

## Installation

```xml
<PackageReference Include="ktsu.Navigation.Core" Version="1.0.0" />
```

## Requirements

-   .NET 8.0 or later
-   Compatible with all .NET platforms (Framework, Core, Standard)

## Contributing

Please read our contributing guidelines and follow the established coding standards.

## License

Licensed under the MIT License. See LICENSE.md for details.
