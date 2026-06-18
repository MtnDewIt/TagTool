using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Threading.Tasks;
using TagTool.Bitmaps;
using TagTool.Bitmaps.Utils;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Commands.Gen2.Bitmaps;
using TagTool.Common;
using TagTool.IO;
using TagTool.Tags.Definitions;
using BitmapGen2 = TagTool.Tags.Definitions.Gen2.Bitmap;

namespace TagTool.Porting.Gen2
{
	partial class PortingContextGen2
	{
        public Bitmap ConvertBitmap(BitmapGen2 gen2Bitmap, string gen2TagName)
        {
            Bitmap newBitmap = new Bitmap
            {
                Flags = BitmapRuntimeFlags.UsingTagInteropAndTagResource,
                SpriteSpacing = gen2Bitmap.SpriteSpacing,
                BumpMapHeight = gen2Bitmap.BumpHeight,
                FadeFactor = gen2Bitmap.DetailFadeFactor,
                Sequences = new List<Bitmap.Sequence>(),
                Images = new List<Bitmap.Image>(),
                HardwareTextures = new List<TagTool.Tags.TagResourceReference>()
            };

            //convert sequences
            foreach (var gen2seq in gen2Bitmap.Sequences)
            {
                Bitmap.Sequence newSeq = new Bitmap.Sequence
                {
                    Name = gen2seq.Name,
                    FirstBitmapIndex = gen2seq.FirstBitmapIndex,
                    BitmapCount = gen2seq.BitmapCount,
                    Sprites = new List<Bitmap.Sequence.Sprite>()
                };
                foreach (var gen2spr in gen2seq.Sprites)
                {
                    newSeq.Sprites.Add(new Bitmap.Sequence.Sprite
                    {
                        BitmapIndex = gen2spr.BitmapIndex,
                        Left = gen2spr.Left,
                        Right = gen2spr.Right,
                        Top = gen2spr.Top,
                        Bottom = gen2spr.Bottom,
                        RegistrationPoint = gen2spr.RegistrationPoint
                    });
                }
                newBitmap.Sequences.Add(newSeq);
            }

            var converterOptions = new BitmapConverterGen2.ConverterOptions()
            {
                ForceDxt5nm = Options.UseExperimentalDxt5nm,
                HqNormalMapCompression = Options.HqNormalMapConversion
            };

            var tasks = new List<Task<(BaseBitmap, Bitmap.Image)>>();

            //convert images
            for (var i = 0; i < gen2Bitmap.Bitmaps.Count; i++)
            {
                var gen2Img = gen2Bitmap.Bitmaps[i];

                int index = i;
                tasks.Add(RunOnThreadPool(() =>
                {
                    BaseBitmap bitmapbase = BitmapConverterGen2.ConvertBitmap(
                        (GameCacheGen2)BlamCache,
                        gen2Bitmap,
                        index,
                        out Bitmap.Image newImg,
                        gen2TagName,
                        forDDS: false,
                        converterOptions
                        );

                    return (bitmapbase, newImg);
                }));
            }

            AddTask(Task.WhenAll(tasks)
                .ContinueWith(task =>
                {
                    // on main thread: Finish bitmap conversion

                    for (int i = 0; i < gen2Bitmap.Bitmaps.Count; i++)
                    {
                        (BaseBitmap bitmapbase, Bitmap.Image newImg) = tasks[i].GetAwaiter().GetResult();

                        var bitmapResourceDefinition = BitmapUtils.CreateBitmapTextureInteropResource(bitmapbase);
                        var resourceReference = CacheContext.ResourceCache.CreateBitmapResource(bitmapResourceDefinition);
                        newBitmap.HardwareTextures.Add(resourceReference);

                        newBitmap.Images.Add(newImg);
                    }

                }, MainThreadScheduler));

            //set scope mask data to make shit work
            if (gen2TagName.Contains("scope_masks"))
            {
                float newLeft = 1.325f;
                float newRight = 0.125f;
                float aspectRatio = (float)newBitmap.Images[0].Height / newBitmap.Images[0].Width;
                if (aspectRatio < 0.9f)
                {
                    float newModifier = aspectRatio * 0.18f;
                    newLeft -= newModifier;
                    newRight += (newModifier / 3);
                }
                newBitmap.SpriteSpacing = -80;
                newBitmap.Sequences[0].Sprites.Add(new Bitmap.Sequence.Sprite
                {
                    BitmapIndex = gen2Bitmap.Sequences[0].FirstBitmapIndex,
                    Left = newLeft,
                    Right = newRight,
                    Top = 1.325f,
                    Bottom = 0.1f,
                    RegistrationPoint = new RealPoint2d { X = 0, Y = 0 },
                });
            }

            return newBitmap;
        }
    }
}
