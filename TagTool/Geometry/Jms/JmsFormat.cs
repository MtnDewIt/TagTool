using System;
using System.Collections.Generic;
using System.IO;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Geometry.Utils;

namespace TagTool.Geometry.Jms
{
    // REFERENCE:  Halo-Asset-Blender-Development-Toolset
    // https://github.com/General-101/Halo-Asset-Blender-Development-Toolset/blob/master/io_scene_halo/file_jms/build_asset.py

    public class JmsFormat
    {
        private int Version = 8213; // 8213 is the maximum documented version of the JMS format
        private int SignificantFigures = 10;

        private uint NodeCount;
        public List<JmsNode> Nodes = new List<JmsNode>();
        private uint MaterialCount;
        public List<JmsMaterial> Materials = new List<JmsMaterial>();
        private uint MarkerCount;
        public List<JmsMarker> Markers = new List<JmsMarker>();
        private uint VertexCount;
        public List<JmsVertex> Vertices = new List<JmsVertex>();
        private uint TriangleCount;
        public List<JmsTriangle> Triangles = new List<JmsTriangle>();
        private uint SphereCount;
        public List<JmsSphere> Spheres = new List<JmsSphere>();
        private uint BoxCount;
        public List<JmsBox> Boxes = new List<JmsBox>();
        private uint CapsuleCount;
        public List<JmsCapsule> Capsules = new List<JmsCapsule>();
        private uint ConvexShapeCount;
        public List<JmsConvexShape> ConvexShapes = new List<JmsConvexShape>();
        private uint RagdollCount;
        public List<JmsRagdoll> Ragdolls = new List<JmsRagdoll>();
        private uint HingeCount;
        public List<JmsHinge> Hinges = new List<JmsHinge>();
        private uint SkylightCount;
        public List<JmsSkylight> Skylights = new List<JmsSkylight>();

        public bool TryRead(FileInfo file)
        {
            try
            {
                Read(file);
            }
            catch (Exception)
            {
                Log.Error("Invalid JMS.");
                return false;
            }
            return true;
        }

