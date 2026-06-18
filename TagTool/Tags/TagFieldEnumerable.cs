using TagTool.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using TagTool.Common;
using TagTool.Commands.Common;
using System.Runtime.CompilerServices;
using TagTool.Common.Logging;

namespace TagTool.Tags
{
	/// <summary>
	/// Allows easy enumeration over a tag structure's elements and filtering by version.
	/// </summary>
	public class TagFieldEnumerable : IEnumerable<TagFieldInfo>
	{
		/// <summary>
		/// Collection of <see cref="Tags.TagFieldInfo"/> for a <see cref="TagStructureInfo"/> in a given
		/// <see cref="CacheVersion"/>.
		/// </summary>
		private readonly List<TagFieldInfo> TagFieldInfos = new List<TagFieldInfo> { };

		/// <summary>
		/// Constructs a <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/> over a tag structure for a <see cref="TagStructureInfo"/> in a given
		/// <see cref="CacheVersion"/>.
		/// </summary>
		/// <param name="info">The info for the structure. Only fields which match the version used to create the info will be enumerated over.</param>
		public TagFieldEnumerable(TagStructureInfo info)
		{
			Info = info;
			Build();
		}

		/// <summary>
		/// Gets the info that was used to construct the enumerator.
		/// </summary>
		public TagStructureInfo Info { get; private set; }

		/// <summary>
		/// Gets information about the current field.
		/// </summary>
		public TagFieldInfo TagFieldInfo { get; private set; }

		/// <summary>
		/// Gets the count of <see cref="TagFieldInfos"/>.
		/// </summary>
		public int Count => TagFieldInfos.Count;

		//standard .NET pattern, allows avoidance of boxing, and inlining better
		public struct Enumerator : IEnumerator<TagFieldInfo>
		{
			private List<TagFieldInfo>.Enumerator enumerator;
            public TagFieldInfo Current => enumerator.Current;
            object IEnumerator.Current => enumerator.Current;
			public void Dispose() => enumerator.Dispose();
			public bool MoveNext() => enumerator.MoveNext();
			void IEnumerator.Reset() => ((IEnumerator<TagFieldInfo>)enumerator).Reset();
        }

		/// <summary>
		/// Gets an <see cref="IEnumerator{T}"/> over the <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/>.
		/// </summary>
		/// <returns></returns>
		public Enumerator GetEnumerator()
		{
			var impl = TagFieldInfos.GetEnumerator();
			//we use Unsafe.As to avoid having to expose a constructor which takes the enumerator, since this would expose implementation details
			//it's safe since we have a struct with exactly 1 field of the same type
			return Unsafe.As<List<TagFieldInfo>.Enumerator, Enumerator>(ref impl);
		}
		IEnumerator<TagFieldInfo> IEnumerable<TagFieldInfo>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// An indexer into the <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/>.
		/// </summary>
		/// <param name="index">The index into the <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/>.</param>
		/// <returns>The <see cref="Tags.TagFieldInfo"/> at the specified index in the 
		/// <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/>.</returns>
		public TagFieldInfo this[int index] => TagFieldInfos[index];

		/// <summary>
		/// Builds the <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/> to be enumerated.
		/// </summary>
		private void Build()
		{
			uint offset = 0;

			// Build the field list. Scan through the type's inheritance
			// hierarchy and add any fields belonging to tag structures.
			foreach (var type in Info.Types.Reverse<Type>())
			{
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					var attr = GetTagFieldAttribute(field, Info.Version, Info.CachePlatform);
					if (attr == null)
						continue;

					CreateTagFieldInfo(field, attr, Info.Version, Info.CachePlatform, ref offset);
				}
			}

#if DEBUG
			uint expectedSize = Info.TotalSize;
			if(Info.Structure.Align > 0)
				offset = offset + (Info.Structure.Align - 1) & ~(Info.Structure.Align - 1);
			if (Info.Structure.Align > 0)
				expectedSize = expectedSize + (Info.Structure.Align - 1) & ~(Info.Structure.Align - 1);

			var typename = Info.Types[0].FullName.Replace("TagTool.", "").Replace("Tags.Definitions.", "");
			if (offset != expectedSize)
				Log.Warning($"Bad Size. Version: {Info.Version}:{Info.CachePlatform}, Type: '{typename}', Expected: 0x{expectedSize:X}, Actual: 0x{offset:X}");
#endif
		}

