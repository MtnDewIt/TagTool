using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Geometry;
using TagTool.Geometry.BspCollisionGeometry;
using TagTool.Geometry.Utils;
using TagTool.Havok;
using TagTool.IO;
using TagTool.Serialization;
using TagTool.Tags;
using TagTool.Tags.Definitions;
using TagTool.Tags.Definitions.Common;
using TagTool.Tags.Resources;
using static TagTool.Porting.Gen2.Gen2BspGeometryConverter;
using static TagTool.Tags.Definitions.Scenario;
using static TagTool.Tags.Definitions.Scenario.SpawnDatum;
using static TagTool.Tags.Definitions.Scenario.ZoneSetPvsBlock;
using Gen2Scenario = TagTool.Tags.Definitions.Gen2.Scenario;
using Gen2ScenarioStructureBsp = TagTool.Tags.Definitions.Gen2.ScenarioStructureBsp;

namespace TagTool.Porting.Gen2
{
    partial class PortingContextGen2
    {
        List<List<Gen2BSPResourceMesh>> bspMeshes = new List<List<Gen2BSPResourceMesh>>();

        private void ConvertNetgameFlags(Gen2Scenario rawgen2tag, Scenario newScenario)
        {
            if (newScenario.MapType == ScenarioMapType.Multiplayer)
            {
                // TODO make new tags that are equivalent to these but don't have a render model
                // TODO fix teleporters

                sbyte ballCount = 0;
                var territoryIdentifiers = new List<short>();
                var kothBorders = new Dictionary<Gen2Scenario.ScenarioNetpointsBlock.TypeValue, List<RealPoint3d>>();

                foreach (var netgameFlagsBlock in rawgen2tag.NetgameFlags)
                {
                    CrateInstance instance = new CrateInstance()
                    {
                        ObjectType = new GameObjectType8() { Halo3ODST = GameObjectTypeHalo3ODST.Crate },
                        NameIndex = -1,
                        Source = ScenarioInstance.SourceValue.Editor,
                        EditorFolder = -1,
                        ParentId = new ScenarioObjectParentStruct() { NameIndex = -1 },
                        UniqueHandle = new DatumHandle(0x0),
                        OriginBspIndex = -1,
                        CanAttachToBspFlags = (ushort)(1u << 0)
                    };

                    var mpProperties = new MultiplayerObjectProperties
                    {
                        Team = (MultiplayerTeamDesignator)netgameFlagsBlock.TeamDesignator
                    };

                    CachedTag objectiveItem = null;
                    switch (netgameFlagsBlock.Type)
                    {
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.OddballSpawn:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\oddball\oddball_ball_spawn_point", out objectiveItem);
                            mpProperties.SpawnOrder = ballCount;
                            ballCount += 1;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.CtfFlagSpawn
                        when (mpProperties.Team != MultiplayerTeamDesignator.Neutral):
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\ctf\ctf_flag_spawn_point", out objectiveItem);
                            netgameFlagsBlock.Position.Z -= 0.05f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.CtfFlagReturn
                        when (mpProperties.Team != MultiplayerTeamDesignator.Neutral):
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\ctf\ctf_flag_return_area", out objectiveItem);
                            netgameFlagsBlock.Position.Z -= 0.03f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.AssaultBombSpawn:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\assault\assault_bomb_spawn_point", out objectiveItem);
                            netgameFlagsBlock.Position.Z -= 0.063f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.AssaultBombReturn:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\assault\assault_bomb_goal_area", out objectiveItem);
                            if (netgameFlagsBlock.Identifier > 0)
                                continue;
                            //netgameFlagsBlock.Position.Z -= 0.038f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.TeleporterSrc:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\teleporter_sender\teleporter_sender", out objectiveItem);
                            mpProperties.TeleporterChannel = (sbyte)netgameFlagsBlock.Identifier;
                            mpProperties.Team = MultiplayerTeamDesignator.Neutral;
                            netgameFlagsBlock.Position.Z -= 0.35f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.TeleporterDest:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\teleporter_reciever\teleporter_reciever", out objectiveItem);
                            mpProperties.TeleporterChannel = (sbyte)netgameFlagsBlock.Identifier;
                            mpProperties.Team = MultiplayerTeamDesignator.Neutral;
                            // hack: replacement (gen3) teleporters need to be offset from walls
                            // shift an arbitrary distance along the direction teleporter is facing
                            // preferable to adapt gen2 teleporters instead in the future
                            float d = 0.2f;
                            netgameFlagsBlock.Position.X = (float)(netgameFlagsBlock.Position.X + d * Math.Cos(netgameFlagsBlock.Facing.Radians));
                            netgameFlagsBlock.Position.Y = (float)(netgameFlagsBlock.Position.Y + d * Math.Sin(netgameFlagsBlock.Facing.Radians));
                            netgameFlagsBlock.Position.Z -= 0.35f;
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.TerritoriesFlag:
                            CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\territories\territory_static", out objectiveItem);
                            if (territoryIdentifiers.Contains(netgameFlagsBlock.Identifier))
                                continue;
                            netgameFlagsBlock.Position.Z -= 0.06f;
                            mpProperties.Team = MultiplayerTeamDesignator.Neutral;
                            mpProperties.Shape = MultiplayerObjectBoundaryShape.Cylinder;
                            mpProperties.BoundaryPositiveHeight = 1.0f;
                            mpProperties.BoundaryNegativeHeight = 0.25f;
                            mpProperties.BoundaryWidthRadius = 2.30f;
                            mpProperties.SpawnOrder = (sbyte)netgameFlagsBlock.Identifier;
                            territoryIdentifiers.Add(netgameFlagsBlock.Identifier);
                            break;
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill0:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill1:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill2:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill3:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill4:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill5:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill6:
                        case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.KingHill7:
                            {
                                if (!kothBorders.ContainsKey(netgameFlagsBlock.Type))
                                    kothBorders.Add(netgameFlagsBlock.Type, new List<RealPoint3d> { });

                                kothBorders[netgameFlagsBlock.Type].Add(netgameFlagsBlock.Position);
                            }
                            break;
                    }

                    if (objectiveItem == null)
                        continue;

                    instance.PaletteIndex = GetPaletteIndexOrAdd(newScenario, objectiveItem);
                    instance.Position = netgameFlagsBlock.Position;
                    instance.Rotation.YawValue = netgameFlagsBlock.Facing.Radians;
                    instance.Multiplayer = mpProperties;

                    SetSymmetryFlags(netgameFlagsBlock, instance);

                    newScenario.Crates.Add(instance);
                }

                FixupKothData(newScenario, kothBorders);
            }
        }

        private short GetPaletteIndexOrAdd(Scenario scnr, CachedTag tag)
        {
            if (tag == null)
                return -1;

            List<ScenarioPaletteEntry> palette = scnr.CratePalette;
            if (tag.Group.Tag.ToString() == "scen")
                palette = scnr.SceneryPalette;

            short index = (short)palette.FindIndex(c => c.Object == tag);
            if (index != -1)
                return index;
            else
            {
                palette.Add(new ScenarioPaletteEntry { Object = tag });
                return (short)(palette.Count - 1);
            }
        }

