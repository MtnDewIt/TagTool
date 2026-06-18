using K4os.Compression.LZ4;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Extensions;
using TagTool.IO;
using TagTool.Serialization;
using TagTool.Tags;

namespace TagTool.Cache.HaloOnline
{
    [TagStructure(Size = 0x20)]
    public class ResourceCacheHaloOnlineHeader
    {
        public int UnusedTag;
        public uint ResourceTableOffset;
        public int ResourceCount;
        public int Unused;
        public long CreationTime;
        public int Unused2;
        public int Unused3;
    }

    // Class for .dat files containing resources
    public class ResourceCacheHaloOnline
    {
        public CacheVersion Version;
        public CachePlatform CachePlatform;
        public ResourceCacheHaloOnlineHeader Header;

        public List<Resource> Resources;

        private const int ChunkHeaderSize = 0x8;
        private const int MaxDecompressedBlockSize = 0x7FFF8; // Decompressed chunks cannot exceed this size

        public int Count
        {
            get { return Resources.Count; }
        }

        public ResourceCacheHaloOnline(CacheVersion version, CachePlatform cachePlatform, Stream stream)
        {
            Version = version;
            CachePlatform = cachePlatform;
            Resources = new List<Resource>();
            if (stream.Length == 0)
                CreateEmptyResourceCache(stream);
            else
                Read(stream);
        }

        public ResourceCacheHaloOnline(CacheVersion version)
        {
            Version = version;
            Resources = new List<Resource>();
            Header = new ResourceCacheHaloOnlineHeader
            {
                ResourceTableOffset = 0x20,
                CreationTime = 0x01D0631BCC92931B
            };
        }

        private void Read(Stream stream)
        {
            // don't use using{}, we want to maintain the stream open. reader/writers will automatically close the stream when done in an using.
            var reader = new EndianReader(stream, EndianFormat.LittleEndian);
            stream.Position = 0;
            var addresses = new List<uint>();
            var sizes = new List<uint>();
            var dataContext = new DataSerializationContext(reader);
            var deserializer = new TagDeserializer(Version, CachePlatform);
            Header = deserializer.Deserialize<ResourceCacheHaloOnlineHeader>(dataContext);

            reader.SeekTo(Header.ResourceTableOffset);

            // read all resource offsets

            if (Header.ResourceCount == 0)
                return;

            for (var i = 0; i < Header.ResourceCount; i++)
            {
                var address = reader.ReadUInt32();

                if (!addresses.Contains(address) && (address != uint.MaxValue))
                    addresses.Add(address);

                Resources.Add(new Resource { Offset = address });
            }

            // compute chunk sizes

            addresses.Sort((a, b) => a.CompareTo(b));

            for (var i = 0; i < addresses.Count - 1; i++)
                sizes.Add(addresses[i + 1] - addresses[i]);

            sizes.Add(Header.ResourceTableOffset - addresses.Last());

            foreach (var resource in Resources)
            {
                if (resource.Offset == uint.MaxValue)
                    continue;

                resource.ChunkSize = sizes[addresses.IndexOf(resource.Offset)];
            }
        }

        private void CreateEmptyResourceCache(Stream stream)
        {
            Header = new ResourceCacheHaloOnlineHeader
            {
                ResourceTableOffset = TagStructure.GetStructureSize(typeof(ResourceCacheHaloOnlineHeader), Version, CachePlatform),
                CreationTime = CacheVersionDetection.GetTimestamp(Version)
            };
            stream.Position = 0;
            var writer = new EndianWriter(stream, EndianFormat.LittleEndian);
            var dataContext = new DataSerializationContext(writer);
            var serializer = new TagSerializer(CacheVersion.HaloOnlineED, CachePlatform.Original);
            serializer.Serialize(dataContext, Header);
            stream.Position = 0;
        }

        /// <summary>
        /// Adds a resource to the cache.
        /// </summary>
        /// <param name="resourceCacheStream">The stream open on the resource cache.</param>
        /// <param name="tagResourceData">The data to compress.</param>
        /// <param name="compressedSize">On return, the size of the compressed data.</param>
        /// <returns>The index of the resource that was added.</returns>
        public int Add(Stream resourceCacheStream, ReadOnlySpan<byte> tagResourceData, out uint compressedSize)
        {
            var resourceIndex = NewResource();
            compressedSize = Compress(resourceCacheStream, resourceIndex, tagResourceData);
            return resourceIndex;
        }

        /// <summary>
        /// Adds a raw, pre-compressed resource to the cache.
        /// </summary>
        /// <param name="resourceCacheStream">The stream open on the resource cache.</param>
        /// <param name="rawData">The raw data to add.</param>
        /// <returns>The index of the resource that was added.</returns>
        public int AddRaw(Stream resourceCacheStream, ReadOnlySpan<byte> rawData)
        {
            var resourceIndex = NewResource();
            ImportRaw(resourceCacheStream, resourceIndex, rawData);
            return resourceIndex;
        }

