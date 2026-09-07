using Godot;
using System;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV100ReleasePolish()
    {
        // The old v0.5 tier selector is retained only because legacy ETA/history code reads it.
        // Runtime quality is now exclusively controlled by the central preset system.
        if (_v05Quality != null)
        {
            _v05Quality.Disabled = true;
            _v05Quality.TooltipText = "Controlled by the central Quality Preset. This legacy selector is display-only.";
        }

        // Internal compatibility names stay stable until the v1.0.11 workflow layer has moved
        // every historical control into its user-facing task tab.
        if (FindChild("TabContainer", true, false) is TabContainer tabs)
        {
            for (int i = 0; i < tabs.GetTabCount(); i++)
            {
                string name = tabs.GetChild(i).Name.ToString();
                string? title = name switch
                {
                    "Print" => "Model",
                    "Quality v0.9.7" => "Quality",
                    "AI Models v0.9.8" => "AI Models",
                    "Locations v0.9.9" => "Files & Locations",
                    _ => null
                };
                if (title != null) tabs.SetTabTitle(i, title);
            }
        }

        SetStatus("Ready — Miniscuplter v1.0.16");
    }
}
