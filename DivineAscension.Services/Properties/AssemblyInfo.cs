using System.Runtime.CompilerServices;

// Expose internal members to main mod project for tight integration
[assembly: InternalsVisibleTo("DivineAscension")]

// Expose internal members to test project
[assembly: InternalsVisibleTo("DivineAscension.Tests")]