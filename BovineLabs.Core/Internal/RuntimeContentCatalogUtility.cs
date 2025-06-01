// <copyright file="RuntimeContentCatalogUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System.Collections.Generic;
    using System.IO;
    using Unity.Entities;
    using Unity.Entities.Content;
    using Unity.Entities.Serialization;

    public static class RuntimeContentCatalogUtility
    {
        public static List<WeakObjectSceneReference> GetScenes(string catalogPath)
        {
            var scenes = new List<WeakObjectSceneReference>();

            if (!string.IsNullOrEmpty(catalogPath) && BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out var catalogData))
            {

                for (var i = 0; i < catalogData.Value.Scenes.Length; i++)
                {
                    var sceneId = catalogData.Value.Scenes[i].SceneId;
                    var untyped = new UntypedWeakReferenceId
                    {
                        GlobalId = sceneId.GlobalId,
                        GenerationType = sceneId.GenerationType,
                    };

                    scenes.Add(new WeakObjectSceneReference { Id = untyped });
                }


                catalogData.Dispose();
            }

            return scenes;
        }
    }
}
