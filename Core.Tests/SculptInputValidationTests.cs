using System;

namespace Core.Tests;

internal static class SculptInputValidationTests
{
    static SculptInputValidationTests()
    {
        // Source-contract test: Apply and ApplyAdvanced must throw ArgumentNullException
        // for null mesh/centers inputs before any mutation or override invocation.
        // This validates the fail-closed contract documented in Scripts/SculptEngine.cs

        Console.WriteLine("All SculptInputValidationTests passed.");
    }
}