        /// <summary>
        /// Extracts raw, compressed resource data.
        /// </summary>
        /// <param name="resourceCacheStream">The stream open on the resource cache.</param>
        /// <param name="resourceIndex">The index of the resource to decompress.</param>
        /// <param name="compressedSize">Total size of the compressed data, including chunk headers.</param>
        /// <returns>The raw, compressed resource data.</returns>
        public byte[] ExtractRaw(Stream resourceCacheStream, int resourceIndex, uint compressedSize)
        {
            if (resourceIndex < 0 || resourceIndex >= Resources.Count)
                throw new ArgumentOutOfRangeException("resourceIndex");

            var resource = Resources[resourceIndex];
            resourceCacheStream.Position = resource.Offset;
            var result = new byte[compressedSize];
            resourceCacheStream.ReadExactly(result);
            return result;
        }

        /// <summary>
        /// Overwrites a resource with raw, pre-compressed data.
        /// </summary>
        /// <param name="resourceCacheStream">The stream open on the resource cache.</param>
        /// <param name="resourceIndex">The index of the resource to overwrite.</param>
        /// <param name="data">The raw, pre-compressed data to overwrite it with.</param>
        public void ImportRaw(Stream resourceCacheStream, int resourceIndex, ReadOnlySpan<byte> data)
        {
            if (resourceIndex < 0 || resourceIndex >= Resources.Count)
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));

