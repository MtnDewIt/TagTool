using Assimp.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Geometry.BspCollisionGeometry;
using TagTool.Geometry.Utils;
using TagTool.Tags.Definitions;
using static TagTool.Geometry.Ass.AssFormat;
using static TagTool.Geometry.Ass.AssFormat.AssObject;

namespace TagTool.Geometry.Ass
{
    public class AssExporter
    {
        GameCache Cache { get; set; }
        AssFormat Ass { get; set; }

        public readonly float ScaleFactor;

        public AssExporter(GameCache cacheContext, AssFormat ass, float scaleFactor = 100.0f)
        {
            Cache = cacheContext;
            Ass = ass;
            ScaleFactor = scaleFactor;
        }

        public void Export(ScenarioStructureBsp sbsp)
        {
            throw new NotImplementedException();
        }

        public void Export(StructureDesign sddt)
        {
            Ass.Instances.Add(new() { Name = "Scene Root", ParentID = -1 });

            RealVector3d globalForward = RealMatrix4x3.Identity.Forward;
            RealVector3d globalUp = RealMatrix4x3.Identity.Up;

            // Soft Ceilings
            foreach (var ceiling in sddt.SoftCeilings)
            {
                string name = Cache.StringTable.GetString(ceiling.Name);
                string prefix = ceiling.Type switch
                {
                    StructureDesign.SoftCeilingType.SoftKill => "soft_kill",
                    StructureDesign.SoftCeilingType.SlipSurface => "slip_surface",
                    _ => "soft_ceiling"
                };

                Ass.Materials.Add(new() { Name = $"+{prefix}:{name}" });

                AssObject assObj = new();
                List<AssVertex.UvSet> uvSets = [new()];
                int materialIndex = Ass.Materials.Count - 1;

                foreach (var triangle in ceiling.SoftCeilingTriangles)
                {
                    List<RealPoint3d> points = [triangle.Point1, triangle.Point2, triangle.Point3];
                    AddStructureDesignTriangle(assObj, points, triangle.Plane, uvSets, materialIndex);
                }

                Ass.Objects.Add(assObj);
                Ass.Instances.Add(new()
                {
                    Name = name,
                    ObjectIndex = Ass.Objects.Count - 1,
                    UniqueID = Ass.Instances.Count - 1,
                });
            }

            // Water Instances
            foreach (var instance in sddt.WaterInstances)
            {
                string waterGroup = Cache.StringTable.GetString(sddt.WaterGroups[instance.WaterNameIndex].Name);
                string name = $"~{waterGroup}";

                AssObject assObj = new();
                List<AssVertex.UvSet> uvSets = [new()];
                List<RealPoint3d> vertices = [];
                List<RealPlane3d> planes = [.. instance.WaterPlanes.Select(n => n.Plane)];
                float tolerance = 2.0e-4f; // looser than ideal but necessary

                foreach (var triangle in instance.WaterDebugTriangles)
                {
                    List<RealPoint3d> points = [triangle.Point1,  triangle.Point2, triangle.Point3];
                    RealPlane3d plane = planes.Find(p => p.ContainsAllPoints(points, tolerance));

                    AddStructureDesignTriangle(assObj, points, plane, uvSets, -1);
                    vertices.AddRange(points);
                }

                Ass.Objects.Add(assObj);
                Ass.Instances.Add(new()
                {
                    Name = name,
                    ObjectIndex = Ass.Objects.Count - 1,
                    UniqueID = Ass.Instances.Count - 1,
                });

                if (instance.FlowVelocity != default)
                {
                    AssInstance direction = new()
                    {
                        Name = $"#{waterGroup}_direction",
                        UniqueID = Ass.Instances.Count,
                        ParentID = Ass.Instances.Count - 1,
                    };

                    // create a decent position for the direction empty
                    RealPoint3d center = RealPoint3d.CenterOf(vertices) * ScaleFactor;
                    float offsetZ = (vertices.Max(v => v.Z) * ScaleFactor) + 50.0f;

                    direction.LocalTransform.Translation = new(center.X, center.Y, offsetZ);
                    direction.LocalTransform.Rotation = RealQuaternion.RotationFromVector(globalForward, instance.FlowVelocity);
                    direction.LocalTransform.Scale = 300.0f; // arbitrary scale for visibility

                    Ass.Instances.Add(direction);
                }
            }
        }

        public void AddStructureDesignTriangle(AssObject assObj, List<RealPoint3d> points, RealPlane3d plane, List<AssVertex.UvSet> uvSets, int materialIndex)
        {
            if (points.Count != 3)
                throw new ArgumentOutOfRangeException(nameof(points), "Invalid vertex count");

            var normal = RealVector3d.Normalize(plane.Normal);
            int firstIndex = assObj.Vertices.Count;

            foreach (var point in points)
            {
                assObj.Vertices.Add(new()
                {
                    Position = point * ScaleFactor,
                    Normal = normal,
                    UvSets = uvSets
                });
            }

            assObj.Triangles.Add(new()
            {
                MaterialIndex = materialIndex,
                VertexIndices = [firstIndex, firstIndex + 1, firstIndex + 2]
            });
        }

        public static int FindNearestNormalizedIndex(RealVector3d target, IList<RealVector3d> vectors)
        {
            var normalizedTarget = RealVector3d.Normalize(target);
            int result = 0;
            float maxFound = 0.0f;

            for (int i = 0; i < vectors.Count; i++)
            {
                RealVector3d normalized = RealVector3d.Normalize(vectors[i]);
                float product = RealVector3d.DotProduct(normalized, normalizedTarget);
                if (product > maxFound)
                {
                    maxFound = product;
                    result = i;
                }
            }

            return result;
        }
    }
}