        private void SetSymmetryFlags(Gen2Scenario.ScenarioNetpointsBlock netgameFlags, ScenarioInstance instance)
        {
            var mpInstance = instance as IMultiplayerInstance;
            if (instance == null)
                return;

            var symmetryFlags = netgameFlags.Flags;
            switch (netgameFlags.Type)
            {
                case Gen2Scenario.ScenarioNetpointsBlock.TypeValue.AssaultBombSpawn 
                when mpInstance.Multiplayer.Team == MultiplayerTeamDesignator.Neutral:
                    mpInstance.Multiplayer.Symmetry |= GameEngineSymmetry.Symmetric;
                    break;

                default:
                    {
                        if (symmetryFlags.HasFlag(Gen2Scenario.ScenarioNetpointsBlock.FlagsValue.MultipleFlagBomb)
                            || symmetryFlags.HasFlag(Gen2Scenario.ScenarioNetpointsBlock.FlagsValue.NeutralFlagBomb))
                            mpInstance.Multiplayer.Symmetry |= GameEngineSymmetry.Symmetric;
                        if (symmetryFlags.HasFlag(Gen2Scenario.ScenarioNetpointsBlock.FlagsValue.SingleFlagBomb))
                            mpInstance.Multiplayer.Symmetry |= GameEngineSymmetry.Asymmetric;
                        if ((int)mpInstance.Multiplayer.Symmetry > 2)
                            mpInstance.Multiplayer.Symmetry = GameEngineSymmetry.Ignore;
                        break;
                    }
            }
        }

        private void FixupKothData(Scenario newScenario, Dictionary<Gen2Scenario.ScenarioNetpointsBlock.TypeValue, List<RealPoint3d>> kothBorders)
        {
            short paletteIndex = -1;
            sbyte spawnOrder = 0;

            if (kothBorders.Any())
            {
                CacheContext.TagCache.TryGetTag<Crate>(@"objects\multi\koth\koth_hill_static", out var objectiveItem);
                paletteIndex = GetPaletteIndexOrAdd(newScenario, objectiveItem);
            }

            foreach(var hillPoint in kothBorders)
            {
                ScenarioInstance instance = new CrateInstance
                {
                    ObjectType = new GameObjectType8() { Halo3ODST = GameObjectTypeHalo3ODST.Crate },
                    PaletteIndex = paletteIndex,
                    NameIndex = -1,
                    Source = ScenarioInstance.SourceValue.Editor,
                    EditorFolder = -1,
                    ParentId = new ScenarioObjectParentStruct() { NameIndex = -1 },
                    UniqueHandle = new DatumHandle(0x0),
                    OriginBspIndex = -1,
                    CanAttachToBspFlags = (ushort)(1u << 0),
                    Position = GetCenter(hillPoint.Value, out float widthRadius, out float length),
                    Multiplayer = new MultiplayerObjectProperties()
                    {
                        SpawnOrder = spawnOrder,
                        Team = MultiplayerTeamDesignator.Neutral,
                        Shape = length > 0.0f ? MultiplayerObjectBoundaryShape.Box : MultiplayerObjectBoundaryShape.Cylinder,
                        BoundaryPositiveHeight = 1.0f,
                        BoundaryNegativeHeight = 0.25f,
                        BoundaryWidthRadius = widthRadius,
                        BoundaryBoxLength = length
                    }
                };

                newScenario.Crates.Add(instance as CrateInstance);
                spawnOrder++;
            }
        }

        private RealPoint3d GetCenter(List<RealPoint3d> points, out float widthRadius, out float length)
        {
            RealPoint3d center = new RealPoint3d
            {
                X = points.Average(point => point.X),
                Y = points.Average(point => point.Y),
                Z = points.Average(point => point.Z)
            };

            List<float> distances = new List<float>();

            foreach (RealPoint3d point in points)
                distances.Add(RealPoint3d.Distance(point - center));

            widthRadius = distances.Max();
            // to-do: determine roundness to alternately use box-shape and length
            length = 0.0f;

            return center;
        }

