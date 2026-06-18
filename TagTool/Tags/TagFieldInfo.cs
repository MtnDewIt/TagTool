using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Geometry.BspCollisionGeometry;
using TagTool.Shaders;
using static TagTool.Tags.TagFieldFlags;
using static TagTool.Tags.TagFieldInfo;

namespace TagTool.Tags
{
	/// <summary>
	/// Class for pairing of <see cref="System.Reflection.FieldInfo"/> and <see cref="TagFieldAttribute"/>.
	/// </summary>
	public class TagFieldInfo
    {
        private readonly TypedValueSetter _typedValueSetter;
        private readonly TypedValueGetter _typedValueGetter;
        /// <summary>
        /// Constructs a <see cref="TagFieldInfo"/> from a <see cref="System.Reflection.FieldInfo"/> and <see cref="TagFieldAttribute"/>.
        /// </summary>
        /// <param name="field">The <see cref="System.Reflection.FieldInfo"/> for the field.</param>
        /// <param name="attribute">The <see cref="TagFieldAttribute"/> for the field.</param>
        /// <param name="offset">The offset (in bytes) of the field in it's structure.</param>
        /// <param name="size">The size of the field (in bytes).</param>
        public TagFieldInfo(FieldInfo field, TagFieldAttribute attribute, uint offset, uint size)
		{
			FieldInfo = field;
			Size = size;
			Offset = offset;
			Attribute = attribute;
			SetValue = CreateSetter(this);
			GetValue = CreateGetter(this);
			_typedValueSetter = CreateTypedSetter(this.FieldInfo);
			_typedValueGetter = CreateTypedGetter(this.FieldInfo);
        }

		/// <summary>
		/// Gets the <see cref="System.Reflection.FieldInfo"/> that was used in construction.
		/// </summary>
		public FieldInfo FieldInfo { get; }

		// Expose the FieldInfo's properties more directly.
		public MemberTypes MemberType => FieldInfo.MemberType;
		public string Name => FieldInfo.Name;
		public Type DeclaringType => FieldInfo.DeclaringType;
		public Type ReflectedType => FieldInfo.ReflectedType;
		public int MetadataToken => FieldInfo.MetadataToken;
		public Module Module => FieldInfo.Module;
		public Type FieldType => FieldInfo.FieldType;

		/// <summary>
		/// Gets the field size (in bytes) that was used in construction.
		/// </summary>
		public uint Size { get; }

		/// <summary>
		/// Gets the field offset (in bytes) that was used in construction.
		/// </summary>
		public uint Offset { get; }

		/// <summary>
		/// Gets the <see cref="TagFieldAttribute"/> that was used in construction.
		/// </summary>
		public TagFieldAttribute Attribute { get; }

		/// <summary>
		/// Encapsulates a method for SETTING this field's value on it's owner.
		/// Usage: 'tagFieldInfo.SetValue(object owner, object value);'
		/// </summary>
		public readonly ValueSetter SetValue;

		/// <summary>
		/// Encapsulates a method for GETTING this field's value on it's owner.
		/// Usage: 'var value = tagFieldInfo.GetValue(object owner);'
		/// </summary>
		public readonly ValueGetter GetValue;


        public void SetValueTyped<T>(object instance, T value)
        {
            _typedValueSetter(instance, __makeref(value));
        }

        public T GetValueTyped<T>(object instance)
        {
			T val = default;
            _typedValueGetter(instance, __makeref(val));
			return val;
        }

        /// <summary>
        /// A <see cref="Delegate"/> for SETTING the value of a field on it's owner.
        /// </summary>
        /// <param name="owner">The <see cref="object"/> that owns the field.</param>
        /// <param name="value">The <see cref="object"/> to SET the value of the field to.</param>
        public delegate void ValueSetter(object owner, object value);

		/// <summary>
		/// A <see cref="Delegate"/> for GETTING the value of a field on it's owner.
		/// </summary>
		/// <param name="owner">The <see cref="object"/> that owns the field.</param>
		/// <returns>The value of the field on it's owner.</returns>
		public delegate object ValueGetter(object owner);

		private static ValueSetter CreateSetter(TagFieldInfo tagFieldInfo)
		{
			var ownerType = tagFieldInfo.DeclaringType;
			var valueType = tagFieldInfo.FieldType;

			if (ownerType.IsGenericTypeDefinition)
				ownerType = ownerType.MakeGenericType(valueType);

			// Parameter "target", the object on which to set the field `field`.
			var ownerParam = Expression.Parameter(typeof(object));

			// Parameter "value" the value to be set in the `field` on "target".
			var valueParam = Expression.Parameter(typeof(object));

			// Unbox structs to their type, or cast a class to it's type.
			var castTartgetExp = ownerType.IsValueType ?
				Expression.Unbox(ownerParam, ownerType) : Expression.Convert(ownerParam, ownerType);

			// Cast the value to its correct type.
			var castValueExp = Expression.Convert(valueParam, valueType);

			// Access the field
			var fieldExp = Expression.Field(castTartgetExp, tagFieldInfo.FieldInfo);

			// Assign the "value" to the `field`.
			var assignExp = Expression.Assign(fieldExp, castValueExp);


			// Compile the whole thing and return.
			var setter = Expression.Lambda<ValueSetter>(assignExp, ownerParam, valueParam).Compile();
			return setter;
		}

