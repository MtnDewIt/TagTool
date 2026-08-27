using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagTool.Common;

namespace TagTool.Tags
{
    public class TagGroup : TagStructure, IEquatable<TagGroup>
    {
        public Tag Tag;
        public Tag ParentTag;
        public Tag GrandParentTag;

        public TagGroup() : this(Tag.Null, Tag.Null, Tag.Null){}

        public TagGroup(Tag tag, Tag parentTag, Tag grandparentTag)
        {
            Tag = tag;
            ParentTag = parentTag;
            GrandParentTag = grandparentTag;
        }

        public TagGroup(Tag tag, Tag parentTag)
        {
            Tag = tag;
            ParentTag = parentTag;
            GrandParentTag = Tag.Null;
        }

        public TagGroup(Tag tag) 
        {
            Tag = tag;
            ParentTag = Tag.Null;
            GrandParentTag = Tag.Null;
        }

        /// <summary>
        /// Determines whether this group is a subgroup of another group.
        /// </summary>
        /// <param name="groups">The group to check.</param>
        /// <returns><c>true</c> if this group is a subgroup of the other group.</returns>
        public bool BelongsTo(params IReadOnlyList<TagGroup> groups)
		{
			foreach (var group in groups)
			{
				var _groupTag = group.Tag;
				if (Tag.Equals(_groupTag) || ParentTag.Equals(_groupTag) || GrandParentTag.Equals(_groupTag)) return true;
			}
			return false;
		}

        /// <summary>
        /// Determines whether this group is a subgroup of another group.
        /// </summary>
        /// <param name="groupTags">The group tag to check, as a 4-character string.</param>
        /// <returns><c>true</c> if this group is a subgroup of the group tag.</returns>
        public bool BelongsTo(params IReadOnlyList<string> groupTags)
        {
			foreach (var groupTag in groupTags)
			{
				var _groupTag = new Tag(groupTag);
				if (Tag.Equals(_groupTag) || ParentTag.Equals(_groupTag) || GrandParentTag.Equals(_groupTag)) return true;
			}
			return false;
        }

        /// <summary>
        /// Determines whether this group is a subgroup of another group.
        /// </summary>
        /// <param name="groupTags">The group tag to check.</param>
        /// <returns><c>true</c> if this group is a subgroup of the group tag.</returns>
        public bool BelongsTo(params IReadOnlyList<Tag> groupTags)
        {
            foreach (var groupTag in groupTags)
                if (Tag.Equals(groupTag) || ParentTag.Equals(groupTag) || GrandParentTag.Equals(groupTag))
                    return true;

            return false;
        }

		public bool BelongsTo(Tag groupTag)
		{
			if (Tag.Equals(groupTag) || ParentTag.Equals(groupTag) || GrandParentTag.Equals(groupTag))
				return true;

			return false;
		}

		public bool Equals(TagGroup other)
        {
            if (other == null)
                return false;

            return Tag == other.Tag && ParentTag == other.ParentTag && GrandParentTag == other.GrandParentTag;
        }

        public override bool Equals(object obj)
        {
            return obj is TagGroup other && Equals(other);
        }

        public static bool operator ==(TagGroup lhs, TagGroup rhs)
        {
            return EqualityComparer<TagGroup>.Default.Equals(lhs, rhs);
        }

        public static bool operator !=(TagGroup lhs, TagGroup rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            var result = 13;
            result = result * 17 + Tag.GetHashCode();
            result = result * 17 + ParentTag.GetHashCode();
            result = result * 17 + GrandParentTag.GetHashCode();
            return result;
        }

        public override string ToString() => Tag.ToString();

        public bool IsNull()
        {
            if (Tag.IsNull())
                return true;
            return false;
        }
    }
}
