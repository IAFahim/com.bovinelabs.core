﻿// <copyright file="ObjectManagementProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.ObjectManagement;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary> An <see cref="AssetPostprocessor" /> that ensures <see cref="IUID" /> types always have a unique ID even if 2 branches merge. </summary>
    public class ObjectManagementProcessor : AssetPostprocessor
    {
        private static GlobalProcessor global = new();

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature", Justification = "Changed in 2021")]
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload || importedAssets.Length == 0)
            {
                return;
            }

            global = new GlobalProcessor();
            var processors = new Dictionary<Type, Processor>();
            var autoRefMap = new Dictionary<Type, AutoRefAttribute>();

            foreach (var assetPath in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                // Instead of just doing a LoadAssetsAtPath tHis helps us early out for all other type of assets
                if (asset == null)
                {
                    continue;
                }

                ProcessAsset(asset, processors, autoRefMap);
                foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                {
                    if (subAsset == null)
                    {
                        continue;
                    }

                    ProcessAsset(subAsset, processors, autoRefMap);
                }
            }

            foreach (var manager in autoRefMap)
            {
                UpdateAutoRef(manager.Key, manager.Value);
            }
        }

        private static void ProcessAsset(Object asset, Dictionary<Type, Processor> processors, Dictionary<Type, AutoRefAttribute> autoRefMap)
        {
            CheckAutoRef(asset, autoRefMap);
            if (CheckAutoID(asset, processors))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void CheckAutoRef(Object asset, Dictionary<Type, AutoRefAttribute> autoRefMap)
        {
            var type = asset.GetType();

            var attribute = type.GetCustomAttribute<AutoRefAttribute>();
            if (attribute == null)
            {
                return;
            }

            autoRefMap[type] = attribute;
        }

        private static bool CheckAutoID(Object asset, Dictionary<Type, Processor> processors)
        {
            switch (asset)
            {
                case IUIDGlobal:
                {
                    return global.Process(asset);
                }

                case IUID:
                {
                    var assetType = asset.GetType();
                    if (!processors.TryGetValue(assetType, out var processor))
                    {
                        processor = processors[assetType] = new Processor(assetType);
                    }

                    return processor.Process(asset);
                }
            }

            return false;
        }

        private static void UpdateAutoRef(Type type, AutoRefAttribute attribute)
        {
            var managerGuid = AssetDatabase.FindAssets($"t:{attribute.ManagerType}");
            if (managerGuid.Length == 0)
            {
                BLDebug.LogErrorString($"No manager found for {attribute.ManagerType}");
                return;
            }

            if (managerGuid.Length > 1)
            {
                BLDebug.LogErrorString($"More than one manager found for {attribute.ManagerType}");
                return;
            }

            var manager = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(managerGuid[0]));
            if (!manager)
            {
                BLDebug.LogErrorString("Manager wasn't a ScriptableObject");
                return;
            }

            var so = new SerializedObject(manager);
            var sp = so.FindProperty(attribute.FieldName);
            if (sp == null)
            {
                BLDebug.LogErrorString($"Property {attribute.FieldName} not found for {attribute.ManagerType}");
                return;
            }

            if (!sp.isArray)
            {
                BLDebug.LogErrorString($"Property {attribute.FieldName} was not type of array for {attribute.ManagerType}");
                return;
            }

            if (sp.arrayElementType != $"PPtr<${type.Name}>")
            {
                BLDebug.LogErrorString($"Property {attribute.FieldName} was not type of {type.Name} for {attribute.ManagerType}");
                return;
            }

            var objects = AssetDatabase
                .FindAssets($"t:{type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct() // In case multi of same type on same path
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(s => s != null && s.GetType() == type)
                .ToList();

            sp.arraySize = objects.Count;
            for (var i = 0; i < objects.Count; i++)
            {
                sp.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(manager);
        }

        private static int GetFirstFreeID(IReadOnlyDictionary<int, int> map)
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (!map.ContainsKey(i))
                {
                    return i;
                }
            }

            return 0; // You'd have to hit int.MaxValue ids to ever hit this case, you have other problems
        }

        private class Processor
        {
            private readonly string filter;
            private Dictionary<int, int>? map;

            public Processor(Type type)
            {
                this.Type = type;
                this.filter = $"t:{type.Name}";
            }

            public Type Type { get; }

            public bool Process(Object obj)
            {
                var asset = (IUID)obj;

                this.map ??= this.GetIDMap();
                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = newId;
                    this.map[newId] = 1;

                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }

            private Dictionary<int, int> GetIDMap()
            {
                var idMap = new Dictionary<int, int>();

                var paths = AssetDatabase.FindAssets(this.filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();

                foreach (var path in paths)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var asset in assets)
                    {
                        if (asset == null)
                        {
                            continue;
                        }

                        if (asset.GetType() != this.Type)
                        {
                            continue;
                        }

                        var uid = (IUID)asset;
                        idMap.TryGetValue(uid.ID, out var count);
                        count++;
                        idMap[uid.ID] = count;
                    }
                }

                return idMap;
            }
        }

        private class GlobalProcessor
        {
            private const string Filter = "t:ScriptableObject";
            private Dictionary<int, int>? map;

            public bool Process(Object obj)
            {
                var asset = (IUIDGlobal)obj;

                this.map ??= GetIDMap();
                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = newId;
                    this.map[newId] = 1;

                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }

            private static Dictionary<int, int> GetIDMap()
            {
                var idMap = new Dictionary<int, int>();

                var paths = AssetDatabase.FindAssets(Filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();

                foreach (var path in paths)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var asset in assets)
                    {
                        if (asset == null)
                        {
                            continue;
                        }

                        if (asset is not IUIDGlobal uid)
                        {
                            continue;
                        }

                        idMap.TryGetValue(uid.ID, out var count);
                        count++;
                        idMap[uid.ID] = count;
                    }
                }

                return idMap;
            }
        }
    }
}
