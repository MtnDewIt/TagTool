using TagTool.Cache;
using TagTool.IO;
using TagTool.Tags;
using System;
using System.IO;
using TagTool.Serialization;
using System.Collections;

namespace TagTool.Serialization
{
    public class RuntimeSerializationContext : ISerializationContext
    {
        private GameCache Cache { get; }
        private ProcessMemoryStream ProcessStream { get; }
        private uint StartAddress;
        private uint OriginalStructOffset;

        public RuntimeSerializationContext(GameCache cache, ProcessMemoryStream processStream, uint tagAddress, uint originalOffset)
        {
            Cache = cache;
            ProcessStream = processStream;
            StartAddress = tagAddress;
            OriginalStructOffset = originalOffset;
        }

        public uint AddressToOffset(uint currentOffset, uint address)
        {
            return address;
        }

        public EndianReader BeginDeserialize(TagStructureInfo info)
        {
            return new EndianReader(ProcessStream);
        }

        public void BeginSerialize(TagStructureInfo info)
        {
        }

        public IDataBlock CreateBlock()
        {
            return new DataBlock(StartAddress);
        }

        public void EndDeserialize(TagStructureInfo info, object obj)
        {
        }

        public void EndSerialize(TagStructureInfo info, byte[] data, uint mainStructOffset)
        {
            if(mainStructOffset <= OriginalStructOffset)
            {
                var hackOffset = OriginalStructOffset - mainStructOffset;
                ProcessStream.Position += hackOffset;   // tihs won't work since it offsets the entire tag data and the block/tag data address will not line up anymore. Need smarter way
                ProcessStream.Write(data, 0, data.Length);
            }
            else
            {
                Console.WriteLine("Too much data to write for poking tag.");
            }
        }

        public CachedTag GetTagByIndex(int index)
        {
            return (index >= 0 && index < Cache.TagCache.Count) ? Cache.TagCache.GetTag(index) : null;
        }

        public CachedTag GetTagByName(TagGroup group, string name)
        {
            throw new NotImplementedException();
        }

        public void AddResourceBlock(int count, CacheAddress address, IList block)
        {
            throw new NotImplementedException();
        }

        private class DataBlock : IDataBlock
        {
            public MemoryStream Stream { get; private set; }
            public EndianWriter Writer { get; private set; }
            private uint StartAddress;

            public DataBlock(uint startAddress)
            {
                Stream = new MemoryStream();
                Writer = new EndianWriter(Stream);
                StartAddress = startAddress;
            }

            public void WritePointer(uint targetOffset, Type type)
            {
                Writer.Write(targetOffset + StartAddress);
            }

            public object PreSerialize(TagFieldAttribute info, object obj)
            {
                return obj;
            }

            public void SuggestAlignment(uint align)
            {
            }

            public uint Finalize(Stream outStream)
            {
                var dataOffset = (uint)outStream.Position;
                outStream.Write(Stream.GetBuffer(), 0, (int)Stream.Length);

                Writer.Close();
                Stream = null;
                Writer = null;

                return dataOffset;
            }

            public void AddTagReference(CachedTag referencedTag, bool isShort)
            {
            }
        }
    }
}
