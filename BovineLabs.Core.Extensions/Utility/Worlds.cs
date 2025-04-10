﻿// <copyright file="Worlds.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    public static class Worlds
    {
        public const WorldSystemFilterFlags Service = (WorldSystemFilterFlags)(1 << 21);

        public const WorldSystemFilterFlags ClientLocal = WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
            WorldSystemFilterFlags.ThinClientSimulation;

        public const WorldSystemFilterFlags ServerLocal = WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation;

        public const WorldSystemFilterFlags ServerLocalEditor = ServerLocal | WorldSystemFilterFlags.Editor;

        public const WorldSystemFilterFlags Simulation =
            WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation;

        public const WorldSystemFilterFlags SimulationService = Simulation | Service;

        public const WorldSystemFilterFlags SimulationThin = Simulation | WorldSystemFilterFlags.ThinClientSimulation;

        public const WorldSystemFilterFlags SimulationThinService = SimulationThin | Service;

        public const WorldSystemFilterFlags SimulationEditor = SimulationThin | WorldSystemFilterFlags.Editor;

        public const WorldSystemFilterFlags All = SimulationEditor | Service;

        public const WorldFlags ServiceWorld = (WorldFlags)(1 << 16) | WorldFlags.Live;
    }
}
