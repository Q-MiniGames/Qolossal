using System;
using System.Collections.Generic;
using UnityEngine;

// The Mountain Relief Atlas route as data: Resources/MRA/mra_world.json, converted from the design
// package (Tools/MountainReliefAtlas/convert_spec.py; SOURCE_TRACE.json records its hashes). The
// builder places every scene from it, and the Chart reads its map lines and landmark positions at
// run time. Region coordinates are local Unity units (x right, y up), one region per scene.
namespace Mra
{
    [Serializable] public sealed class Metrics { public float jumpRise, jumpGap, stepRise, landingWidth, anchorReach, headroom; }

    [Serializable] public sealed class Node
    {
        public string id, name, checkpointId, ability, knot, showWhen;
        public Vector2 position, spawn, chartUv;
        public float padWidth, padDepth, orthoSize;
        public bool checkpoint, waymark;
    }

    [Serializable] public sealed class Module
    {
        public string template, saveFlag;
        public Vector2 position, gateSize;
        public float bridgeWidth, gapWidth, anchorHeight, landingWidth, catchDrop, landingDrop, returnRise, amplitude, period;
        public bool Present => !string.IsNullOrEmpty(template);
    }

    [Serializable] public sealed class Edge
    {
        public string id, source, target, kind, surfaceKind;
        public Vector2[] points, chartLine;
        public float depth;
        public string[] requires;
        public Module module;
    }

    [Serializable] public sealed class Region
    {
        public string id, name, slug, kit, scene, map, grant, entry, exit, primaryWaymark, visibility;
        public int ordinal, grantAt, knotAt;
        public Vector2 size, cameraMin, cameraMax;
        public string[] landmarks, chamberIds;
        public Node[] nodes;
        public Edge[] edges;

        public Node NodeById(string nodeId) => Array.Find(nodes, n => n.id == nodeId);
        public bool PostReveal => visibility == "post_reveal_only";
    }

    [Serializable] public sealed class Chamber
    {
        public string id, name, region, type, reward, rewardId, entranceNode, prerequisite, presentation, scene, returnId, objective, solution;
        public Vector2 entrance, playerSpawn, returnSpawn, chartUv, size;
        public Vector4[] platforms, walls, gates, water, hazards, catchFloor;
        public Vector2[] switches, anchors, pods, plate, crate, lever, resident, rewardAt;

        public bool InPlace => presentation == "in-place-cutaway";
        /// <summary>A Qvale home (or the musician's loft): its own interior scene, entered by its door like a side chamber.</summary>
        public bool IsHouse => presentation == "house-instance";
        public string DiscoveredFlag => "mra:" + id + ":discovered";
    }

    [Serializable] public sealed class Link
    {
        public string id, source, target, sourceScene, targetScene;
        public Vector2 arrival, reverseArrival;
        public string[] requires;
    }

    [Serializable] public sealed class Encounter
    {
        public string id, region, edge, prefab, prefabSha;
        public Vector2 center, patrol;
        public int count, shelfWidth;
    }

    [Serializable] public sealed class World
    {
        public int schema;
        public string source, specSha256, chartSha256, encounterSha256, revealFlag;
        public Metrics metrics;
        public Region[] regions;
        public Chamber[] chambers;
        public Link[] links;
        public Encounter[] encounters;

        static World loaded;
        public static World Load()
        {
            if (loaded != null) return loaded;
            var text = Resources.Load<TextAsset>("MRA/mra_world");
            return loaded = text != null ? JsonUtility.FromJson<World>(text.text) : null;
        }
#if UNITY_EDITOR
        // The builder rereads the file after the converter runs.
        public static void Forget() => loaded = null;
#endif

        public Region RegionById(string id) => Array.Find(regions, r => r.id == id);
        public Region RegionByScene(string scene) => Array.Find(regions, r => r.scene == scene);
        public Chamber ChamberById(string id) => Array.Find(chambers, c => c.id == id);
        public Chamber ChamberByScene(string scene) => Array.Find(chambers, c => c.scene == scene);
        public IEnumerable<Chamber> ChambersOf(string region) { foreach (var c in chambers) if (c.region == region) yield return c; }
    }
}