		private static ValueGetter CreateGetter(TagFieldInfo tagFieldInfo)
		{
			var ownerType = tagFieldInfo.DeclaringType;

			// Parameter "owner", the object on which to get the field value from.
			ParameterExpression ownerParam = Expression.Parameter(typeof(object));

			// Unbox structs to their type, or cast a class to it's type.
			Expression castTartgetExp = ownerType.IsValueType ?
				Expression.Unbox(ownerParam, ownerType) : Expression.Convert(ownerParam, ownerType);

			// Access the field
			MemberExpression fieldExp = Expression.Field(castTartgetExp, tagFieldInfo.FieldInfo);

			// Convert field to object Type
			UnaryExpression boxedExp = Expression.Convert(fieldExp, typeof(object));

			var getter = Expression.Lambda<ValueGetter>(boxedExp, ownerParam).Compile();
			return getter;
		}


        public delegate void TypedValueSetter(object instance, TypedReference value);
        public delegate void TypedValueGetter(object instance, TypedReference value);


        private static TypedValueGetter CreateTypedGetter(FieldInfo fieldInfo)
        {
            var dm = new DynamicMethod(
                $"{fieldInfo.Name}_GetValueTyped",
                typeof(void),
                [typeof(object), typeof(TypedReference)],
                fieldInfo.Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_1); // Load TypedReference
            il.Emit(OpCodes.Refanyval, fieldInfo.FieldType); // Get the TypedReference's value address
            il.Emit(OpCodes.Ldarg_0); // Load instance
            il.Emit(OpCodes.Ldfld, fieldInfo); // Load the field value

            if (fieldInfo.FieldType == typeof(byte) || fieldInfo.FieldType == typeof(sbyte))
                il.Emit(OpCodes.Stind_I1);
            else if (fieldInfo.FieldType == typeof(short) || fieldInfo.FieldType == typeof(ushort))
                il.Emit(OpCodes.Stind_I2);
            else if (fieldInfo.FieldType == typeof(int) || fieldInfo.FieldType == typeof(uint))
                il.Emit(OpCodes.Stind_I4);
            else if (fieldInfo.FieldType == typeof(long) || fieldInfo.FieldType == typeof(ulong))
                il.Emit(OpCodes.Stind_I8);
            else if (fieldInfo.FieldType == typeof(float))
                il.Emit(OpCodes.Stind_R4);
            else if (fieldInfo.FieldType == typeof(double))
                il.Emit(OpCodes.Stind_R8);
            else if (fieldInfo.FieldType.IsValueType)
                il.Emit(OpCodes.Stobj, fieldInfo.FieldType);
            else if (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsInterface)
                il.Emit(OpCodes.Stind_Ref);
            else
                throw new NotSupportedException($"Field type {fieldInfo.FieldType} is not supported.");

            il.Emit(OpCodes.Ret);

            return (TypedValueGetter)dm.CreateDelegate(typeof(TypedValueGetter));
        }

