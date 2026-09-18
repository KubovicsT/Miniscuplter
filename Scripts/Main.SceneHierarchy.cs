using Godot;
using Miniscuplter.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    Tree? _sceneHierarchyTree;
    ScrollContainer? _sceneHierarchyScroll;
    Timer? _sceneHierarchySyncTimer;
    readonly Dictionary<ObjectId, TreeItem> _sceneHierarchyItems = new();
    string _sceneHierarchySignature = "";
    bool _sceneHierarchySyncing;

    public void InstallSceneHierarchy()
    {
        if (_sceneHierarchyTree != null && IsInstanceValid(_sceneHierarchyTree)) return;
        if (_sceneList == null || !IsInstanceValid(_sceneList)) return;
        if (_sceneList.GetParent() is not ScrollContainer legacyScroll) return;
        if (legacyScroll.GetParent() is not Container scenePanel) return;

        int legacyIndex = legacyScroll.GetIndex();
        legacyScroll.Visible = false;

        _sceneHierarchyScroll = new ScrollContainer
        {
            Name = "SceneHierarchyScroll",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 120)
        };
        scenePanel.AddChild(_sceneHierarchyScroll);
        scenePanel.MoveChild(_sceneHierarchyScroll, legacyIndex);

        _sceneHierarchyTree = new Tree
        {
            Name = "SceneHierarchy",
            HideRoot = false,
            Columns = 1,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(205, 120),
            TooltipText = "Scene hierarchy. Selecting an item uses the same viewport/object selection owner."
        };
        _sceneHierarchyTree.ItemSelected += SceneHierarchyItemSelected;
        _sceneHierarchyScroll.AddChild(_sceneHierarchyTree);

        _sceneHierarchySyncTimer = new Timer
        {
            Name = "SceneHierarchySync",
            WaitTime = 0.2,
            OneShot = false,
            Autostart = true
        };
        _sceneHierarchySyncTimer.Timeout += SceneHierarchySync;
        AddChild(_sceneHierarchySyncTimer);

        SceneHierarchyRebuild();
    }

    void SceneHierarchySync()
    {
        if (_sceneHierarchyTree == null || !IsInstanceValid(_sceneHierarchyTree)) return;

        var liveObjects = _objects.Where(IsInstanceValid).ToList();
        string signature = SceneHierarchyStableSignature(liveObjects);

        if (!string.Equals(signature, _sceneHierarchySignature, StringComparison.Ordinal))
        {
            SceneHierarchyRebuild();
            return;
        }

        SceneHierarchySyncSelection();
    }

    void SceneHierarchyRebuild()
    {
        if (_sceneHierarchyTree == null || !IsInstanceValid(_sceneHierarchyTree)) return;

        _sceneHierarchySyncing = true;
        try
        {
            _sceneHierarchyTree.Clear();
            _sceneHierarchyItems.Clear();

            TreeItem root = _sceneHierarchyTree.CreateItem();
            root.SetText(0, "Scene");
            root.SetTooltipText(0, "Current editable scene");
            root.Collapsed = false;

            var liveObjects = _objects.Where(IsInstanceValid).ToList();
            var liveIds = liveObjects.Select(obj => obj.GetInstanceId()).ToHashSet();

            foreach (MeshInstance3D obj in liveObjects.Where(obj => obj.GetParent() is not MeshInstance3D parent || !liveIds.Contains(parent.GetInstanceId())))
                SceneHierarchyAddObject(obj, root, liveObjects, liveIds);

            _sceneHierarchySignature = SceneHierarchyStableSignature(liveObjects);
            SceneHierarchySyncSelection();
        }
        finally
        {
            _sceneHierarchySyncing = false;
        }
    }

    void SceneHierarchyAddObject(MeshInstance3D obj, TreeItem parentItem, IReadOnlyList<MeshInstance3D> liveObjects, HashSet<ulong> liveIds)
    {
        if (_sceneHierarchyTree == null) return;

        ObjectId objectId = V1013ObjectId(obj);
        TreeItem item = _sceneHierarchyTree.CreateItem(parentItem);
        item.SetText(0, obj.Name.ToString());
        item.SetMetadata(0, objectId.ToString());
        item.SetTooltipText(0, $"{obj.Name} · object {objectId}");
        _sceneHierarchyItems[objectId] = item;

        foreach (MeshInstance3D child in liveObjects.Where(candidate => candidate.GetParent() == obj && liveIds.Contains(candidate.GetInstanceId())))
            SceneHierarchyAddObject(child, item, liveObjects, liveIds);
    }

    void SceneHierarchySyncSelection()
    {
        if (_sceneHierarchyTree == null || _selected == null || !IsInstanceValid(_selected)) return;
        ObjectId selectedObjectId = V1013ObjectId(_selected);
        if (!_sceneHierarchyItems.TryGetValue(selectedObjectId, out TreeItem? item)) return;
        if (_sceneHierarchyTree.GetSelected() == item) return;

        _sceneHierarchySyncing = true;
        try
        {
            item.Select(0);
        }
        finally
        {
            _sceneHierarchySyncing = false;
        }
    }

    void SceneHierarchyItemSelected()
    {
        if (_sceneHierarchySyncing || _sceneHierarchyTree == null) return;
        TreeItem? item = _sceneHierarchyTree.GetSelected();
        if (item == null) return;

        string objectIdText = item.GetMetadata(0).AsString();
        if (!Guid.TryParse(objectIdText, out Guid objectGuid)) return;
        var objectId = new ObjectId(objectGuid);
        MeshInstance3D? obj = V1020FindSceneObject(objectId);
        if (obj == null) return;

        // Selection remains authoritative in Main.Select; the hierarchy is presentation only.
        Select(obj);
        RebuildSceneList();
        V1017UpdateGizmo();
        SetStatus($"Selected: {obj.Name}");
    }

    string SceneHierarchyStableSignature(IReadOnlyList<MeshInstance3D> liveObjects)
    {
        var liveInstanceIds = liveObjects.Select(obj => obj.GetInstanceId()).ToHashSet();
        return string.Join("|", liveObjects.Select(obj =>
        {
            ObjectId objectId = V1013ObjectId(obj);
            ObjectId? parentObjectId = obj.GetParent() is MeshInstance3D parent &&
                                       liveInstanceIds.Contains(parent.GetInstanceId())
                ? V1013ObjectId(parent)
                : null;
            return $"{objectId}:{obj.Name}:{parentObjectId?.ToString() ?? "root"}";
        }));
    }
}
