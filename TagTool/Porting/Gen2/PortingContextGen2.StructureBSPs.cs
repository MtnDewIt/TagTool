using System;
using System.Collections.Generic;
using System.Linq;
using TagTool.Cache;
using TagTool.Commands.Scenarios;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Geometry;
using TagTool.Geometry.BspCollisionGeometry;
using TagTool.Geometry.Utils;
using TagTool.Havok;
using TagTool.Pathfinding;
using TagTool.Tags;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;
using static TagTool.Cache.HaloOnline.ResourceCacheHaloOnline;
using static TagTool.Porting.Gen2.Gen2BspGeometryConverter;
using Gen2Pathfinding = TagTool.Tags.Definitions.Gen2.ScenarioStructureBsp.PathfindingDataBlock;
using Gen2ScenarioStructureBsp = TagTool.Tags.Definitions.Gen2.ScenarioStructureBsp;

namespace TagTool.Porting.Gen2
{
    partial class PortingContextGen2
    {
        public TagStructure ConvertStructureBSP(Gen2ScenarioStructureBsp gen2Tag)
        {
            ScenarioStructureBsp newSbsp = new ScenarioStructureBsp();
            newSbsp.UseResourceItems = 1; // use CollisionBspResource
            newSbsp.ImportVersion = 7;

            AutoConverter.InitTagBlocks(newSbsp);

            //materials
            foreach (var material in gen2Tag.Materials)
            {
                newSbsp.Materials.Add(new RenderMaterial
                {
                    RenderMethod = material.Shader == null ? CacheContext.TagCache.GetTag(@"shaders\invalid.shader") : material.Shader,
                    Properties = material.Properties?.Count() == 0 ? null : new List<RenderMaterial.Property>
                    {
                        new RenderMaterial.Property()
                        {
                            Type = (RenderMaterial.Property.PropertyType)material.Properties[0].Type,
                            ShortValue = material.Properties[0].IntValue,
                            IntValue = material.Properties[0].IntValue,
                            RealValue = material.Properties[0].RealValue
                        }
                    },
                    ImportedMaterialIndex = -1,
                    BreakableSurfaceIndex = material.BreakableSurfaceIndex
                });
            }

            //collision materials
            foreach (var material in gen2Tag.CollisionMaterials)
            {
                newSbsp.CollisionMaterials.Add(new ScenarioStructureBsp.CollisionMaterial
                {
                    RenderMethod = material.NewShader == null ? CacheContext.TagCache.GetTag(@"shaders\invalid.shader") : material.NewShader,
                    RuntimeGlobalMaterialIndex = material.RuntimeGlobalMaterialIndex,
                    //RuntimeGlobalMaterialIndex = GetEquivalentGlobalMaterial(material.RuntimeGlobalMaterialIndex, Globals, Gen3Globals),
                    ConveyorSurfaceIndex = material.ConveyorSurfaceIndex,
                    SeamMappingIndex = -1
                });
            }

            //RENDER GEO RESOURCE
            //begin building render geo resource
            var builder = new RenderModelBuilder(CacheContext);
            builder.BeginRegion(StringId.Empty);
            builder.BeginPermutation(StringId.Empty);

            //COLLISION RESOURCE
            //create new collisionresource and populate values from tag
            StructureBspTagResources CollisionResource = new StructureBspTagResources();

            //main collision geometry
            CollisionResource.CollisionBsps = new TagBlock<CollisionGeometry>(CacheAddressType.Definition);

            int collisionEdgeCount = 0;
            foreach (var bsp in gen2Tag.CollisionBsp)
            {
                var newBsp = ConvertCollisionGeometry(bsp);
                collisionEdgeCount = newBsp.Edges.Count;
                CollisionResource.CollisionBsps.Add(newBsp);
            }

            //structure physics
            newSbsp.Physics = new ScenarioStructureBsp.StructurePhysics
            {
                MoppBoundsMin = gen2Tag.StructurePhysics.MoppBoundsMin,
                MoppBoundsMax = gen2Tag.StructurePhysics.MoppBoundsMax
            };

            byte[] moppdata = gen2Tag.StructurePhysics.MoppCode;
            newSbsp.Physics.CollisionMoppCodes = ConvertH2MOPP(moppdata);

            //world bounds
            newSbsp.WorldBoundsX = gen2Tag.WorldBoundsX;
            newSbsp.WorldBoundsY = gen2Tag.WorldBoundsY;
            newSbsp.WorldBoundsZ = gen2Tag.WorldBoundsZ;

            //leaves
            foreach (var leaf in gen2Tag.Leaves)
            {
                newSbsp.Leaves.Add(new ScenarioStructureBsp.Leaf
                {
                    ClusterNew = (byte)leaf.Cluster
                });
            }
            ;

            //transparent planes
            foreach (var plane in gen2Tag.TransparentPlanes)
            {
                newSbsp.TransparentPlanes.Add(new ScenarioStructureBsp.TransparentPlane
                {
                    MeshIndex = plane.SectionIndex,
                    PartIndex = plane.PartIndex,
                    Plane = plane.Plane
                });
            }

            //acoustic sound clusters (needed to prevent crash)
            newSbsp.AcousticsSoundClusters = new List<ScenarioStructureBsp.StructureBspSoundClusterBlock>() {
                    new ScenarioStructureBsp.StructureBspSoundClusterBlock() {
                        PaletteIndex = -1,
                    }
                };

            //cluster portals
            foreach (var portal in gen2Tag.ClusterPortals)
            {
                var newportal = new ScenarioStructureBsp.ClusterPortal
                {
                    BackCluster = portal.BackCluster,
                    FrontCluster = portal.FrontCluster,
                    PlaneIndex = portal.PlaneIndex,
                    Centroid = portal.Centroid,
                    BoundingRadius = portal.BoundingRadius,
                    Flags = (ScenarioStructureBsp.ClusterPortal.FlagsValue)portal.Flags,
                    Vertices = new List<ScenarioStructureBsp.ClusterPortal.Vertex>()
                };
                foreach (var vertex in portal.Vertices)
                {
                    newportal.Vertices.Add(new ScenarioStructureBsp.ClusterPortal.Vertex
                    {
                        Position = vertex.Point
                    });
                }
                newSbsp.ClusterPortals.Add(newportal);
            }

            List<Gen2BSPResourceMesh> Gen2Meshes = new List<Gen2BSPResourceMesh>();

            //cluster data
            foreach (var cluster in gen2Tag.Clusters)
            {
                List<Gen2BSPResourceMesh> clustermeshes = new List<Gen2BSPResourceMesh>();
                if (cluster.SectionInfo.TotalVertexCount > 0)
                {
                    //render geometry
                    var compressor = new VertexCompressor(
                        cluster.SectionInfo.Compression.Count > 0 ?
                            cluster.SectionInfo.Compression[0] :
                            new RenderGeometryCompression
                            {
                                X = new Bounds<float>(0.0f, 1.0f),
                                Y = new Bounds<float>(0.0f, 1.0f),
                                Z = new Bounds<float>(0.0f, 1.0f),
                                U = new Bounds<float>(0.0f, 1.0f),
                                V = new Bounds<float>(0.0f, 1.0f),
                                U2 = new Bounds<float>(0.0f, 1.0f),
                                V2 = new Bounds<float>(0.0f, 1.0f),
                            });
                    clustermeshes = ReadResourceMeshes((GameCacheGen2)BlamCache, cluster.GeometryBlockInfo,
                    cluster.SectionInfo.TotalVertexCount, (RenderGeometryCompressionFlags)cluster.SectionInfo.GeometryCompressionFlags,
                    (TagTool.Tags.Definitions.Gen2.RenderModel.SectionLightingFlags)cluster.SectionInfo.SectionLightingFlags, compressor);

                    if (clustermeshes.Count > 1)
                    {
                        Log.Warning("cluster had >1 render mesh! Culling extras...");
                        clustermeshes = new List<Gen2BSPResourceMesh> { clustermeshes.First() };
                    }
                }

                int newmeshindex = -1;
                if (clustermeshes.Count > 0)
                {
                    BuildMeshes(builder, clustermeshes, (RenderGeometryClassification)cluster.SectionInfo.GeometryClassification,
                        cluster.SectionInfo.OpaqueMaxNodesVertex, 0);

                    //fixup mesh part fields
                    var newmesh = builder.Meshes.Last();
                    for (var i = 0; i < newmesh.Mesh.Parts.Count; i++)
                    {
                        newmesh.Mesh.Parts[i].FirstSubPartIndex = clustermeshes[0].Parts[i].FirstSubPartIndex;
                        newmesh.Mesh.Parts[i].SubPartCount = clustermeshes[0].Parts[i].SubPartCount;
                        newmesh.Mesh.Parts[i].TypeNew = (Part.PartTypeNew)clustermeshes[0].Parts[i].TypeOld;
                    }
                    Gen2Meshes.AddRange(clustermeshes);
                    newmeshindex = Gen2Meshes.Count - 1;
                }

                //block values
                var newcluster = new ScenarioStructureBsp.Cluster
                {
                    //mesh that was just built
                    MeshIndex = (short)newmeshindex,
                    BoundsX = cluster.BoundsX,
                    BoundsY = cluster.BoundsY,
                    BoundsZ = cluster.BoundsZ,
                    AtmosphereIndex = -1,
                    CameraFxIndex = -1,
                    BackgroundSoundEnvironmentIndex = -1,
                    AcousticsSoundClusterIndex = 0,
                    Unknown3 = -1,
                    Unknown4 = -1,
                    Unknown5 = -1,
                    RuntimeDecalStartIndex = -1,
                    Portals = new List<ScenarioStructureBsp.Cluster.Portal>(),
                    InstancedGeometryPhysics = new ScenarioStructureBsp.Cluster.InstancedGeometryPhysicsData
                    {
                        ClusterIndex = gen2Tag.Clusters.IndexOf(cluster),
                        MoppCodes = cluster.CollisionMoppCode.Length > 0 ? ConvertH2MOPP(cluster.CollisionMoppCode) : null
                    }
                };

                //cluster portal indices
                foreach (var portal in cluster.Portals)
                {
                    newcluster.Portals.Add(new ScenarioStructureBsp.Cluster.Portal
                    {
                        PortalIndex = portal.PortalIndex
                    });
                }
                ;

                newSbsp.Clusters.Add(newcluster);
            }

            //instanced geometry definitions
            CollisionResource.InstancedGeometry = new TagBlock<InstancedGeometryBlock>(CacheAddressType.Definition);
            foreach (var instanced in gen2Tag.InstancedGeometriesDefinitions)
            {
                List<Gen2BSPResourceMesh> instancemeshes = new List<Gen2BSPResourceMesh>();
                if (instanced.RenderInfo.SectionInfo.TotalVertexCount > 0)
                {
                    //render geometry
                    var compressor = new VertexCompressor(
                        instanced.RenderInfo.SectionInfo.Compression.Count > 0 ?
                            instanced.RenderInfo.SectionInfo.Compression[0] :
                            new RenderGeometryCompression
                            {
                                X = new Bounds<float>(0.0f, 1.0f),
                                Y = new Bounds<float>(0.0f, 1.0f),
                                Z = new Bounds<float>(0.0f, 1.0f),
                                U = new Bounds<float>(0.0f, 1.0f),
                                V = new Bounds<float>(0.0f, 1.0f),
                                U2 = new Bounds<float>(0.0f, 1.0f),
                                V2 = new Bounds<float>(0.0f, 1.0f),
                            });
                    instancemeshes = ReadResourceMeshes((GameCacheGen2)BlamCache, instanced.RenderInfo.GeometryBlockInfo,
                    instanced.RenderInfo.SectionInfo.TotalVertexCount, (RenderGeometryCompressionFlags)instanced.RenderInfo.SectionInfo.GeometryCompressionFlags,
                    (TagTool.Tags.Definitions.Gen2.RenderModel.SectionLightingFlags)instanced.RenderInfo.SectionInfo.SectionLightingFlags, compressor);

                    if (instancemeshes.Count > 1)
                    {
                        Log.Warning("instance had >1 render mesh! Culling extras...");
                        instancemeshes = new List<Gen2BSPResourceMesh> { instancemeshes.First() };
                    }
                }

                int newmeshindex = -1;
                if (instancemeshes.Count > 0)
                {
                    BuildMeshes(builder, instancemeshes, (RenderGeometryClassification)instanced.RenderInfo.SectionInfo.GeometryClassification,
                    instanced.RenderInfo.SectionInfo.OpaqueMaxNodesVertex, 0);

                    //fixup mesh part fields
                    var newmesh = builder.Meshes.Last();
                    for (var i = 0; i < newmesh.Mesh.Parts.Count; i++)
                    {
                        newmesh.Mesh.Parts[i].FirstSubPartIndex = instancemeshes[0].Parts[i].FirstSubPartIndex;
                        newmesh.Mesh.Parts[i].SubPartCount = instancemeshes[0].Parts[i].SubPartCount;
                        newmesh.Mesh.Parts[i].TypeNew = (Part.PartTypeNew)instancemeshes[0].Parts[i].TypeOld;
                    }
                    Gen2Meshes.AddRange(instancemeshes);
                    newmeshindex = Gen2Meshes.Count - 1;
                }

                //block values
                var newinstance = new InstancedGeometryBlock
                {
                    Checksum = instanced.Checksum,
                    BoundingSphereOffset = instanced.BoundingSphereCenter,
                    BoundingSphereRadius = instanced.BoundingSphereRadius,
                    //index of mesh just builts
                    MeshIndex = (short)newmeshindex,
                };

                var bsp = ConvertCollisionGeometry(instanced.CollisionInfo);
                newinstance.CollisionInfo = bsp;

                //build mopp codes from collision info and add
                if (instanced.BspPhysics != null && instanced.BspPhysics.Count > 0)
                {
                    var mopps = ConvertH2MOPP(instanced.BspPhysics[0].MoppCodeData);
                    newinstance.CollisionMoppCodes = new TagBlock<TagHkpMoppCode>(CacheAddressType.Definition, mopps);
                }

                CollisionResource.InstancedGeometry.Add(newinstance);
            }

            //instanced geometry instances
            foreach (var instanced in gen2Tag.InstancedGeometryInstances)
            {
                var newinstance = new InstancedGeometryInstance
                {
                    Scale = instanced.Scale,
                    Matrix = new RealMatrix4x3(instanced.Forward.I, instanced.Forward.J, instanced.Forward.K,
                    instanced.Left.I, instanced.Left.J, instanced.Left.K,
                    instanced.Up.I, instanced.Up.J, instanced.Up.K,
                    instanced.Position.X, instanced.Position.Y, instanced.Position.Z),
                    DefinitionIndex = instanced.InstanceDefinition,
                    LightmapTexcoordBlockIndex = -1,
                    Name = instanced.Name,
                    WorldBoundingSphereCenter = instanced.WorldBoundingSphereCenter,
                    BoundingSphereRadiusBounds = new Bounds<float>(instanced.BoundingSphereRadius, instanced.BoundingSphereRadius),
                    PathfindingPolicy = (Scenery.PathfindingPolicyValue)instanced.PathfindingPolicy,
                    LightmappingPolicy = ((int)instanced.LightmappingPolicy) == 0 ?
                    InstancedGeometryInstance.InstancedGeometryLightmappingPolicy.PerPixelShared :
                    InstancedGeometryInstance.InstancedGeometryLightmappingPolicy.PerVertex,
                    LightmapResolutionScale = 1.0f
                };

                //make sure there is a bsp physics block in the instance def
                var instancedef = gen2Tag.InstancedGeometriesDefinitions[instanced.InstanceDefinition];
                if (instancedef.BspPhysics != null && instancedef.BspPhysics.Count > 0)
                {
                    newinstance.Flags |= InstancedGeometryInstance.InstancedGeometryFlags.Collidable;
                    newinstance.BspPhysics = new List<InstancedGeometryPhysics>
                    {
                        new InstancedGeometryPhysics
                        {
                            MoppBvTreeShape = new CMoppBvTreeShape
                            {
                                Scale = 1.0f,
                                Type = 27
                            },
                            GeometryShape = new CollisionGeometryShape
                            {
                                AABB_Center = instancedef.BspPhysics[0].AABB_Center,
                                AABB_Half_Extents = instancedef.BspPhysics[0].AABB_Half_Extents,
                                Scale = 1.0f
                            }
                        }
                    };
                }

                newSbsp.InstancedGeometryInstances.Add(newinstance);
            }

            //close out render geo resource
            builder.EndPermutation();
            builder.EndRegion();
            RenderModel meshbuild = builder.Build(CacheContext.Serializer);

            //create pathfinding resource
            var pathfindingresource = new StructureBspCacheFileTagResources();
            AutoConverter.InitTagBlocks(pathfindingresource);
            for (int i = 0; i < collisionEdgeCount; i++)
                pathfindingresource.EdgeToSeams.Add(new EdgeToSeamMapping() { SeamIndex = -1, SeamEdgeIndex = -1 });

            //convert pathfinding data
            if (gen2Tag.PathfindingData != null && gen2Tag.PathfindingData.Count > 0)
            {
                TagTool.Pathfinding.ResourcePathfinding newPathfinding = new TagTool.Pathfinding.ResourcePathfinding();
                AutoConverter.TranslateTagStructure(gen2Tag.PathfindingData[0], newPathfinding);

                //upgrade jump hints
                foreach (var hint in newPathfinding.PathfindingHints)
                {
                    if (hint.HintType == PathfindingHint.HintTypeValue.JumpLink || hint.HintType == PathfindingHint.HintTypeValue.WallJumpLink)
                    {
                        hint.Data[3] = (hint.Data[3] & ~ushort.MaxValue) | ((hint.Data[2] >> 16) & ushort.MaxValue); //move landing sector to data3
                        hint.Data[2] = (hint.Data[2] & ~(ushort.MaxValue << 16)); //remove old landing sector
                        hint.Data[2] = (hint.Data[2] | ((hint.Data[2] & (byte.MaxValue << 8)) << 8)); //move jump height flags
                        hint.Data[2] = (hint.Data[2] & ~(byte.MaxValue << 8)); //remove old jump height flags
                    }
                }
                pathfindingresource.PathfindingData = new TagBlock<Pathfinding.ResourcePathfinding>() {newPathfinding};
            }

            //write pathfinding resource
            newSbsp.PathfindingResource = CacheContext.ResourceCache.CreateStructureBspCacheFileResource(pathfindingresource);

            //write meshes and render model resource
            newSbsp.Geometry = meshbuild.Geometry;

            //generate water meshes if the map has them
            if (gen2Tag.WaterDefinitions != null && gen2Tag.WaterDefinitions.Count > 0 && gen2Tag.WaterDefinitions[0].Shader != null)
            {
                var waterWorldParams = new WorldGenerator.WorldParameters()
                {
                    Center = gen2Tag.WaterDefinitions[0].Center,
                    Extents = gen2Tag.WaterDefinitions[0].Extents,
                    Z = gen2Tag.WaterDefinitions[0].Height,
                    Shader = CacheContext.TagCache.GetTag(@"objects\eldewrito\reforge\shaders\water.shader"),
                    CellSize = 5,
                    Tesselation = 20,
                    Opacity = 0.9f,                 
                };

                //WorldGenerator.GenerateWaterWorld(CacheContext, newSbsp, waterWorldParams, out var waterGeometry, out var waterResource);
                WorldGenerator.GenerateFlatWorld(CacheContext, newSbsp, waterWorldParams, out var waterGeometry, out var waterResource);
                
                CollisionResource.InstancedGeometry.Add(new InstancedGeometryBlock
                {
                    BoundingSphereOffset = new RealPoint3d(gen2Tag.WaterDefinitions[0].Center.I, gen2Tag.WaterDefinitions[0].Center.J, gen2Tag.WaterDefinitions[0].Height),
                    BoundingSphereRadius = 500.0f,
                    MeshIndex = (short)(newSbsp.Geometry.Meshes.Count)
                });

                newSbsp.InstancedGeometryInstances.Add(new InstancedGeometryInstance
                {
                    Scale = 1.0f,
                    Matrix = new RealMatrix4x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, gen2Tag.WaterDefinitions[0].Center.I, gen2Tag.WaterDefinitions[0].Center.J, gen2Tag.WaterDefinitions[0].Height),
                    DefinitionIndex = (short)(CollisionResource.InstancedGeometry.Count - 1),
                    Flags = InstancedGeometryInstance.InstancedGeometryFlags.RenderOnly,
                    WorldBoundingSphereCenter = new RealPoint3d(gen2Tag.WaterDefinitions[0].Center.I, gen2Tag.WaterDefinitions[0].Center.J, gen2Tag.WaterDefinitions[0].Height),
                    BoundingSphereRadiusBounds = new Bounds<float>(500.0f, 500.0f),
                });               
                
                /*
                newSbsp.Clusters.Add(new ScenarioStructureBsp.Cluster()
                {
                    BoundsX = newSbsp.WorldBoundsX,
                    BoundsY = newSbsp.WorldBoundsY,
                    BoundsZ = newSbsp.WorldBoundsZ,
                    AtmosphereIndex = -1,
                    CameraFxIndex = -1,
                    BackgroundSoundEnvironmentIndex = -1,
                    AcousticsSoundClusterIndex = 0,
                    Unknown3 = -1,
                    Unknown4 = -1,
                    Unknown5 = -1,
                    RuntimeDecalStartIndex = -1,
                    MeshIndex = (short)(newSbsp.Geometry.Meshes.Count),
                });               
                */

                var geometryResource = CacheContext.ResourceCache.GetRenderGeometryApiResourceDefinition(newSbsp.Geometry.Resource);
                waterGeometry.Meshes[0].IndexBufferIndices[0] = (short)geometryResource.IndexBuffers.Count;
                waterGeometry.Meshes[0].VertexBufferIndices[0] = (short)geometryResource.VertexBuffers.Count;
                //waterGeometry.Meshes[0].VertexBufferIndices[6] = (short)(geometryResource.VertexBuffers.Count + 1);
                //waterGeometry.Meshes[0].VertexBufferIndices[7] = (short)(geometryResource.VertexBuffers.Count + 2);
                foreach (var part in waterGeometry.Meshes[0].Parts)
                    part.MaterialIndex = (short)(newSbsp.Materials.Count - 1);             
                newSbsp.Geometry.Meshes.Add(waterGeometry.Meshes[0]);
                geometryResource.IndexBuffers.Add(waterResource.IndexBuffers[0]);
                geometryResource.VertexBuffers.Add(waterResource.VertexBuffers[0]);
                //geometryResource.VertexBuffers.Add(waterResource.VertexBuffers[1]);
                //geometryResource.VertexBuffers.Add(waterResource.VertexBuffers[2]);
                newSbsp.Geometry.InstancedGeometryPerPixelLighting = new List<RenderGeometry.StaticPerPixelLighting>();
                newSbsp.Geometry.SetResourceBuffers(geometryResource, false);
                CacheContext.ResourceCaches.ReplaceResource(newSbsp.Geometry.Resource, geometryResource);

                /*
                int waterMeshIndex = newSbsp.Geometry.Meshes.Count - 1;
                newSbsp.Geometry.Meshes[waterMeshIndex].SubParts = new List<SubPart> { new SubPart
                {
                    FirstIndex = 0,
                    IndexCount = newSbsp.Geometry.Meshes[waterMeshIndex].Parts[0].IndexCount,
                    PartIndex = 0,
                    VertexCount = (ushort)waterResource.VertexBuffers[0].Definition.Count
                } };
                newSbsp.Geometry.MeshClusterVisibility.Add(HavokMoppGenerator.GeneratePerMeshMopp(CacheContext, newSbsp.Geometry.Meshes[waterMeshIndex]));
                */
            }