        private static TypedValueSetter CreateTypedSetter(FieldInfo fieldInfo)
        {
            var dm = new DynamicMethod($"{fieldInfo.Name}_SetValueTyped", typeof(void), [typeof(object), typeof(TypedReference)]);

            ILGenerator il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // load instance
            il.Emit(OpCodes.Ldflda, fieldInfo); // load the field address

            il.Emit(OpCodes.Ldarg_1); // TypedReference
            il.Emit(OpCodes.Refanyval, fieldInfo.FieldType); // load address

            if (fieldInfo.FieldType == typeof(byte))
            {
                il.Emit(OpCodes.Ldind_U1);
                il.Emit(OpCodes.Stind_I1);
            }
            else if (fieldInfo.FieldType == typeof(sbyte))
            {
                il.Emit(OpCodes.Ldind_I1);
                il.Emit(OpCodes.Stind_I1);
            }
            else if (fieldInfo.FieldType == typeof(short))
            {
                il.Emit(OpCodes.Ldind_I2);
                il.Emit(OpCodes.Stind_I2);
            }
            else if (fieldInfo.FieldType == typeof(ushort))
            {
                il.Emit(OpCodes.Ldind_U2);
                il.Emit(OpCodes.Stind_I2);
            }
            else if (fieldInfo.FieldType == typeof(int))
            {
                il.Emit(OpCodes.Ldind_I4);
                il.Emit(OpCodes.Stind_I4);
            }
            else if (fieldInfo.FieldType == typeof(uint))
            {
                il.Emit(OpCodes.Ldind_U4);
                il.Emit(OpCodes.Stind_I4);
            }
            else if (fieldInfo.FieldType == typeof(long) || fieldInfo.FieldType == typeof(ulong))
            {
                il.Emit(OpCodes.Ldind_I8);
                il.Emit(OpCodes.Stind_I8);
            }
            else if (fieldInfo.FieldType == typeof(float))
            {
                il.Emit(OpCodes.Ldind_R4);
                il.Emit(OpCodes.Stind_R4);
            }
            else if (fieldInfo.FieldType == typeof(double))
            {
                il.Emit(OpCodes.Ldind_R8);
                il.Emit(OpCodes.Stind_R8);
            }
            else if (fieldInfo.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Ldobj, fieldInfo.FieldType);
                il.Emit(OpCodes.Stobj, fieldInfo.FieldType);
            }
            else if (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsInterface)
            {
                il.Emit(OpCodes.Ldind_Ref);
                il.Emit(OpCodes.Stind_Ref);
            }
            else
                throw new NotSupportedException($"Field type {fieldInfo.FieldType} is not supported.");

            il.Emit(OpCodes.Ret);

            return (TypedValueSetter)dm.CreateDelegate(typeof(TypedValueSetter));
        }

