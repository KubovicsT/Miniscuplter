using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    HSplitContainer? _v109ResponsiveSplit;
    SubViewportContainer? _v109ResponsiveHost;
    SubViewport? _v109ResponsiveSubViewport;
    TabContainer? _v109ResponsiveTabs;
    VBoxContainer? _v109ResponsiveRoot;

    public void InstallV109ResponsiveLayout()
    {
        _v109ResponsiveHost = FindChild("ViewportHost", true, false) as SubViewportContainer;
        _v109ResponsiveSubViewport = FindChild("Viewport", true, false) as SubViewport;
        _v109ResponsiveSplit = _v109ResponsiveHost?.GetParent() as HSplitContainer;
        _v109ResponsiveTabs = _v109ResponsiveSplit?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        _v109ResponsiveRoot = _v109ResponsiveSplit?.GetParent()?.GetParent() as VBoxContainer;

        if (_v109ResponsiveRoot != null)
        {
            GetViewport().SizeChanged += SyncV109RootToViewport;
            SyncV109RootToViewport();
        }

        if (_v109ResponsiveTabs != null)
        {
            _v109ResponsiveTabs.CustomMinimumSize = new Vector2(330, 0);
            _v109ResponsiveTabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _v109ResponsiveTabs.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            WrapV109MainTabsForScrolling(_v109ResponsiveTabs);
        }

        if (_v109ResponsiveHost != null)
        {
            _v109ResponsiveHost.CustomMinimumSize = new Vector2(260, 180);
            _v109ResponsiveHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _v109ResponsiveHost.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _v109ResponsiveHost.Resized += QueueV109ViewportResize;
        }

        if (_v109ResponsiveSplit != null)
        {
            _v109ResponsiveSplit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _v109ResponsiveSplit.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _v109ResponsiveSplit.Resized += SyncV109ResponsiveSplit;
        }

        if (_prompt != null) _prompt.CustomMinimumSize = new Vector2(0, 105);
        if (_aiPreview != null)
        {
            _aiPreview.CustomMinimumSize = new Vector2(0, 180);
            _aiPreview.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }

        SyncV109ResponsiveSplit();
        QueueV109ViewportResize();
    }

    void SyncV109RootToViewport()
    {
        if (_v109ResponsiveRoot == null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        if (size.X <= 1 || size.Y <= 1) return;

        _v109ResponsiveRoot.Position = Vector2.Zero;
        _v109ResponsiveRoot.Size = size;
        SyncV109ResponsiveSplit();
    }

    void WrapV109MainTabsForScrolling(TabContainer tabs)
    {
        int selected = tabs.CurrentTab;
        var pages = tabs.GetChildren().OfType<Control>().ToList();
        var titles = new List<string>(pages.Count);
        for (int i = 0; i < pages.Count; i++) titles.Add(tabs.GetTabTitle(i));

        for (int i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            if (page is ScrollContainer alreadyScrollable)
            {
                alreadyScrollable.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                alreadyScrollable.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                continue;
            }

            tabs.RemoveChild(page);
            var scroll = new ScrollContainer
            {
                Name = page.Name + " Responsive Scroll",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            tabs.AddChild(scroll);
            tabs.MoveChild(scroll, i);
            scroll.AddChild(page);

            page.CustomMinimumSize = new Vector2(0, page.CustomMinimumSize.Y);
            page.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            page.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            tabs.SetTabTitle(i, titles[i]);
        }

        if (tabs.GetTabCount() > 0)
            tabs.CurrentTab = Math.Clamp(selected, 0, tabs.GetTabCount() - 1);
    }

    void SyncV109ResponsiveSplit()
    {
        if (_v109ResponsiveSplit == null || _v109ResponsiveTabs == null) return;
        float width = _v109ResponsiveSplit.Size.X;
        if (width <= 1) return;

        const float sidebar = 330f;
        int maxViewport = Math.Max(1, (int)Math.Floor(width - _v109ResponsiveTabs.CustomMinimumSize.X));
        int desiredViewport = Math.Max(1, (int)Math.Round(width - sidebar));
        _v109ResponsiveSplit.SplitOffset = Math.Min(desiredViewport, maxViewport);
        QueueV109ViewportResize();
    }

    void QueueV109ViewportResize()
    {
        if (_v1019ViewportPipelineInstalled) return;
        CallDeferred(nameof(SyncV109SubViewportToHost));
    }

    void SyncV109SubViewportToHost()
    {
        // v1.0.19+ native viewport pipeline uses SubViewportContainer.Stretch as the single
        // normal resize owner. Keep this legacy path only for older composition states.
        if (_v1019ViewportPipelineInstalled) return;
        if (_v109ResponsiveHost == null || _v109ResponsiveSubViewport == null) return;
        Vector2 size = _v109ResponsiveHost.Size;
        var target = new Vector2I(Math.Max(1, (int)Math.Round(size.X)), Math.Max(1, (int)Math.Round(size.Y)));
        if (_v109ResponsiveSubViewport.Size != target)
            _v109ResponsiveSubViewport.Size = target;
    }
}