        public TagStructure ConvertScenario(Gen2Scenario gen2Tag, Gen2Scenario rawgen2Tag, string scenarioPath
            , Stream cacheStream, Stream gen2CacheStream)
        {
            Scenario newScenario = new Scenario();
            AutoConverter.InitTagBlocks(newScenario);

            //default values for now, pulled from valhalla
            CacheContext.TagCache.TryGetTag<Wind>(@"levels\multi\riverworld\wind_riverworld", out var windTag);
            CacheContext.TagCache.TryGetTag<Bitmap>(@"levels\multi\riverworld\riverworld_riverworld_cubemaps", out var cubemapsTag);
            CacheContext.TagCache.TryGetTag<CameraFxSettings>(@"levels\multi\riverworld\riverworld", out var cfxsTag);
            CacheContext.TagCache.TryGetTag<SkyAtmParameters>(@"levels\multi\riverworld\sky\riverworld", out var skyaTag);
            CacheContext.TagCache.TryGetTag<ChocolateMountainNew>(@"levels\multi\riverworld\riverworld", out var chmtTag);
            CacheContext.TagCache.TryGetTag<PerformanceThrottles>(@"levels\multi\riverworld\riverworld", out var perfTag);

            newScenario.DefaultCameraFx = cfxsTag;
            newScenario.SkyParameters = skyaTag;
            newScenario.GlobalLighting = chmtTag;
            newScenario.PerformanceThrottles = perfTag;

            //mapid and type
            newScenario.MapType = (ScenarioMapType)gen2Tag.Type;
            switch (newScenario.MapType)
            {
                case ScenarioMapType.Multiplayer:
                    newScenario.MapId = gen2Tag.LevelData[0].Multiplayer[0].MapId;
                    newScenario.CampaignId = -1;
                    break;
                case ScenarioMapType.SinglePlayer:
                    if (gen2Tag.LevelData[0].CampaignLevelData.Count > 0)
                    {
                        newScenario.MapId = gen2Tag.LevelData[0].CampaignLevelData[0].MapId;
                        newScenario.CampaignId = gen2Tag.LevelData[0].CampaignLevelData[0].CampaignId;
                    }
                    break;
            }

            if (newScenario.MapId < 0)
                newScenario.MapId = CreateMapID(scenarioPath);

            // Starting Profiles
            AutoConverter.TranslateList(gen2Tag.PlayerStartingProfile, newScenario.PlayerStartingProfile);

            if (newScenario.MapType == ScenarioMapType.Multiplayer)
                ConfigurePlayerStartingProfile(newScenario, gen2Tag);

            //soft surfaces
            newScenario.SoftSurfaces = new List<Scenario.SoftSurfaceBlock> { new Scenario.SoftSurfaceBlock() };


            for (var i = 0; i < gen2Tag.StructureBsps.Count; i++)
            {
                newScenario.ZoneSets.Add(new Scenario.ZoneSet
                {
                    Name = CacheContext.StringTable.GetStringId("default"),
                    AudibilityIndex = -1
                });

                ScenarioStructureBsp currentbsp = CacheContext.Deserialize<ScenarioStructureBsp>(cacheStream, gen2Tag.StructureBsps[i].StructureBsp);

                //bsps
                newScenario.StructureBsps.Add(new Scenario.StructureBspBlock
                {
                    StructureBsp = gen2Tag.StructureBsps[i].StructureBsp,
                    Flags = (Scenario.StructureBspBlock.StructureBspFlags)32,
                    DefaultSkyIndex = -1,
                    Cubemap = cubemapsTag,
                    Wind = windTag
                });

                //zoneset pvs
                newScenario.ZoneSetPvs.Add(new Scenario.ZoneSetPvsBlock());
                newScenario.ZoneSetPvs[i].StructureBspMask = (Scenario.BspFlags)(((int)newScenario.ZoneSetPvs[0].StructureBspMask) | (1 << i));
                newScenario.ZoneSetPvs[i].StructureBspPvs = new List<Scenario.ZoneSetPvsBlock.BspPvsBlock>();
                newScenario.ZoneSetPvs[i].StructureBspPvs.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock());
                AutoConverter.InitTagBlocks(newScenario.ZoneSetPvs[i].StructureBspPvs[0]);

                var allClustersBitVector = new BitVectorDword[(currentbsp.Clusters.Count + 31) >> 5];
                for (var k = 0; k < currentbsp.Clusters.Count; k++)
                    (allClustersBitVector[k >> 5] ??= new()).Bits |= (BitVectorDword.DwordBits)(1u << (k & 31));

                var connectedClusters = currentbsp.Clusters.Select((_, i) => (byte)i );

                for (var j = 0; j < currentbsp.Clusters.Count; j++)
                {
                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].ClusterPvs.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock
                    {
                        ClusterPvsBitVectors = new List<Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock.CluserPvsBitVectorBlock>
                    {
                        new Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock.CluserPvsBitVectorBlock
                        {
                            Bits = [..allClustersBitVector]
                        }
                    }
                    });
                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].ClusterPvsDoorsClosed.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock
                    {
                        ClusterPvsBitVectors = new List<Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock.CluserPvsBitVectorBlock>
                    {
                        new Scenario.ZoneSetPvsBlock.BspPvsBlock.ClusterPvsBlock.CluserPvsBitVectorBlock
                        {
                            Bits = [..allClustersBitVector]
                        }
                    }
                    });

                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].AttachedSkyIndices.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock.SkyIndicesBlock());
                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].VisibleSkyIndices.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock.SkyIndicesBlock());
                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].ClusterMappings.Add(new Scenario.ZoneSetPvsBlock.BspPvsBlock.BspSeamClusterMapping() { Clusters = [.. connectedClusters] });
                    newScenario.ZoneSetPvs[i].StructureBspPvs[0].ClusterAudioBitvector.Add(new Scenario.ZoneSetPvsBlock.BitVectorDword());
                }

                //zonesets
                newScenario.ZoneSets[i].Bsps = (Scenario.BspFlags)(1 << i);
                newScenario.ZoneSetPvs[i].PortaldeviceMapping = new List<Scenario.ZoneSetPvsBlock.PortalDeviceMappingBlock>
                {
                    new Scenario.ZoneSetPvsBlock.PortalDeviceMappingBlock()
                };
            }

            //skies
            int bspbits = 0;
            for (var b = 0; b < gen2Tag.StructureBsps.Count; b++)
            {
                bspbits |= 1 << b;
            }
            foreach (var gen2sky in rawgen2Tag.Skies)
            {
                if (gen2sky.Sky != null)
                {
                    string skytagname = gen2sky.Sky.Name;

                    var gen2skytag = BlamCache.Deserialize<TagTool.Tags.Definitions.Gen2.Sky>(gen2CacheStream, gen2sky.Sky);
                    CachedTag skymodetag = null;
                    if (gen2skytag.RenderModel != null)
                    {
                        skymodetag = ConvertTag(cacheStream, gen2CacheStream, gen2skytag.RenderModel);

                        //fixup skymodetag with gen2 sky render model scale
                        RenderModel skymode = CacheContext.Deserialize<RenderModel>(cacheStream, skymodetag);

                        skymode.Nodes[0].DefaultScale *= (gen2skytag.RenderModelScale / 2);

                        CacheContext.Serialize(cacheStream, skymodetag, skymode);
                    }

                    var newmodel = new Model
                    {
                        RenderModel = skymodetag
                    };
                    CachedTag newmodeltag = CacheContext.TagCache.AllocateTag<Model>($"{skytagname}");
                    CacheContext.Serialize(cacheStream, newmodeltag, newmodel);
                    var newscen = new Scenery
                    {
                        BoundingRadius = 5555.0f,
                        ObjectType = new GameObjectType16 { Halo3ODST = GameObjectTypeHalo3ODST.Scenery },
                        Model = newmodeltag
                    };
                    CachedTag newscentag = CacheContext.TagCache.AllocateTag<Scenery>($"{skytagname}");
                    CacheContext.Serialize(cacheStream, newscentag, newscen);
                    newScenario.SkyReferences.Add(new Scenario.SkyReference
                    {
                        SkyObject = newscentag,
                        NameIndex = -1,
                        ActiveBsps = (Scenario.BspShortFlags)bspbits
                    });
                }
            }

            ConvertScenarioPlacements(gen2Tag, rawgen2Tag, newScenario, gen2CacheStream, cacheStream);

            newScenario.Lightmap = ConvertLightmap(rawgen2Tag, newScenario, scenarioPath, cacheStream, gen2CacheStream);

            return newScenario;
        }

        private int CreateMapID(string scenarioPath)
        {
            byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(scenarioPath));
            int output = BitConverter.ToInt32(encoded, 0) % 1000000;
            if (output < 0)
                output *= -1;

            return output;
        }

        private void ConfigurePlayerStartingProfile(Scenario scnr, Gen2Scenario gen2scnr)
        {
            bool noGrenades = false;
            bool plasmaGrenades = false;

            if (gen2scnr.StartingEquipment?.Count() > 0)
            {
                noGrenades = gen2scnr.StartingEquipment[0].Flags.HasFlag(Gen2Scenario.ScenarioStartingEquipmentBlock.FlagsValue.NoGrenades);
                plasmaGrenades = gen2scnr.StartingEquipment[0].Flags.HasFlag(Gen2Scenario.ScenarioStartingEquipmentBlock.FlagsValue.PlasmaGrenades);
            }

            //get from globals later maybe
            byte grenadeCount = 2;

            if (scnr.PlayerStartingProfile == null || scnr.PlayerStartingProfile.Count == 0)
            {
                scnr.PlayerStartingProfile = new List<Scenario.PlayerStartingProfileBlock>() {
                    new Scenario.PlayerStartingProfileBlock() {
                        Name = "start_assault",
                        PrimaryWeapon = CacheContext.TagCache.GetTag(@"objects\weapons\rifle\assault_rifle\assault_rifle", "weap"),
                        PrimaryRoundsLoaded = 32,
                        PrimaryRoundsTotal = 108,
                        StartingFragGrenadeCount = (noGrenades || plasmaGrenades) ? (byte)0 : grenadeCount,
                        StartingPlasmaGrenadeCount = (plasmaGrenades && !noGrenades) ? grenadeCount : (byte)0
                    }
                };
            }
            else
            {
                if (string.IsNullOrEmpty(scnr.PlayerStartingProfile[0].Name))
                    scnr.PlayerStartingProfile[0].Name = "start_assault";
        
                if (scnr.PlayerStartingProfile[0].PrimaryWeapon == null)
                    scnr.PlayerStartingProfile[0].PrimaryWeapon = CacheContext.TagCache.GetTag(@"objects\weapons\rifle\assault_rifle\assault_rifle", "weap");
            }
        }
     
        public void ConvertScenarioPlacements(Gen2Scenario gen2Tag, Gen2Scenario rawgen2tag,
            Scenario newScenario, Stream gen2CacheStream, Stream cacheStream)
        {

            List<uint> UniqueIDList = new List<uint>();

            // Object names
            foreach (var objname in gen2Tag.ObjectNames)
            {
                newScenario.ObjectNames.Add(new Scenario.ObjectName {
                    Name = objname.Name,
                    ObjectType = objname.ObjectType,
                    PlacementIndex = objname.PlacementIndex
                });
            }

            // Device Groups
            foreach (var devgroup in gen2Tag.DeviceGroups)
            {
                newScenario.DeviceGroups.Add(new Scenario.DeviceGroup
                {
                    Name = devgroup.Name,
                    InitialValue = devgroup.InitialValue,
                    Flags = (Scenario.DeviceGroupFlags)devgroup.Flags
                });
            }

            // Device machines
            foreach (var macpal in gen2Tag.MachinePalette)
            {
                newScenario.MachinePalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = macpal.Name
                });
            }
            for (var machobjindex = 0; machobjindex < gen2Tag.Machines.Count; machobjindex++)
            {
                var machobj = gen2Tag.Machines[machobjindex];
                newScenario.Machines.Add(new Scenario.MachineInstance
                {
                    PaletteIndex = machobj.Type,
                    NameIndex = machobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)machobj.ObjectData.PlacementFlags },
                    Position = machobj.ObjectData.Position,
                    Rotation = machobj.ObjectData.Rotation,
                    Scale = machobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)machobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)machobj.ObjectData.ManualBspFlags,

                    CanAttachToBspFlags = (ushort)(machobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.ScenarioInstance.SourceValue)machobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)machobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Machine },
                    //extra device/machine flags
                    DeviceFlags = (Scenario.ScenarioDeviceFlags)machobj.DeviceData.Flags,
                    MachineFlags = (Scenario.MachineInstance.ScenarioMachineFlags)machobj.MachineData.Flags,
                    PowerGroup = machobj.DeviceData.PowerGroup,
                    PositionGroup = machobj.DeviceData.PositionGroup
                });

                UniqueIDList.Add((uint)machobj.ObjectData.ObjectId.UniqueId);
            }

            // Device controls
            foreach (var ctrlpal in gen2Tag.ControlPalette)
            {
                newScenario.ControlPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = ctrlpal.Name
                });
            }
            for (var ctrlobjindex = 0; ctrlobjindex < gen2Tag.Controls.Count; ctrlobjindex++)
            {
                var ctrlobj = gen2Tag.Controls[ctrlobjindex];
                newScenario.Controls.Add(new Scenario.ControlInstance
                {
                    PaletteIndex = ctrlobj.Type,
                    NameIndex = ctrlobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)ctrlobj.ObjectData.PlacementFlags },
                    Position = ctrlobj.ObjectData.Position,
                    Rotation = ctrlobj.ObjectData.Rotation,
                    Scale = ctrlobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)ctrlobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)ctrlobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(ctrlobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.ScenarioInstance.SourceValue)ctrlobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)ctrlobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Control },
                    ControlFlags = (Scenario.ControlInstance.ScenarioControlFlags)ctrlobj.ControlData.Flags,
                    DeviceFlags = (Scenario.ScenarioDeviceFlags)ctrlobj.DeviceData.Flags,
                    PowerGroup = ctrlobj.DeviceData.PowerGroup,
                    PositionGroup = ctrlobj.DeviceData.PositionGroup
                });

                UniqueIDList.Add((uint)ctrlobj.ObjectData.ObjectId.UniqueId);
            }

            // Crates
            foreach (var blocpal in gen2Tag.CratesPalette)
            {
                newScenario.CratePalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = blocpal.Name
                });
            }
            for (var blocobjindex = 0; blocobjindex < gen2Tag.Crates.Count; blocobjindex++)
            {
                var crateobj = gen2Tag.Crates[blocobjindex];
                newScenario.Crates.Add(new Scenario.CrateInstance
                {
                    PaletteIndex = crateobj.Type,
                    NameIndex = crateobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)crateobj.ObjectData.PlacementFlags },
                    Position = crateobj.ObjectData.Position,
                    Rotation = crateobj.ObjectData.Rotation,
                    Scale = crateobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)crateobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)crateobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(crateobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.ScenarioInstance.SourceValue)crateobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)crateobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Crate },
                });

                UniqueIDList.Add((uint)crateobj.ObjectData.ObjectId.UniqueId);
            }

            // Scenery
            foreach (var scenpal in gen2Tag.SceneryPalette)
            {
                newScenario.SceneryPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = scenpal.Name
                });
            }
            int flagBaseIndex = newScenario.SceneryPalette.FindIndex(n => n.Object?.Name == "objects\\multi\\flag_base\\flag_base");
            gen2Tag.Scenery.RemoveAll(entry => entry.Type == (short)flagBaseIndex);
            for (var scenobjindex = 0; scenobjindex < gen2Tag.Scenery.Count; scenobjindex++)
            {
                var scenobj = gen2Tag.Scenery[scenobjindex];
                var scenery = new Scenario.SceneryInstance
                {
                    PaletteIndex = scenobj.Type,
                    NameIndex = scenobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)scenobj.ObjectData.PlacementFlags },
                    Position = scenobj.ObjectData.Position,
                    Rotation = scenobj.ObjectData.Rotation,
                    Scale = scenobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)scenobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)scenobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(scenobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.ScenarioInstance.SourceValue)scenobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)scenobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Scenery },
                    Multiplayer = new Scenario.MultiplayerObjectProperties(),
                    HavokMoppIndex = -1
                };
                newScenario.Scenery.Add(scenery);
                AutoConverter.TranslateEnum(gen2Tag.Scenery[scenobjindex].SceneryData.ValidMultiplayerGames, out scenery.Multiplayer.EngineFlags, scenery.Multiplayer.EngineFlags.GetType());

                UniqueIDList.Add((uint)scenobj.ObjectData.ObjectId.UniqueId);
            }

            // Bipeds
            foreach (var bipdpal in gen2Tag.BipedPalette)
            {
                newScenario.BipedPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = bipdpal.Name
                });
            }
            for (var bipdobjindex = 0; bipdobjindex < gen2Tag.Bipeds.Count; bipdobjindex++)
            {
                var bipdobj = gen2Tag.Bipeds[bipdobjindex];
                var biped = new Scenario.BipedInstance
                {
                    PaletteIndex = bipdobj.Type,
                    NameIndex = bipdobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)bipdobj.ObjectData.PlacementFlags },
                    Position = bipdobj.ObjectData.Position,
                    Rotation = bipdobj.ObjectData.Rotation,
                    Scale = bipdobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)bipdobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)bipdobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(bipdobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.BipedInstance.SourceValue)bipdobj.ObjectData.ObjectId.Source,
                    
                    UniqueHandle = new DatumHandle((uint)bipdobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Biped },
                    Variant = bipdobj.PermutationData.VariantName,
                    ActiveChangeColors = (Scenario.ActiveChangeColorFlags)bipdobj.PermutationData.ActiveChangeColors,

                    BodyVitalityFraction = bipdobj.UnitData.BodyVitality,
                    Flags = (Scenario.BipedInstance.ScenarioUnitDatumFlags)bipdobj.UnitData.Flags
                };
                newScenario.Bipeds.Add(biped);

                UniqueIDList.Add((uint)bipdobj.ObjectData.ObjectId.UniqueId);
            }

            // Weapons
            foreach (var weappal in gen2Tag.WeaponPalette)
            {
                newScenario.WeaponPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = weappal.Name
                });
            }
            for (var weapobjindex = 0; weapobjindex < gen2Tag.Weapons.Count; weapobjindex++)
            {
                var weapobj = gen2Tag.Weapons[weapobjindex];
                var weapon = new Scenario.WeaponInstance
                {
                    PaletteIndex = weapobj.Type,
                    NameIndex = weapobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)weapobj.ObjectData.PlacementFlags },
                    Position = weapobj.ObjectData.Position,
                    Rotation = weapobj.ObjectData.Rotation,
                    Scale = weapobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)weapobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)weapobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(weapobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.BipedInstance.SourceValue)weapobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)weapobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Weapon },
                    WeaponFlags = (Scenario.WeaponInstance.ScenarioWeaponDatumFlags)weapobj.WeaponData.Flags,
                };
                newScenario.Weapons.Add(weapon);

                UniqueIDList.Add((uint)weapobj.ObjectData.ObjectId.UniqueId);
            }

            // Equipment
            foreach (var eqippal in gen2Tag.EquipmentPalette)
            {
                newScenario.EquipmentPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = eqippal.Name
                });
            }
            for (var eqipobjindex = 0; eqipobjindex < gen2Tag.Equipment.Count; eqipobjindex++)
            {
                var eqipobj = gen2Tag.Equipment[eqipobjindex];
                var equipment = new Scenario.EquipmentInstance
                {
                    PaletteIndex = eqipobj.Type,
                    NameIndex = eqipobj.Name,
                    PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = (Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags)eqipobj.ObjectData.PlacementFlags },
                    Position = eqipobj.ObjectData.Position,
                    Rotation = eqipobj.ObjectData.Rotation,
                    Scale = eqipobj.ObjectData.Scale,
                    BspPolicy = (Scenario.ScenarioInstance.BspPolicyValue)eqipobj.ObjectData.BspPolicy,
                    OriginBspIndex = (short)eqipobj.ObjectData.ManualBspFlags,
                    CanAttachToBspFlags = (ushort)(eqipobj.ObjectData.ManualBspFlags + 1),
                    Source = (Scenario.BipedInstance.SourceValue)eqipobj.ObjectData.ObjectId.Source,
                    UniqueHandle = new DatumHandle((uint)eqipobj.ObjectData.ObjectId.UniqueId),
                    EditorFolder = -1,
                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Equipment },
                };
                newScenario.Equipment.Add(equipment);

                UniqueIDList.Add((uint)eqipobj.ObjectData.ObjectId.UniqueId);
            }

            // Player starting locations
            foreach (var startlocation in gen2Tag.PlayerStartingLocations)
            {
                newScenario.PlayerStartingLocations.Add(new Scenario.PlayerStartingLocation
                {
                    Position = startlocation.Position,
                    Facing = new RealEulerAngles2d(startlocation.Facing, Angle.FromDegrees(0.0f)),
                    EditorFolderIndex = -1
                });
            }

            // Spawn points from starting locations
            if (newScenario.MapType == ScenarioMapType.Multiplayer || newScenario.MapType == ScenarioMapType.SinglePlayer)
            {
                newScenario.SceneryPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = CacheContext.TagCache.GetTag<Scenery>(@"objects\multi\spawning\respawn_point")
                });
                bool prematchcameraset = false;
                int firstSpawnIndex = newScenario.Scenery.Count();
                foreach (var startlocation in newScenario.PlayerStartingLocations)
                {
                    var position = startlocation.Position;
                    position.Z -= 0.06f;
                    newScenario.Scenery.Add(new Scenario.SceneryInstance
                    {
                        PaletteIndex = (short)(newScenario.SceneryPalette.Count - 1),
                        NameIndex = -1,
                        Position = position,
                        Rotation = new RealEulerAngles3d(startlocation.Facing.Yaw, Angle.FromDegrees(0.0f), Angle.FromDegrees(0.0f)),
                        ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Scenery },
                        Source = Scenario.ScenarioInstance.SourceValue.Editor,
                        EditorFolder = -1,
                        ParentId = new ScenarioObjectParentStruct() { NameIndex = -1 },
                        UniqueHandle = new DatumHandle(0x0),
                        OriginBspIndex = -1,
                        CanAttachToBspFlags = (ushort)(1u << 0),
                        Multiplayer = new Scenario.MultiplayerObjectProperties() { Team = TagTool.Tags.Definitions.Common.MultiplayerTeamDesignator.Neutral },
                    });
                    if (!prematchcameraset)
                    {
                        newScenario.CutsceneCameraPoints.Add(new Scenario.CutsceneCameraPoint()
                        {
                            Position = startlocation.Position,
                            Orientation = new RealEulerAngles3d(startlocation.Facing.Yaw, Angle.FromDegrees(0.0f), Angle.FromDegrees(0.0f)),
                            Flags = Scenario.CutsceneCameraPointFlags.PrematchCameraHack,
                            Name = "prematch_camera",
                        });
                        prematchcameraset = true;
                    }
                }

                newScenario.SceneryPalette.Add(new Scenario.ScenarioPaletteEntry
                {
                    Object = CacheContext.TagCache.GetTag<Scenery>(@"objects\multi\spawning\respawn_point_invisible")
                });
                var invisibleSpawn = newScenario.Scenery[firstSpawnIndex].DeepCloneV2();
                invisibleSpawn.PaletteIndex = (short)(newScenario.SceneryPalette.Count() - 1);
                newScenario.Scenery.Add(invisibleSpawn);
            }

            //Spawn Zones
            AutoConverter.TranslateList(gen2Tag.SpawnData, newScenario.SpawnData);

            // Item Collection -> Weapon/Vehicle Placement
            uint uniqueid = UniqueIDList.Max() + 1;
            CachedTag paletteTag;
            TagTool.Tags.Definitions.Gen2.ItemCollection itemlayout = null;
            TagTool.Tags.Definitions.Gen2.VehicleCollection vehilayout = null;
            foreach (var NetgameEquipment in rawgen2tag.NetgameEquipment)
            {
                TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags EngineFlags = 0;
                ConvertH2GametypeFlags(ref EngineFlags, NetgameEquipment);

                if (NetgameEquipment.ItemVehicleCollection != null)
                {
                    object itemdef = BlamCache.Deserialize(gen2CacheStream, NetgameEquipment.ItemVehicleCollection);

                    switch (NetgameEquipment.ItemVehicleCollection.Group.ToString())
                    {
                        case "vehc":
                            vehilayout = (TagTool.Tags.Definitions.Gen2.VehicleCollection)itemdef;
                            if (vehilayout.VehiclePermutations[0].Vehicle != null)
                            {
                                ConvertTag(cacheStream, gen2CacheStream, vehilayout.VehiclePermutations[0].Vehicle);
                                if (!CacheContext.TagCache.TryGetCachedTag(vehilayout.VehiclePermutations[0].Vehicle.ToString(), out paletteTag))
                                    break;

                                var palette_index = newScenario.VehiclePalette.FindIndex(v => (v.Object == null ? "" : v.Object.Name) == vehilayout.VehiclePermutations[0].Vehicle.Name);
                                if (palette_index == -1)
                                {
                                    newScenario.VehiclePalette.Add(new Scenario.ScenarioPaletteEntry
                                    {
                                        Object = paletteTag
                                    });
                                    palette_index = newScenario.VehiclePalette.Count - 1;
                                }
                                newScenario.Vehicles.Add(new Scenario.VehicleInstance
                                {
                                    PaletteIndex = (short)palette_index,
                                    NameIndex = -1,
                                    Position = NetgameEquipment.Position,
                                    Rotation = NetgameEquipment.Orientation.Orientation,
                                    Scale = 1,
                                    BspPolicy = Scenario.ScenarioInstance.BspPolicyValue.Default,
                                    OriginBspIndex = 1,
                                    CanAttachToBspFlags = 1,
                                    Source = Scenario.ScenarioInstance.SourceValue.Editor,
                                    UniqueHandle = new DatumHandle(uniqueid),
                                    EditorFolder = -1,
                                    ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Vehicle },
                                    Multiplayer = new Scenario.MultiplayerObjectProperties
                                    {
                                        SpawnTime = NetgameEquipment.SpawnTimeInSeconds0Default,
                                        AbandonTime = NetgameEquipment.RespawnOnEmptyTime,
                                        EngineFlags = EngineFlags
                                    }
                                });
                            }
                            break;
                        default:
                            itemlayout = (TagTool.Tags.Definitions.Gen2.ItemCollection)itemdef;
                            if (itemlayout.ItemPermutations[0].Item != null)
                            {
                                ConvertTag(cacheStream, gen2CacheStream, itemlayout.ItemPermutations[0].Item);
                                if (!CacheContext.TagCache.TryGetCachedTag(itemlayout.ItemPermutations[0].Item.ToString(), out paletteTag))
                                    break;

                                if (itemlayout.ItemPermutations[0].Item.Group.ToString().Equals("weap"))
                                {
                                    // Convert weapon flags
                                    var WeaponFlags = Scenario.WeaponInstance.ScenarioWeaponDatumFlags.DoesAcceleratemovesDueToExplosions;
                                    if (NetgameEquipment.Flags.HasFlag(Gen2Scenario.ScenarioNetgameEquipmentBlock.FlagsValue.Levitate))
                                    {
                                        WeaponFlags |= Scenario.WeaponInstance.ScenarioWeaponDatumFlags.InitiallyAtRestdoesntFall;
                                    }
                                    var palette_index = newScenario.WeaponPalette.FindIndex(v => (v.Object == null ? "" : v.Object.Name) ==
                                    itemlayout.ItemPermutations[0].Item.Name);
                                    if (palette_index == -1)
                                    {
                                        newScenario.WeaponPalette.Add(new Scenario.ScenarioPaletteEntry
                                        {
                                            Object = paletteTag
                                        });
                                        palette_index = newScenario.WeaponPalette.Count - 1;
                                    }
                                    newScenario.Weapons.Add(new Scenario.WeaponInstance
                                    {
                                        PaletteIndex = (short)palette_index,
                                        NameIndex = -1,
                                        PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags.None},
                                        Position = NetgameEquipment.Position,
                                        Rotation = NetgameEquipment.Orientation.Orientation,
                                        Scale = 1,
                                        BspPolicy = Scenario.ScenarioInstance.BspPolicyValue.Default,
                                        OriginBspIndex = 1,
                                        CanAttachToBspFlags = 1,
                                        Source = Scenario.ScenarioInstance.SourceValue.Editor,
                                        UniqueHandle = new DatumHandle(uniqueid),
                                        EditorFolder = -1,
                                        ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Weapon },
                                        WeaponFlags = WeaponFlags,
                                        Multiplayer = new Scenario.MultiplayerObjectProperties
                                        {
                                            SpawnTime = NetgameEquipment.SpawnTimeInSeconds0Default,
                                            AbandonTime = NetgameEquipment.RespawnOnEmptyTime,
                                            EngineFlags = EngineFlags
                                        }
                                    });
                                }
                                else
                                {
                                    var palette_index = newScenario.EquipmentPalette.FindIndex(v => (v.Object == null ? "" : v.Object.Name) ==
                                    itemlayout.ItemPermutations[0].Item.Name);
                                    if (palette_index == -1)
                                    {
                                        newScenario.EquipmentPalette.Add(new Scenario.ScenarioPaletteEntry
                                        {
                                            Object = paletteTag
                                        });
                                        palette_index = newScenario.EquipmentPalette.Count - 1;
                                    }
                                    newScenario.Equipment.Add(new Scenario.EquipmentInstance
                                    {
                                        PaletteIndex = (short)palette_index,
                                        NameIndex = -1,
                                        PlacementFlags = new Scenario.ObjectPlacementFlags { Flags = Scenario.ObjectPlacementFlags.ObjectLocationPlacementFlags.None},
                                        Position = NetgameEquipment.Position,
                                        Rotation = NetgameEquipment.Orientation.Orientation,
                                        Scale = 1,
                                        BspPolicy = Scenario.ScenarioInstance.BspPolicyValue.Default,
                                        OriginBspIndex = 1,
                                        CanAttachToBspFlags = 1,
                                        Source = Scenario.ScenarioInstance.SourceValue.Editor,
                                        UniqueHandle = new DatumHandle(uniqueid),
                                        EditorFolder = -1,
                                        ObjectType = new GameObjectType8 { Halo3ODST = GameObjectTypeHalo3ODST.Equipment },
                                        Multiplayer = new Scenario.MultiplayerObjectProperties
                                        {
                                            SpawnTime = NetgameEquipment.SpawnTimeInSeconds0Default,
                                            AbandonTime = NetgameEquipment.RespawnOnEmptyTime,
                                            EngineFlags = EngineFlags
                                        }
                                    });
                                }
                            }
                            break;
                    }
                }
                uniqueid++;
            }

            ConvertNetgameFlags(rawgen2tag, newScenario);
            ConvertSpawnData(newScenario);

            // Trigger Volumes
            foreach (var vol in gen2Tag.KillTriggerVolumes)
            {
                newScenario.TriggerVolumes.Add(new Scenario.TriggerVolume
                {
                    Name = vol.Name,
                    ObjectName = vol.ObjectName,
                    NodeName = vol.NodeName,
                    Position = vol.Position,
                    Forward = vol.Forward,
                    Up = vol.Up,
                    Extents = vol.Extents,
                    KillVolume = vol.KillTriggerVolume
                });
            }

            // Bsp Switch -> ZoneSet Switch
            foreach (var switchvol in gen2Tag.BspSwitchTriggerVolumes)
            {
                newScenario.ZonesetSwitchTriggerVolumes.Add(new Scenario.ZoneSetSwitchTriggerVolume {
                    Flags = Scenario.ZoneSetSwitchTriggerVolume.FlagBits.TeleportVehicles,
                    BeginZoneSet = -1,
                    TriggerVolume = switchvol.TriggerVolume,
                    CommitZoneSet = switchvol.Destination
                });
            }

            // Kill Trigger Volumes
            foreach (var killvol in gen2Tag.ScenarioKillTriggers)
            {
                newScenario.ScenarioKillTriggers.Add(new Scenario.ScenarioKillTrigger
                {
                    TriggerVolume = killvol.TriggerVolume
                });
            }
        }

        private void ConvertSpawnData(Scenario newScenario)
        {
            var datum = newScenario.SpawnData?.First();
            if (datum == null)
                return;

            CacheContext.TagCache.TryGetTag<Scenery>(@"objects\multi\spawning\initial_spawn_point", out CachedTag initialSpawnItem);
            CacheContext.TagCache.TryGetTag<Scenery>(@"objects\multi\spawning\respawn_point", out CachedTag spawnItem);
            CacheContext.TagCache.TryGetTag<Scenery>(@"objects\multi\spawning\respawn_zone", out CachedTag zoneItem);

            // convert initial zones to spawns
            short initialSpawnIndex = GetPaletteIndexOrAdd(newScenario, initialSpawnItem);
            short spawnIndex = GetPaletteIndexOrAdd(newScenario, spawnItem);
            short zoneIndex = GetPaletteIndexOrAdd(newScenario, zoneItem);

            foreach (var initialZone in datum.StaticInitialSpawnZones)
            {
                if (initialZone.Weight < 0)
                    continue;

                short index = GetNearestSceneryIndex(initialZone.Position, newScenario.Scenery, spawnIndex);
                SceneryInstance initialSpawn = newScenario.Scenery[index].DeepCloneV2();

                initialSpawn.PaletteIndex = initialSpawnIndex;
                initialSpawn.OriginBspIndex = 0;
                initialSpawn.EditorFolder = 10;
                initialSpawn.Multiplayer.Team = GetTeamDesignator(initialZone);
                initialSpawn.Multiplayer.EngineFlags = (GameEngineSubTypeFlags)0x1FF; // any
                
                newScenario.Scenery.Add(initialSpawn);
            }

            // convert respawn zones
            if (zoneItem != null)
            {
                foreach (var block in datum.StaticRespawnZones)
                {
                    if (block.Weight < 0)
                        continue;

                    SceneryInstance instance = new SceneryInstance()
                    {
                        ObjectType = new GameObjectType8() { Halo3ODST = GameObjectTypeHalo3ODST.Scenery },
                        PaletteIndex = zoneIndex,
                        NameIndex = -1,
                        Source = ScenarioInstance.SourceValue.Editor,
                        EditorFolder = 9,
                        ParentId = new ScenarioObjectParentStruct() { NameIndex = -1 },
                        UniqueHandle = new DatumHandle(0x0),
                        OriginBspIndex = -1,
                        CanAttachToBspFlags = (ushort)(1u << 0),
                        Position = block.Position,
                        Multiplayer = new MultiplayerObjectProperties()
                        {
                            Team = GetTeamDesignator(block),
                            EngineFlags = (GameEngineSubTypeFlags)0x1E1, //CTF,Terries,Assault,VIP,Infection
                            Shape = MultiplayerObjectBoundaryShape.None,
                            BoundaryWidthRadius = block.OuterRadius,
                            BoundaryPositiveHeight = 1.5f,
                            BoundaryNegativeHeight = 1.5f
                        }
                    };

                    newScenario.Scenery.Add(instance);
                }

            }
        }

        private MultiplayerTeamDesignator GetTeamDesignator(SpawnZone block)
        {
            switch (block.Data.RelevantTeam.ToString())
            {
                case "RedAlpha":
                    return MultiplayerTeamDesignator.Red;
                case "BlueBravo":
                    return MultiplayerTeamDesignator.Blue;
                case "YellowCharlie":
                    return MultiplayerTeamDesignator.Green;
                case "GreenDelta":
                    return MultiplayerTeamDesignator.Orange;
                default:
                    return MultiplayerTeamDesignator.Neutral;
            }
        }

        private short GetNearestSceneryIndex(RealPoint3d position, List<SceneryInstance> list, short spawnIndex = -1)
        {
            float lowestDistance = float.MaxValue;
            short lowIndex = -1;

            for (short i = 0; i < list.Count; i++)
            {
                if (spawnIndex >= 0 && list[i].PaletteIndex != spawnIndex)
                    continue;

                float d = RealPoint3d.Distance(position - list[i].Position);
                if (d < lowestDistance)
                {
                    lowestDistance = d;
                    lowIndex = i;
                }
            }

            return lowIndex;
        }

        public List<TagHkpMoppCode> ConvertH2MOPP(byte[] moppdata)
        {
            var result = new List<TagHkpMoppCode>();

            using (var moppStream = new MemoryStream(moppdata))
            using (var moppReader = new EndianReader(moppStream, BlamCache.Endianness))
            {
                var context = new DataSerializationContext(moppReader);
                var deserializer = new TagDeserializer(BlamCache.Version, BlamCache.Platform);
                while (!moppReader.EOF)
                {
                    long startOffset = moppReader.Position;
                    Havok.Gen2.MoppCodeHeader moppHeader = deserializer.Deserialize<Havok.Gen2.MoppCodeHeader>(context);
                    byte[] moppCode = moppReader.ReadBytes((int)(moppHeader.Size - 0x30));
                    moppReader.SeekTo((startOffset + moppHeader.Size) + 0xF & ~0xF);

                    result.Add(new TagHkpMoppCode
                    {
                        Info = new CodeInfo
                        {
                            Offset = moppHeader.Offset
                        },
                        ArrayBase = new HkArrayBase
                        {
                            Size = (uint)moppCode.Length,
                            CapacityAndFlags = (uint)(moppCode.Length | 0x80000000)
                        },
                        Data = new TagBlock<byte>(CacheAddressType.Data)
                        {
                            Elements = moppCode.ToList()
                        }
                    });
                }
            }

            return result;
        }

        public byte[] ConvertH2MoppData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            byte[] result;
            using (var inputReader = new EndianReader(new MemoryStream(data), CacheVersionDetection.IsLittleEndian(BlamCache.Version, BlamCache.Platform) ? EndianFormat.LittleEndian : EndianFormat.BigEndian))
            using (var outputStream = new MemoryStream())
            using (var outputWriter = new EndianWriter(outputStream, CacheVersionDetection.IsLittleEndian(CacheContext.Version, CacheContext.Platform) ? EndianFormat.LittleEndian : EndianFormat.BigEndian))
            {
                var dataContext = new DataSerializationContext(inputReader, outputWriter);
                var deserializer = new TagDeserializer(BlamCache.Version, BlamCache.Platform);
                var serializer = new TagSerializer(CacheContext.Version, CacheContext.Platform);
                while (!inputReader.EOF)
                {
                    var header = deserializer.Deserialize<Havok.Gen2.MoppCodeHeader>(dataContext);
                    var dataSize = header.Size - 0x30;
                    var nextOffset = (inputReader.Position + dataSize) + 0xf & ~0xf;


                    List<byte> moppCodes = new List<byte>();
                    for (int j = 0; j < dataSize; j++)
                    {
                        moppCodes.Add(inputReader.ReadByte());
                    }
                    inputReader.SeekTo(nextOffset);

                    var newHeader = new HkpMoppCode
                    {
                        Info = new CodeInfo
                        {
                            Offset = header.Offset
                        },
                        ArrayBase = new HkArrayBase
                        {
                            Size = (uint)moppCodes.Count,
                            CapacityAndFlags = (uint)(moppCodes.Count | 0x80000000)
                        }
                    };

                    serializer.Serialize(dataContext, newHeader);
                    for (int j = 0; j < moppCodes.Count; j++)
                        outputWriter.Write(moppCodes[j]);

                    StreamUtil.Align(outputStream, 0x10);
                }
                result = outputStream.ToArray();
            }
            return result;
        }

        void ConvertGen2EnvironmentMopp(ScenarioStructureBsp sbsp)
        {
            if (sbsp.Physics.CollisionMoppCodes.Count == 0)
                return;

            var data = sbsp.Physics.CollisionMoppCodes[0].Data;
            for (int i = 0; i < data.Count; i++)
            {
                switch (data[i])
                {
                    case 0x00: // HK_MOPP_RETURN
                        break;
                    case 0x05: // HK_MOPP_JUMP8
                    case 0x09: // HK_MOPP_TERM_REOFFSET8
                    case 0x50: // HK_MOPP_TERM8
                    case 0x54: // HK_MOPP_NTERM_8
                    case 0x60: // HK_MOPP_PROPERTY8_0
                    case 0x61: // HK_MOPP_PROPERTY8_1
                    case 0x62: // HK_MOPP_PROPERTY8_2
                    case 0x63: // HK_MOPP_PROPERTY8_3
                        i += 1;
                        break;
                    case 0x06: // HK_MOPP_JUMP16
                    case 0x0A: // HK_MOPP_TERM_REOFFSET16
                    case 0x0C: // HK_MOPP_JUMP_CHUNK
                    case 0x20: // HK_MOPP_SINGLE_SPLIT_X
                    case 0x21: // HK_MOPP_SINGLE_SPLIT_Y
                    case 0x22: // HK_MOPP_SINGLE_SPLIT_Z
                    case 0x26: // HK_MOPP_DOUBLE_CUT_X
                    case 0x27: // HK_MOPP_DOUBLE_CUT_Y
                    case 0x28: // HK_MOPP_DOUBLE_CUT_Z
                    case 0x51: // HK_MOPP_TERM16
                    case 0x55: // HK_MOPP_NTERM_16
                    case 0x64: // HK_MOPP_PROPERTY16_0
                    case 0x65: // HK_MOPP_PROPERTY16_1
                    case 0x66: // HK_MOPP_PROPERTY16_2
                    case 0x67: // HK_MOPP_PROPERTY16_3
                        i += 2;
                        break;
                    case 0x01: // HK_MOPP_SCALE1
                    case 0x02: // HK_MOPP_SCALE2
                    case 0x03: // HK_MOPP_SCALE3
                    case 0x04: // HK_MOPP_SCALE4
                    case 0x07: // HK_MOPP_JUMP24
                    case 0x10: // HK_MOPP_SPLIT_X
                    case 0x11: // HK_MOPP_SPLIT_Y
                    case 0x12: // HK_MOPP_SPLIT_Z
                    case 0x13: // HK_MOPP_SPLIT_YZ
                    case 0x14: // HK_MOPP_SPLIT_YMZ
                    case 0x15: // HK_MOPP_SPLIT_XZ
                    case 0x16: // HK_MOPP_SPLIT_XMZ
                    case 0x17: // HK_MOPP_SPLIT_XY
                    case 0x18: // HK_MOPP_SPLIT_XMY
                    case 0x19: // HK_MOPP_SPLIT_XYZ
                    case 0x1A: // HK_MOPP_SPLIT_XYMZ
                    case 0x1B: // HK_MOPP_SPLIT_XMYZ
                    case 0x1C: // HK_MOPP_SPLIT_XMYMZ
                    case 0x52: // HK_MOPP_TERM24
                    case 0x56: // HK_MOPP_NTERM_24
                        i += 3;
                        break;
                    case 0x57: // HK_MOPP_NTERM_32
                    case 0x68: // HK_MOPP_PROPERTY32_0
                    case 0x69: // HK_MOPP_PROPERTY32_1
                    case 0x6A: // HK_MOPP_PROPERTY32_2
                    case 0x6B: // HK_MOPP_PROPERTY32_3
                        i += 4;
                        break;
                    case 0x0D: // HK_MOPP_DATA_OFFSET
                        i += 5;
                        break;
                    case 0x23: // HK_MOPP_SPLIT_JUMP_X
                    case 0x24: // HK_MOPP_SPLIT_JUMP_Y
                    case 0x25: // HK_MOPP_SPLIT_JUMP_Z
                    case 0x29: // HK_MOPP_DOUBLE_CUT24_X
                    case 0x2A: // HK_MOPP_DOUBLE_CUT24_Y
                    case 0x2B: // HK_MOPP_DOUBLE_CUT24_Z
                        i += 6;
                        break;
                    case byte op when op >= 0x30 && op <= 0x4F:
                        break;
                    case 0x0B: // HK_MOPP_TERM_REOFFSET32
                    case 0x53: // HK_MOPP_TERM32
                        int key = data[i + 4] + ((data[i + 3] + ((data[i + 2] + (data[i + 1] << 8)) << 8)) << 8);
                        key = ConvertShapeKey(key);
                        //keys with this flag reference scenery objects which is unsupported in later games
                        //swap the opcode to 0 to return and null the key
                        if ((key & 0x80000000) != 0)
                        {
                            key = 0;
                            data[i] = 0;
                        }
                        data[i + 1] = (byte)((key & 0x7F000000) >> 24);
                        data[i + 2] = (byte)((key & 0x00FF0000) >> 16);
                        data[i + 3] = (byte)((key & 0x0000FF00) >> 8);
                        data[i + 4] = (byte)(key & 0x000000FF);
                        i += 4;
                        break;
                    default:
                        throw new NotSupportedException($"Opcode 0x{data[i]:X2}");
                }
            }

            int ConvertShapeKey(int key)
            {
                int type = key >> 29;
                if (type == 1)
                    key |= (5 << 26); // needed to pass group filter
                return key;
            }
        }

        void ConvertH2GametypeFlags(ref TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags EngineFlags,
            Gen2Scenario.ScenarioNetgameEquipmentBlock NetgameEquipment)
        {
            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.CaptureTheFlag
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.CaptureTheFlag
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.CaptureTheFlag
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.CaptureTheFlag)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.CaptureTheFlag;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Assault;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.Slayer
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.Slayer
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.Slayer
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.Slayer)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Infection;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Slayer;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Vip;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.Oddball
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.Oddball
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.Oddball
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.Oddball)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Oddball;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.KingOfTheHill
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.KingOfTheHill
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.KingOfTheHill
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.KingOfTheHill)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.KingOfTheHill;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.Juggernaut
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.Juggernaut
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.Juggernaut
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.Juggernaut)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Juggernaut;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.Territories
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.Territories
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.Territories
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.Territories)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Territories;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.AllExceptCtf
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.AllExceptCtf
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.AllExceptCtf
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.AllExceptCtf)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Infection;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Slayer;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Vip;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Oddball;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.KingOfTheHill;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Juggernaut;
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.Territories;
            }

            if (NetgameEquipment.GameType1 == Gen2Scenario.GameTypeValue.AllGameTypes
                || NetgameEquipment.GameType2 == Gen2Scenario.GameTypeValue.AllGameTypes
                || NetgameEquipment.GameType3 == Gen2Scenario.GameTypeValue.AllGameTypes
                || NetgameEquipment.GameType4 == Gen2Scenario.GameTypeValue.AllGameTypes)
            {
                EngineFlags |= TagTool.Tags.Definitions.Common.GameEngineSubTypeFlags.All;
            }
        }
    }
}