        private static TagFieldAttribute GetTagFieldAttribute(FieldInfo field, CacheVersion version, CachePlatform platform)
        {
            var attributes = (TagFieldAttribute[])field.GetCustomAttributes<TagFieldAttribute>(false);
			if (attributes.Length == 0)
				return TagFieldAttribute.Default;

            foreach (var attr in attributes)
            {
                if (CacheVersionDetection.TestAttribute(attr, version, platform))
                    return attr;
            }

			return null;
        }

        /// <summary>
        /// Creates and adds a <see cref="Tags.TagFieldInfo"/> to the <see cref="Tags.TagFieldInfo"/> <see cref="List{T}"/>.
        /// </summary>
        /// <param name="field">The <see cref="FieldInfo"/> to create the <see cref="Tags.TagFieldInfo"/> from.</param>
        /// <param name="attribute">The <see cref="TagFieldAttribute"/> for the <see cref="Tags.TagFieldInfo"/>.</param>
        /// <param name="targetVersion">The target <see cref="CacheVersion"/> the <see cref="Tags.TagFieldInfo"/> belongs to.</param>
        /// <param name="cachePlatform"></param>
        /// <param name="offset">The offset (in bytes) of the field. Gets updated to reflect the new offset following field.</param>
        private void CreateTagFieldInfo(FieldInfo field, TagFieldAttribute attribute, CacheVersion targetVersion, CachePlatform cachePlatform, ref uint offset)
        {
			ValidateField(field, attribute, targetVersion, cachePlatform);

            var fieldSize = TagFieldInfo.GetFieldSize(field.FieldType, attribute, targetVersion, cachePlatform);

            if (fieldSize == 0 && !attribute.Flags.HasFlag(TagFieldFlags.Runtime))
                throw new InvalidOperationException();

            uint align = TagFieldInfo.GetFieldAlignment(field.FieldType, attribute, targetVersion, cachePlatform);
            if (align > 0)
                offset = offset + (align - 1) & ~(align - 1);

            var tagFieldInfo = new TagFieldInfo(field, attribute, offset, fieldSize);
            TagFieldInfos.Add(tagFieldInfo);
            offset += fieldSize;
        }
        /// <summary>
        /// Finds a <see cref="TagFieldInfo"/> based on a <see cref="FieldInfo"/> <see cref="Predicate{T}"/>.
        /// </summary>
        /// <param name="match">The <see cref="FieldInfo"/> <see cref="Predicate{T}"/> to query.</param>
        /// <returns></returns>
        public FieldInfo Find(Predicate<FieldInfo> match) =>
			TagFieldInfos.Find(f => match.Invoke(f.FieldInfo))?.FieldInfo ?? null;

		private static void ValidateField(FieldInfo field, TagFieldAttribute attribute, CacheVersion targetVersion, CachePlatform cachePlatform)
        {
#if DEBUG
			if(attribute.Flags.HasFlag(TagFieldFlags.Padding))
            {
				if (field.FieldType != typeof(byte[]))
					new Exception("Padding fields must be of type Byte[]");
            }
            else
            {
				ValidateEnumRequiments(field, attribute, targetVersion, cachePlatform);
			}
#endif
		}

		private static void ValidateEnumRequiments(FieldInfo field, TagFieldAttribute attribute, CacheVersion targetVersion, CachePlatform cachePlatform)
		{
			if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(BitFlags<>))
			{
				var enumType = field.FieldType.GenericTypeArguments[0];
				var info = TagEnum.GetInfo(enumType, targetVersion, cachePlatform);

                if (!info.IsVersioned)
					throw new Exception("BitFlags Enum must have a 'TagEnum' attribute with IsVersioned=True");
                
                if (attribute.EnumType == null)
                    throw new Exception("BitFlags Enum must have the 'EnumType' TagField attribute set");
			}
			else if(field.FieldType.IsEnum)
			{
				var info = TagEnum.GetInfo(field.FieldType, targetVersion, cachePlatform);

				if (info.IsVersioned)
				{
					if (attribute.EnumType == null)
						throw new Exception("Versioned Enum must have the 'EnumType' TagField attribute set");
				}
			}
		}
	}
}