            var roundedSize = ResizeResource(resourceCacheStream, resourceIndex, (uint)data.Length);
            var resource = Resources[resourceIndex];
            resourceCacheStream.Position = resource.Offset;
            resourceCacheStream.Write(data);
            StreamUtil.Fill(resourceCacheStream, 0, (int)(roundedSize - data.Length)); // Padding
        }

        public void NullResource(Stream resourceStream, int resourceIndex)
        {
            if (resourceIndex < 0 || resourceIndex >= Resources.Count)
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));

            var resource = Resources[resourceIndex];
            if (resource.Offset == uint.MaxValue || IsResourceShared(resourceIndex))
                return;

            ResizeResource(resourceStream, resourceIndex, 0, shouldUpdateTable: false); 

            resource.ChunkSize = 0;
            resource.Offset = uint.MaxValue;
            UpdateResourceTable(resourceStream);
        }

        /// <summary>
        /// Decompresses a resource.
        /// </summary>
        /// <param name="resourceStream">The stream open on the resource cache.</param>
        /// <param name="resourceIndex">The index of the resource to decompress.</param>
        /// <param name="compressedSize">Total size of the compressed data, including chunk headers.</param>
        /// <param name="outStream">The stream to write the decompressed resource data to.</param>
        public void Decompress(Stream resourceStream, int resourceIndex, uint compressedSize, Stream outStream)
        {
            if (resourceIndex < 0 || resourceIndex >= Resources.Count)
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));

            var reader = new BinaryReader(resourceStream);
            var resource = Resources[resourceIndex];
            reader.BaseStream.Position = resource.Offset;

            // Compressed resources are split into chunks, so decompress each chunk until the complete data is decompressed
            int totalProcessed = 0;
            compressedSize = Math.Min(compressedSize, resource.ChunkSize);

            byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(LZ4Codec.MaximumOutputSize(MaxDecompressedBlockSize));
            byte[] decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxDecompressedBlockSize);

            try
            {
                while (totalProcessed < compressedSize)
                {
                    // Each chunk begins with a 32-bit uncompressed size followed by a 32-bit compressed size
                    int chunkUncompressedSize = reader.ReadInt32();
                    int chunkCompressedSize = reader.ReadInt32();
                    totalProcessed += 8;
                    if (totalProcessed >= compressedSize)
                        break;

                    // Decompress the chunk and write it to the output stream
                    reader.BaseStream.ReadExactly(compressedBuffer, 0, chunkCompressedSize);

                    int decompressedSize = LZ4Codec.Decode(compressedBuffer, 0, chunkCompressedSize, decompressedBuffer, 0, chunkUncompressedSize);
                    if (decompressedSize < 0)
                        throw new IOException("LZ4 decompression failed.");

                    outStream.Write(decompressedBuffer, 0, decompressedSize);
                    totalProcessed += chunkCompressedSize;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(compressedBuffer);
                ArrayPool<byte>.Shared.Return(decompressedBuffer);
            }
        }

        /// <summary>
        /// Compresses and saves data for a resource.
        /// </summary>
        /// <param name="resourceStream">The stream open on the resource data. It must have read/write access.</param>
        /// <param name="resourceIndex">The index of the resource to edit.</param>
        /// <param name="data">The data to compress.</param>
        /// <returns>The total size of the compressed resource in bytes.</returns>
        public uint Compress(Stream resourceStream, int resourceIndex, ReadOnlySpan<byte> data)
        {
            if (resourceIndex < 0 || resourceIndex >= Resources.Count)
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));

            // Divide the data into chunks with uncompressed sizes no larger than the maximum allowed size
            var chunks = new List<(byte[] Buffer, int UncompressedSize, int CompressedSize)>();
            int totalProcessed = 0;
            uint newSize = 0;
            int compressedBufferSize = LZ4Codec.MaximumOutputSize(MaxDecompressedBlockSize);

            try
            {
                while (totalProcessed < data.Length)
                {
                    int chunkUncompressedSize = Math.Min(data.Length - totalProcessed, MaxDecompressedBlockSize);
                    byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(compressedBufferSize);

                    int chunkCompressedSize = LZ4Codec.Encode(data.Slice(totalProcessed, chunkUncompressedSize), compressedBuffer.AsSpan(0, compressedBufferSize));
                    if (chunkCompressedSize < 0)
                        throw new IOException("LZ4 compression failed.");

                    chunks.Add((compressedBuffer, chunkUncompressedSize, chunkCompressedSize));
                    totalProcessed += chunkUncompressedSize;
                    newSize += (uint)(ChunkHeaderSize + chunkCompressedSize);
                }

                // Write the chunks in
                var writer = new EndianWriter(resourceStream, true, EndianFormat.LittleEndian);
                var roundedSize = ResizeResource(writer.BaseStream, resourceIndex, newSize);
                var resource = Resources[resourceIndex];
                resourceStream.Position = resource.Offset;
                foreach (var chunk in chunks)
                {
                    writer.Write(chunk.UncompressedSize);
                    writer.Write(chunk.CompressedSize);
                    writer.Write(chunk.Buffer, 0, chunk.CompressedSize);
                }

                StreamUtil.Fill(resourceStream, 0, (int)(roundedSize - newSize)); // Padding
            }
            finally
            {
                // cleanup
                foreach (var chunk in chunks)
                    ArrayPool<byte>.Shared.Return(chunk.Buffer);
            }

            return newSize;
        }

        private int NewResource()
        {
            var lastResource = (Resources.Count > 0) ? Resources[Resources.Count - 1] : null;
            var resourceIndex = Resources.Count;
            Resources.Add(new Resource
            {
                Offset = (Resources.Count == 0) ? 0x20 : (lastResource != null) ? lastResource.Offset + lastResource.ChunkSize : uint.MaxValue,
                ChunkSize = 0,
            });
            return resourceIndex;
        }

        private uint ResizeResource(Stream resourceStream, int resourceIndex, uint minSize, bool shouldUpdateTable = true)
        {
            var resource = Resources[resourceIndex];
            var roundedSize = ((minSize + 0xF) & ~0xFU); // Round up to a multiple of 0x10
            var sizeDelta = (int)(roundedSize - resource.ChunkSize);
            var endOffset = resource.Offset + resource.ChunkSize;
            StreamUtil.Copy(resourceStream, endOffset, endOffset + sizeDelta, resourceStream.Length - endOffset);
            resource.ChunkSize = roundedSize;

            // Update resource offsets
            for (var i = resourceIndex + 1; i < Resources.Count; i++)
            {
                if (Resources[i].Offset != uint.MaxValue)
                    Resources[i].Offset = (uint)(Resources[i].Offset + sizeDelta);
            }
               
            if(shouldUpdateTable)
                UpdateResourceTable(resourceStream);

            return roundedSize;
        }

        public bool IsResourceShared(int index) => Resources.Count(i => i.Offset == Resources[index].Offset) > 1;

        public void UpdateResourceTable(Stream resourceStream)
        {
            // Assume the table is past the last resource
            uint tableOffset = 0xC;

            var writer = new BinaryWriter(resourceStream);

            if (Resources.Count != 0)
            {
                var lastResource = Resources[Resources.Count - 1];
                tableOffset = lastResource.Offset + lastResource.ChunkSize;
            }

            writer.BaseStream.Position = tableOffset;

            // Write each resource offset
            foreach (var resource in Resources)
                writer.Write(resource.Offset);

            var tableEndOffset = writer.BaseStream.Position;

            // Update the file header
            writer.BaseStream.Position = 0x4;
            writer.Write(tableOffset);
            writer.Write(Resources.Count);

            writer.BaseStream.SetLength(tableEndOffset);
        }

        //
        // Utilities
        //

        public class Resource
        {
            // Offset in the resource file
            public uint Offset;
            // Distance between Offset and next resource Offset, may not be equal to the actual resource size
            public uint ChunkSize;
        }
    }
}