        /// <summary>
        /// Gets the size of a tag-field.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the field.</param>
        /// <param name="attr">The <see cref="TagFieldAttribute"/> of the field.</param>
        /// <param name="targetVersion">The <see cref="CacheVersion"/> to target.</param>
        /// <param name="cachePlatform"></param>
        /// <returns></returns>
        public static uint GetFieldSize(Type type, TagFieldAttribute attr, CacheVersion targetVersion, CachePlatform cachePlatform)
		{
            if (attr.Flags.HasFlag(Runtime))
                return 0;

			if(type.IsEnum)
				return TagFieldInfo.GetFieldSize(attr.EnumType ?? type.GetEnumUnderlyingType(), attr, targetVersion, cachePlatform);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					return 0x01;

				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Object when type == typeof(IndexBufferIndex) && (targetVersion <= CacheVersion.Halo3ODST || (targetVersion >= CacheVersion.HaloOnline106708 && targetVersion < CacheVersion.HaloReach)):
					return 0x02;

				case TypeCode.Single:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Object when attr != null && attr.Flags.HasFlag(TagFieldFlags.Pointer):
				case TypeCode.Object when type == typeof(Tag):
				case TypeCode.Object when type == typeof(CacheAddress):
				case TypeCode.Object when type == typeof(CachedTag) && attr.Flags.HasFlag(Short):
                //case TypeCode.Object when type == typeof(RgbColor):
                case TypeCode.Object when type == typeof(ArgbColor):
                case TypeCode.Object when type == typeof(Point2d):
				case TypeCode.Object when type == typeof(StringId):
				case TypeCode.Object when type == typeof(Angle):
				case TypeCode.Object when type == typeof(VertexShaderReference):
				case TypeCode.Object when type == typeof(PixelShaderReference):
				case TypeCode.Object when type == typeof(PlatformUnsignedValue) && CacheVersionDetection.GetPlatformType(cachePlatform) == PlatformType._32Bit:
				case TypeCode.Object when type == typeof(PlatformSignedValue) && CacheVersionDetection.GetPlatformType(cachePlatform) == PlatformType._32Bit:
				case TypeCode.Object when type == typeof(IndexBufferIndex) && (targetVersion >= CacheVersion.HaloReach || CacheVersionDetection.IsEldewrito(targetVersion)):
				case TypeCode.Object when type == typeof(StructureSurfaceToTriangleMapping):
					return 0x04;

				case TypeCode.Double:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Object when type == typeof(CachedTag) && targetVersion != CacheVersion.Unknown && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo2Alpha, CacheVersion.Halo2PC):
				case TypeCode.Object when attr.Length == 0 && type == typeof(byte[]) && targetVersion != CacheVersion.Unknown && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo2Alpha, CacheVersion.Halo2PC):
				case TypeCode.Object when type == typeof(Rectangle2d):
                case TypeCode.Object when type == typeof(RealEulerAngles2d):
				case TypeCode.Object when type == typeof(RealPoint2d):
				case TypeCode.Object when type == typeof(RealVector2d):
				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && targetVersion != CacheVersion.Unknown && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo2Alpha, CacheVersion.Halo2PC):
				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TagBlock<>) && targetVersion != CacheVersion.Unknown && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo2Alpha, CacheVersion.Halo2PC):
				case TypeCode.Object when type == typeof(PlatformUnsignedValue) && CacheVersionDetection.GetPlatformType(cachePlatform) == PlatformType._64Bit:
				case TypeCode.Object when type == typeof(PlatformSignedValue) && CacheVersionDetection.GetPlatformType(cachePlatform) == PlatformType._64Bit:
					return 0x08;

				case TypeCode.Object when type == typeof(RealRgbColor):
				case TypeCode.Object when type == typeof(RealEulerAngles3d):
				case TypeCode.Object when type == typeof(RealPoint3d):
				case TypeCode.Object when type == typeof(RealVector3d):
				case TypeCode.Object when type == typeof(RealPlane2d):
				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo3Beta, CacheVersion.Unknown):
				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TagBlock<>) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo3Beta, CacheVersion.Unknown):
                case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.HaloXbox, CacheVersion.HaloCustomEdition):
                case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TagBlock<>) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.HaloXbox, CacheVersion.HaloCustomEdition):
				case TypeCode.Object when attr.Length == 0 && type == typeof(byte[]) && targetVersion != CacheVersion.Unknown && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.HaloXbox, CacheVersion.HaloCustomEdition):
					return 0x0C;

				case TypeCode.Decimal:
				case TypeCode.Object when type == typeof(CachedTag) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo3Beta, CacheVersion.Unknown):
                case TypeCode.Object when type == typeof(CachedTag) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.HaloXbox, CacheVersion.HaloCustomEdition):
                case TypeCode.Object when type == typeof(RealArgbColor):
				case TypeCode.Object when type == typeof(RealRgbaColor):
				case TypeCode.Object when type == typeof(RealQuaternion):
				case TypeCode.Object when type == typeof(RealVector4d):
				case TypeCode.Object when type == typeof(RealPlane3d):
					return 0x10;

				case TypeCode.Object when attr.Length == 0 && type == typeof(byte[]) && CacheVersionDetection.IsBetween(targetVersion, CacheVersion.Halo3Beta, CacheVersion.Unknown):
				case TypeCode.Object when type == typeof(TagData):
					return 0x14;

                case TypeCode.Object when type == typeof(RealBoundingBox):
				case TypeCode.Object when type == typeof(RealRectangle3d):
					return 0x18;

				case TypeCode.Object when type == typeof(RealMatrix4x3):
					return 0x30;

                case TypeCode.Object when type == typeof(DatumHandle):
                    return sizeof(uint);

				case TypeCode.String when attr.CharSet == System.Runtime.InteropServices.CharSet.Ansi:
					return (uint)attr.Length;
				case TypeCode.String when attr.CharSet == System.Runtime.InteropServices.CharSet.Unicode:
					return (uint)attr.Length * 2;

				case TypeCode.Object when type.IsArray && attr.Length != 0:
					return TagFieldInfo.GetFieldSize(type.GetElementType(), attr, targetVersion, cachePlatform) * (uint)attr.Length;

				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Bounds<>):
					return TagFieldInfo.GetFieldSize(type.GenericTypeArguments[0], attr, targetVersion, cachePlatform) * 2;

                case TypeCode.Object when type.IsSubclassOf(typeof(TagStructure)):
                    return TagStructure.GetTagStructureInfo(type, targetVersion, cachePlatform).TotalSize;

				case TypeCode.Object when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BitFlags<>):
					return TagFieldInfo.GetFieldSize(type.GenericTypeArguments[0], attr, targetVersion, cachePlatform);

				default:
					return TagStructure.GetTagStructureInfo(type, targetVersion, cachePlatform).TotalSize;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetFieldAlignment(Type type, TagFieldAttribute attr, CacheVersion targetVersion, CachePlatform cachePlatform)
        {
			// We could do implicit alignment for all fields, however for now, for performance, and to reduce the chance of regression, 
			// keeping it to platform speicfic types, or if alignment was explictly asked for with the Align TagFieldAttribute.

			if (attr.Align > 0)
				return attr.Align;

			if (type == typeof(PlatformUnsignedValue) || type == typeof(PlatformSignedValue))
				return cachePlatform == CachePlatform.MCC ? 8u : 4u;

            return 0;
        }

		/// <summary>
		/// Check whether a field is in the given cache version and platform
		/// </summary>
        public static bool FieldInCacheVersion(TagFieldInfo field, CacheVersion version, CachePlatform platform)
        {
            var attributes = (TagFieldAttribute[])field.FieldInfo.GetCustomAttributes<TagFieldAttribute>(false);
            if (attributes.Length == 0)
                return true;

            foreach (TagFieldAttribute attr in attributes)
            {
                if (CacheVersionDetection.TestAttribute(attr, version, platform))
                    return true;
            }

            return false;
        }
    }
}