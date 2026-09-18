using System;
using Miniscuplter.Core;

internal static class ProjectStoreInputValidationTests
{
    internal static void ValidateProjectStoreInputValidation()
    {
        // Test null path
        try
        {
            var layout = ProjectLayout.FromManifest(null);
            throw new Exception("Expected ArgumentException for null path");
        }
        catch (ArgumentException ex)
        {
            if (ex.ParamName != "projectPath")
                throw new Exception($"Expected ParamName 'projectPath', got '{ex.ParamName}'");
        }

        // Test empty path
        try
        {
            var layout = ProjectLayout.FromManifest("");
            throw new Exception("Expected ArgumentException for empty path");
        }
        catch (ArgumentException ex)
        {
            if (ex.ParamName != "projectPath")
                throw new Exception($"Expected ParamName 'projectPath', got '{ex.ParamName}'");
        }

        // Test whitespace-only path
        try
        {
            var layout = ProjectLayout.FromManifest("   ");
            throw new Exception("Expected ArgumentException for whitespace path");
        }
        catch (ArgumentException ex)
        {
            if (ex.ParamName != "projectPath")
                throw new Exception($"Expected ParamName 'projectPath', got '{ex.ParamName}'");
        }
    }
}