            //add empty meshes for clusters and instances with no mesh
            foreach (var cluster in newSbsp.Clusters)
            {
                if (cluster.MeshIndex == -1)
                {
                    Gen2Meshes.Add(new Gen2BSPResourceMesh());
                    newSbsp.Geometry.Meshes.Add(new Mesh()
                    {
                        Type = VertexType.World,
                        RigidNodeIndex = -1,
                        VertexBufferIndices = new short[8] { -1, -1, -1, -1, -1, -1, -1, -1 },
                        IndexBufferIndices = new short[2] { -1, -1 },
                    });
                    cluster.MeshIndex = (short)(newSbsp.Geometry.Meshes.Count - 1);
                }
            }
            foreach (var instance in CollisionResource.InstancedGeometry)
            {
                if (instance.MeshIndex == -1)
                {
                    Gen2Meshes.Add(new Gen2BSPResourceMesh());
                    newSbsp.Geometry.Meshes.Add(new Mesh()
                    {
                        Type = VertexType.World,
                        RigidNodeIndex = -1,
                        VertexBufferIndices = new short[8] { -1, -1, -1, -1, -1, -1, -1, -1 },
                        IndexBufferIndices = new short[2] { -1, -1 },
                    });
                    instance.MeshIndex = (short)(newSbsp.Geometry.Meshes.Count - 1);
                }
            }