        public void Write(FileInfo file)
        {
            using (var stream = BlamAssetWriter.Create(file))
            {
                stream.WriteLine(";### VERSION ###");
                stream.WriteLine(Version);
                stream.WriteLine();

                stream.WriteLine(";### NODES ###");
                stream.WriteLine(Nodes.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<parent node index>");
                stream.WriteLine(";\t<default rotation <i,j,k,w>>");
                stream.WriteLine(";\t<default translation <x,y,z>>");
                stream.WriteLine();
                for (var nodeindex = 0; nodeindex < Nodes.Count; nodeindex++)
                {
                    stream.WriteLine($";NODE {nodeindex}");
                    Nodes[nodeindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### MATERIALS ###");
                stream.WriteLine(Materials.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<material name>");
                stream.WriteLine();
                for (var materialindex = 0; materialindex < Materials.Count; materialindex++)
                {
                    stream.WriteLine($";MATERIAL {materialindex}");
                    Materials[materialindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### MARKERS ###");
                stream.WriteLine(Markers.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<node index>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<radius>");
                stream.WriteLine();
                for (var markerindex = 0; markerindex < Markers.Count; markerindex++)
                {
                    stream.WriteLine($";MARKER {markerindex}");
                    Markers[markerindex].Write(stream);
                    stream.WriteLine();
                }

                //leave unpopulated for now
                stream.WriteLine(";### INSTANCE XREF PATHS ###");
                stream.WriteLine(0);
                stream.WriteLine(";\t<path>");
                stream.WriteLine(";\t<name>");
                stream.WriteLine();

                //leave unpopulated for now
                stream.WriteLine(";### INSTANCE MARKERS ###");
                stream.WriteLine(0);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<unique identifier>");
                stream.WriteLine(";\t<path index>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine();

                stream.WriteLine(";### VERTICES ###");
                stream.WriteLine(Vertices.Count);
                stream.WriteLine(";\t<position>");
                stream.WriteLine(";\t<normal>");
                stream.WriteLine(";\t<node influences count>");
                stream.WriteLine(";\t\t<node influences <index, weight>>");
                stream.WriteLine(";\t\t<...>");
                stream.WriteLine(";\t<texture coordinate count>");
                stream.WriteLine(";\t\t<texture coordinates <u,v>>");
                stream.WriteLine(";\t\t<...>");
                stream.WriteLine(";\t\t<vertex color <r,g,b>>");
                stream.WriteLine(";\t\t<...>");
                stream.WriteLine();
                for (var vertexindex = 0; vertexindex < Vertices.Count; vertexindex++)
                {
                    stream.WriteLine($";VERTEX {vertexindex}");
                    Vertices[vertexindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### TRIANGLES ###");
                stream.WriteLine(Triangles.Count);
                stream.WriteLine(";\t<material index>");
                stream.WriteLine(";\t<vertex indices <v0,v1,v2>>");
                stream.WriteLine();
                for (var triangleindex = 0; triangleindex < Triangles.Count; triangleindex++)
                {
                    stream.WriteLine($";TRIANGLE {triangleindex}");
                    Triangles[triangleindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### SPHERES ###");
                stream.WriteLine(Spheres.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<parent>");
                stream.WriteLine(";\t<material>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<radius>");
                stream.WriteLine();
                for (var sphereindex = 0; sphereindex < Spheres.Count; sphereindex++)
                {
                    stream.WriteLine($";SPHERE {sphereindex}");
                    Spheres[sphereindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### BOXES ###");
                stream.WriteLine(Boxes.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<parent>");
                stream.WriteLine(";\t<material>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<width (x)>");
                stream.WriteLine(";\t<length (y)>");
                stream.WriteLine(";\t<height (z)>");
                stream.WriteLine();
                for (var boxindex = 0; boxindex < Boxes.Count; boxindex++)
                {
                    stream.WriteLine($";BOX {boxindex}");
                    Boxes[boxindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### CAPSULES ###");
                stream.WriteLine(Capsules.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<parent>");
                stream.WriteLine(";\t<material>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<height>");
                stream.WriteLine(";\t<radius>");
                stream.WriteLine();
                for (var capsuleindex = 0; capsuleindex < Capsules.Count; capsuleindex++)
                {
                    stream.WriteLine($";CAPSULE {capsuleindex}");
                    Capsules[capsuleindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### CONVEX SHAPES ###");
                stream.WriteLine(ConvexShapes.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<parent>");
                stream.WriteLine(";\t<material>");
                stream.WriteLine(";\t<rotation <i,j,k,w>>");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<vertex count>");
                stream.WriteLine(";\t<...vertices>");
                stream.WriteLine();
                for (var convexshapeindex = 0; convexshapeindex < ConvexShapes.Count; convexshapeindex++)
                {
                    stream.WriteLine($";CONVEX {convexshapeindex}");
                    ConvexShapes[convexshapeindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### RAGDOLLS ###");
                stream.WriteLine(Ragdolls.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<attached index>");
                stream.WriteLine(";\t<referenced index>");
                stream.WriteLine(";\t<attached transform>");
                stream.WriteLine(";\t<reference transform>");
                stream.WriteLine(";\t<min twist>");
                stream.WriteLine(";\t<max twist>");
                stream.WriteLine(";\t<min cone>");
                stream.WriteLine(";\t<max cone>");
                stream.WriteLine(";\t<min plane>");
                stream.WriteLine(";\t<max plane>");
                stream.WriteLine(";\t<friction limit>");
                stream.WriteLine();
                for (var ragdollindex = 0; ragdollindex < Ragdolls.Count; ragdollindex++)
                {
                    stream.WriteLine($";RAGDOLL {ragdollindex}");
                    Ragdolls[ragdollindex].Write(stream);
                    stream.WriteLine();
                }

                stream.WriteLine(";### HINGES ###");
                stream.WriteLine(Hinges.Count);
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<body A index>");
                stream.WriteLine(";\t<body B index>");
                stream.WriteLine(";\t<body A transform>");
                stream.WriteLine(";\t<body B transform>");
                stream.WriteLine(";\t<is limited>");
                stream.WriteLine(";\t<friction limit>");
                stream.WriteLine(";\t<min angle>");
                stream.WriteLine(";\t<max angle");
                stream.WriteLine();
                for (var hingeindex = 0; hingeindex < Hinges.Count; hingeindex++)
                {
                    stream.WriteLine($";HINGE {hingeindex}");
                    Hinges[hingeindex].Write(stream);
                    stream.WriteLine();
                }

                //leave unpopulated for now
                stream.WriteLine(";### CAR_WHEEL ###");
                stream.WriteLine("0");
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<chassis index>");
                stream.WriteLine(";\t<wheel index>");
                stream.WriteLine(";\t<chassis transform>");
                stream.WriteLine(";\t<wheel transform>");
                stream.WriteLine(";\t<suspension transform>");
                stream.WriteLine(";\t<suspension min limit>");
                stream.WriteLine(";\t<suspension max limit>");
                stream.WriteLine(";\t<friction limit>");
                stream.WriteLine(";\t<velocity>");
                stream.WriteLine(";\t<gain>");
                stream.WriteLine();

                //leave unpopulated for now
                stream.WriteLine(";### POINT_TO_POINT ###");
                stream.WriteLine("0");
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<body A index>");
                stream.WriteLine(";\t<body B index>");
                stream.WriteLine(";\t<body A transform>");
                stream.WriteLine(";\t<body B transform>");
                stream.WriteLine(";\t<constraint type>");
                stream.WriteLine(";\t<x min limit>");
                stream.WriteLine(";\t<x max limit>");
                stream.WriteLine(";\t<y min limit>");
                stream.WriteLine(";\t<y max limit>");
                stream.WriteLine(";\t<z min limit>");
                stream.WriteLine(";\t<z max limit>");
                stream.WriteLine(";\t<spring length>");
                stream.WriteLine();

                //leave unpopulated for now
                stream.WriteLine(";### PRISMATIC ###");
                stream.WriteLine("0");
                stream.WriteLine(";\t<name>");
                stream.WriteLine(";\t<body A index>");
                stream.WriteLine(";\t<body B index>");
                stream.WriteLine(";\t<body A transform>");
                stream.WriteLine(";\t<body B transform>");
                stream.WriteLine(";\t<is limited>");
                stream.WriteLine(";\t<friction limit>");
                stream.WriteLine(";\t<min limit>");
                stream.WriteLine(";\t<max limit>");
                stream.WriteLine();

                //leave unpopulated for now
                stream.WriteLine(";### BOUNDING SPHERE ###");
                stream.WriteLine("0");
                stream.WriteLine(";\t<translation <x,y,z>>");
                stream.WriteLine(";\t<radius>");
                stream.WriteLine();

                //leave unpopulated for now
                stream.WriteLine(";### SKYLIGHT ###");
                stream.WriteLine(Skylights.Count);
                stream.WriteLine(";\t<direction <x,y,z>>");
                stream.WriteLine(";\t<radiant intensity <x,y,z>>");
                stream.WriteLine(";\t<solid angle>");
                stream.WriteLine();
                for (var skylightindex = 0; skylightindex < Skylights.Count; skylightindex++)
                {
                    stream.WriteLine($";SKYLIGHT {skylightindex}");
                    Skylights[skylightindex].Write(stream);
                    stream.WriteLine();
                }
            }
        }

        public void Read(FileInfo file)
        {
            using (var stream = file.OpenText())
            {
                if (stream.BaseStream.Length > 0)
                {
                    stream.ReadLine();
                    Version = int.Parse(stream.ReadLine());
                    if (Version != 8213)
                    {
                        Log.Error("JMS reading only supported for version 8213");
                        return;
                    }

                    // parse Nodes
                    NodeCount = uint.Parse(stream.ReadLine());
                    for (int i = 0; i < NodeCount; i++)
                    {
                        JmsNode node = new JmsNode();
                        node.Read(stream);                  
                        Nodes.Add(node);
                    }

                    // parse Materials
                    MaterialCount = uint.Parse(stream.ReadLine());
                    for (int i = 0; i < MaterialCount; i++)
                    {
                        JmsMaterial material = new JmsMaterial();
                        material.Read(stream);
                        Materials.Add(material);
                    }

                    // parse Markers
                    MarkerCount = uint.Parse(stream.ReadLine());
                    for (int i = 0; i < MarkerCount; i++)
                    {
                        JmsMarker marker = new JmsMarker();
                        marker.Read(stream);
                        Markers.Add(marker);
                    }

                    // parse Vertices
                    VertexCount = uint.Parse(stream.ReadLine());
                    for (int i = 0; i < VertexCount; i++)
                    {
                        JmsVertex vertex = new JmsVertex();
                        vertex.Read(stream);
                        Vertices.Add(vertex);
                    }

                    // parse Triangles
                    TriangleCount = uint.Parse(stream.ReadLine());
                    for (int i = 0; i < TriangleCount; i++)
                    {
                        JmsTriangle triangle = new JmsTriangle();
                        triangle.Read(stream);
                        Triangles.Add(triangle);
                    }
                }
            }
            return;
        }

        public class JmsNode : JmsElement
        {
            public string Name = "default";
            public int ParentNodeIndex = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Position = new RealVector3d(0, 0, 0);

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(ParentNodeIndex);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Position);            
            }
        }

        public class JmsMaterial : JmsElement
        {
            public string Name = "default";
            public string MaterialName = "default";

            public void Read(StreamReader stream)
            {
                Name = stream.ReadLine();
                MaterialName = stream.ReadLine();
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(MaterialName);
            }
        }

        public class JmsMarker : JmsElement
        {
            public string Name = "default";
            public int NodeIndex = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Translation = new RealVector3d(0, 0, 0);
            public float Radius = 0.0f;

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(NodeIndex);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Translation);
                stream.WriteFloat(Radius);
            }
        }

        public class JmsVertex : JmsElement
        {
            public RealPoint3d Position = new RealPoint3d(0, 0, 0);
            public RealVector3d Normal = new RealVector3d(0, 0, 0);
            public List<NodeSet> NodeSets = new List<NodeSet>();
            public List<UvSet> UvSets = new List<UvSet>();
            public RealRgbColor VertexColor = new RealRgbColor();

            public class NodeSet
            {
                public int NodeIndex = -1;
                public float NodeWeight = 0.0f;
            }
            public class UvSet
            {
                public RealPoint2d TextureCoordinates = new RealPoint2d();
            }
            public void Read(StreamReader stream)
            {
            }
            public void Write(BlamAssetWriter stream)
            {
                stream.WritePoint3d(Position);
                stream.WriteVector3d(Normal);
                stream.WriteLine(NodeSets.Count);
                if (NodeSets.Count > 0)
                {
                    foreach (var nodeset in NodeSets)
                    {
                        stream.WriteLine(nodeset.NodeIndex);
                        stream.WriteFloat(nodeset.NodeWeight);
                    }
                }
                stream.WriteLine(UvSets.Count);
                if (UvSets.Count > 0)
                {
                    foreach (var uvset in UvSets)
                    {
                        stream.WritePoint2d(uvset.TextureCoordinates);
                    }
                }
                //color is null
                stream.WritePoint3d(new RealPoint3d());
            }
        }

        public class JmsTriangle : JmsElement
        {
            public int MaterialIndex = -1;
            public List<int> VertexIndices = new List<int>();

            public void Read(StreamReader stream)
            {
                MaterialIndex = int.Parse(stream.ReadLine());

                // parse triangle indices
                string[] indexArray = stream.ReadLine().Split('\t');
                for (byte index = 0; index < indexArray.Length; index++)
                    VertexIndices.Add(int.Parse(indexArray[index]));
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(MaterialIndex);
                stream.WriteLine($"{VertexIndices[0]}\t{VertexIndices[1]}\t{VertexIndices[2]}");
            }
        }

        public class JmsSphere : JmsElement
        {
            public string Name = "default";
            public int Parent = -1;
            public int Material = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Translation = new RealVector3d(0, 0, 0);
            public float Radius = 0.0f;

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(Parent);
                stream.WriteLine(Material);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Translation);
                stream.WriteFloat(Radius);
            }
        }

        public class JmsBox : JmsElement
        {
            public string Name = "default";
            public int Parent = -1;
            public int Material = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Translation = new RealVector3d(0, 0, 0);
            public float Width = 0.0f;
            public float Length = 0.0f;
            public float Height = 0.0f;

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(Parent);
                stream.WriteLine(Material);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Translation);
                stream.WriteFloat(Width);
                stream.WriteFloat(Length);
                stream.WriteFloat(Height);
            }
        }

        public class JmsCapsule : JmsElement
        {
            public string Name = "default";
            public int Parent = -1;
            public int Material = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Translation = new RealVector3d(0, 0, 0);
            public float Height = 0.0f;
            public float Radius = 0.0f;

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(Parent);
                stream.WriteLine(Material);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Translation);
                stream.WriteFloat(Height);
                stream.WriteFloat(Radius);
            }
        }

        public class JmsConvexShape : JmsElement
        {
            public string Name = "default";
            public int Parent = -1;
            public int Material = -1;
            public RealQuaternion Rotation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d Translation = new RealVector3d(0, 0, 0);
            public int ShapeVertexCount = 0;
            public List<RealPoint3d> ShapeVertices = new List<RealPoint3d>();

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(Parent);
                stream.WriteLine(Material);
                stream.WriteQuaternion(Rotation);
                stream.WriteVector3d(Translation);
                stream.WriteLine(ShapeVertexCount);
                foreach(var shapevert in ShapeVertices)
                    stream.WritePoint3d(shapevert);
            }
        }

        public class JmsRagdoll : JmsElement
        {
            public string Name = "default";
            public int AttachedIndex = -1;
            public int ReferencedIndex = -1;
            public RealQuaternion AttachedTransformOrientation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d AttachedTransformPosition = new RealVector3d(0, 0, 0);
            public RealQuaternion ReferenceTransformOrientation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d ReferenceTransformPosition = new RealVector3d(0, 0, 0);
            public float MinTwist;
            public float MaxTwist;
            public float MinCone;
            public float MaxCone;
            public float MinPlane;
            public float MaxPlane;
            public float FrictionLimit;
            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(AttachedIndex);
                stream.WriteLine(ReferencedIndex);
                stream.WriteQuaternion(AttachedTransformOrientation);
                stream.WriteVector3d(AttachedTransformPosition);
                stream.WriteQuaternion(ReferenceTransformOrientation);
                stream.WriteVector3d(ReferenceTransformPosition);
                stream.WriteFloat(MinTwist);
                stream.WriteFloat(MaxTwist);
                stream.WriteFloat(MinCone);
                stream.WriteFloat(MaxCone); 
                stream.WriteFloat(MinPlane);
                stream.WriteFloat(MaxPlane);
                stream.WriteFloat(FrictionLimit);
            }
        }

        public class JmsHinge : JmsElement
        {
            public string Name = "default";
            public int BodyAIndex = -1;
            public int BodyBIndex = -1;
            public RealQuaternion BodyATransformOrientation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d BodyATransformPosition = new RealVector3d(0, 0, 0);
            public RealQuaternion BodyBTransformOrientation = new RealQuaternion(0, 0, 0, 1);
            public RealVector3d BodyBTransformPosition = new RealVector3d(0, 0, 0);
            public int IsLimited;
            public float FrictionLimit;
            public float MinAngle;
            public float MaxAngle;
            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteLine(Name);
                stream.WriteLine(BodyAIndex);
                stream.WriteLine(BodyBIndex);
                stream.WriteQuaternion(BodyATransformOrientation);
                stream.WriteVector3d(BodyATransformPosition);
                stream.WriteQuaternion(BodyBTransformOrientation);
                stream.WriteVector3d(BodyBTransformPosition);
                stream.WriteLine(IsLimited);
                stream.WriteFloat(FrictionLimit);
                stream.WriteFloat(MinAngle);
                stream.WriteFloat(MaxAngle);
            }
        }

        public class JmsSkylight : JmsElement
        {
            public RealVector3d Direction = new RealVector3d(0, 0, 0);
            public RealVector3d RadiantIntensity = new RealVector3d(0, 0, 0);
            public float SolidAngle = 0.0f;

            public void Read(StreamReader stream)
            {
            }

            public void Write(BlamAssetWriter stream)
            {
                stream.WriteVector3d(Direction);
                stream.WriteVector3d(RadiantIntensity);
                stream.WriteFloat(SolidAngle);
            }
        }

        public class JmsElement
        {
            public RealVector3d ReadVector3d(StreamReader stream)
            {
                string[] vector3dArray = stream.ReadLine().Split('\t');
                return new RealVector3d(float.Parse(vector3dArray[0]), float.Parse(vector3dArray[1]), float.Parse(vector3dArray[2]));
            }

            public RealPoint3d ReadPoint3d(StreamReader stream)
            {
                string[] point3dArray = stream.ReadLine().Split('\t');
                return new RealPoint3d(float.Parse(point3dArray[0]), float.Parse(point3dArray[1]), float.Parse(point3dArray[2]));
            }
        }       
    }
}
