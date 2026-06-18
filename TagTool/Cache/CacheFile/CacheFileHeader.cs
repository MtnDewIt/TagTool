using TagTool.Cache.MCC;
using TagTool.Common;
using TagTool.IO;
using TagTool.Serialization;
using TagTool.Tags;

namespace TagTool.Cache
{
    public abstract class CacheFileHeader :  TagStructure
    {
        public virtual bool IsValid()
        {
            if (GetHeadTag() == "head" && GetFootTag() == "foot")
                return true;
            else
                return false;
        }

        public static CacheFileHeader Read(CacheFileVersion fileVersion, CacheVersion version, CachePlatform cachePlatform, EndianReader reader)
        {
            var deserializer = new TagDeserializer(version, cachePlatform);
            reader.SeekTo(0);
            var dataContext = new DataSerializationContext(reader);

            if (fileVersion == CacheFileVersion.HaloMCCUniversal)
            {
                // TODO: cleanup
                // adapt the header for gen3 for now
                var header = deserializer.Deserialize<CacheFileHeaderMCC>(dataContext);
                header.StringIdsHeader.BufferOffset = header.StringIdsHeader.BufferOffsetMCC;

                if (version >= CacheVersion.Halo4)
                {
                    return new CacheFileHeaderGen4()
                    {
                        HeaderSignature = header.HeaderSignature,
                        FileVersion = header.FileVersion,
                        TagTableHeaderOffset64 = header.TagTableHeaderOffset.Value,
                        TagMemoryHeader = header.TagMemoryHeader,
                        SourceFile = header.SourceFile,
                        Build = header.Build,
                        CacheType = header.CacheType,
                        SharedCacheType = header.SharedCacheType,
                        StringIdsHeader = header.GetStringIDHeader(),
                        TagNamesHeader = header.TagNamesHeader,
                        Name = header.Name,
                        VirtualBaseAddress64 = header.VirtualBaseAddress.Value,
                        Partitions = header.Partitions,
                        SectionTable = header.SectionTable,
                        FooterSignature = header.FooterSignature
                    };
                }

                var adapter = new CacheFileHeaderGen3()
                {
                    HeaderSignature = header.HeaderSignature,
                    FileVersion = header.FileVersion,
                    TagTableHeaderOffset = header.TagTableHeaderOffset,
                    TagMemoryHeader = header.TagMemoryHeader,
                    SourceFile = header.SourceFile,
                    Build = header.Build,
                    CacheType = header.CacheType,
                    SharedCacheType = header.SharedCacheType,
                    StringIdsHeader = header.GetStringIDHeader(),
                    TagNamesHeader = header.TagNamesHeader,
                    Name = header.Name,
                    VirtualBaseAddress = header.VirtualBaseAddress,
                    Partitions = header.Partitions,
                    SectionTable = header.SectionTable,
                    FooterSignature = header.FooterSignature
                };
                return adapter;
            }

            return CacheVersionDetection.GetGeneration(version) switch
            {
                CacheGeneration.First => deserializer.Deserialize<CacheFileHeaderGen1>(dataContext),
                CacheGeneration.Second => deserializer.Deserialize<CacheFileHeaderGen2>(dataContext),
                CacheGeneration.Third => deserializer.Deserialize<CacheFileHeaderGen3>(dataContext),
                CacheGeneration.HaloOnline => deserializer.Deserialize<CacheFileHeaderGenHaloOnline>(dataContext),
                CacheGeneration.Fourth => deserializer.Deserialize<CacheFileHeaderGen4>(dataContext),
                _ => null,
            };
        }

        public abstract Tag GetHeadTag();
        public abstract Tag GetFootTag();
        public abstract ulong GetTagTableHeaderOffset();
        public abstract string GetName();
        public abstract string GetBuild();
        public abstract string GetScenarioPath();
        public abstract int GetScenarioTagIndex();
        public abstract CacheFileType GetCacheType();
        public abstract CacheFileSharedType GetSharedCacheType();
        public abstract StringIDHeader GetStringIDHeader();
        public abstract TagNameHeader GetTagNameHeader();
        public abstract TagMemoryHeader GetTagMemoryHeader();

    }

    [TagStructure(Size = 0x14, MinVersion = CacheVersion.Halo2Alpha, MaxVersion = CacheVersion.Halo3Beta)]
    [TagStructure(Size = 0x10, MinVersion = CacheVersion.Halo3Retail, Platform = CachePlatform.Original)]
    [TagStructure(Size = 0x18, MinVersion = CacheVersion.Halo3Retail, Platform = CachePlatform.MCC)]
    public class StringIDHeader : TagStructure
    {
        [TagField(MinVersion = CacheVersion.Halo2Alpha, MaxVersion = CacheVersion.Halo3Beta)]
        public uint BufferAlignedOffset;

        public int Count;

        [TagField(Platform = CachePlatform.MCC)]
        public uint BufferOffsetMCC;

        public int BufferSize;
        public uint IndicesOffset;

        [TagField(Platform = CachePlatform.Original)]
        public uint BufferOffset;

        [TagField(Platform = CachePlatform.MCC)]
        public int NamespacesCount;
        [TagField(Platform = CachePlatform.MCC)]
        public uint NamespacesOffset;
    }

    [TagStructure(Size = 0x10)]
    public class TagNameHeader
    {
        public int TagNamesCount;
        public uint TagNamesBufferOffset;
        public int TagNamesBufferSize;
        public uint TagNameIndicesOffset;
    }

    [TagStructure(Size = 0xC, MaxVersion = CacheVersion.HaloCustomEdition)]
    [TagStructure(Size = 0xC, MinVersion = CacheVersion.Halo2Alpha, MaxVersion = CacheVersion.Halo2Xbox)]
    [TagStructure(Size = 0x10, MinVersion = CacheVersion.Halo2PC, MaxVersion = CacheVersion.Halo2PC)]
    [TagStructure(Size = 0x8, MinVersion = CacheVersion.Halo3Beta)]
    public class TagMemoryHeader
    {
        [TagField(MaxVersion = CacheVersion.HaloCustomEdition)]
        public int TagDataSize;

        public uint MemoryBufferOffset;
        public int MemoryBufferSize;

        [TagField(MinVersion = CacheVersion.Halo2Alpha, MaxVersion = CacheVersion.Halo2PC)]
        public int MemoryBufferCapacity;

        [TagField(MinVersion = CacheVersion.Halo2PC, MaxVersion = CacheVersion.Halo2PC)]
        public uint VirtualAddress;
    }
}