            //write collision resource
            newSbsp.CollisionBspResource = CacheContext.ResourceCache.CreateStructureBspResource(CollisionResource);

            //fixup per mesh visibility mopp
            newSbsp.Geometry.MeshClusterVisibility = new List<RenderGeometry.PerMeshMoppBlock>();
            newSbsp.Geometry.PerMeshSubpartVisibility = new List<RenderGeometry.PerMeshSubpartVisibilityBlock>();
            for (var i = 0; i < Gen2Meshes.Count; i++)
            {
                Gen2BSPResourceMesh gen2mesh = Gen2Meshes[i];
                if (gen2mesh.VisibilityMoppCodeData == null || gen2mesh.VisibilityBounds == null)
                    continue;
                //mesh visibility mopp and mopp reorder table
                if (gen2mesh.VisibilityMoppCodeData.Length > 0 && gen2mesh.MoppReorderTable != null)
                {
                    newSbsp.Geometry.MeshClusterVisibility.Add(new RenderGeometry.PerMeshMoppBlock
                    {
                        MoppCode = ConvertH2MoppData(gen2mesh.VisibilityMoppCodeData),
                        MoppReorderTable = gen2mesh.MoppReorderTable.Select(m => m.Index).ToList()
                    });
                }
                //visibility bounds (approximate conversion)
                for (var j = 0; j < gen2mesh.VisibilityBounds.Count; j++)
                {
                    newSbsp.Geometry.PerMeshSubpartVisibility.Add(new RenderGeometry.PerMeshSubpartVisibilityBlock
                    {
                        BoundingSpheres = new List<RenderGeometry.BoundingSphere> { new RenderGeometry.BoundingSphere
                    {
                        Position = gen2mesh.VisibilityBounds[j].Position,
                        Radius = gen2mesh.VisibilityBounds[j].Radius,
                        NodeIndices = new sbyte[]{ (sbyte)gen2mesh.VisibilityBounds[j].NodeIndex, 0, 0, 0}
                    } }
                    });
                }

            }

            bspMeshes.Add(Gen2Meshes);

            InstanceBucketGenerator.Generate(newSbsp, CollisionResource);
            ConvertGen2EnvironmentMopp(newSbsp);
            return newSbsp;
        }
    }
}
