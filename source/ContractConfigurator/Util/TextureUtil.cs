using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using DDSHeaders;

namespace ContractConfigurator.Util
{
    public class TextureUtil
    {
        public static Texture2D LoadTexture(string url)
        {
            // Check cache for texture
            Texture2D texture;
            try
            {
                string path = "GameData/" + url;
                // PNG loading
                if (File.Exists(path) && path.Contains(".png"))
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.LoadImage(File.ReadAllBytes(path.Replace('/', Path.DirectorySeparatorChar)));
                }
                // DDS loading
                else if (File.Exists(path) && path.Contains(".dds"))
                {
                    BinaryReader br = new BinaryReader(new MemoryStream(File.ReadAllBytes(path)));

                    if (br.ReadUInt32() != DDSValues.uintMagic)
                    {
                        throw new Exception("Format issue with DDS texture '" + path + "'!");
                    }
                    DDSHeader ddsHeader = new DDSHeader(br);
                    if (ddsHeader.ddspf.dwFourCC == DDSValues.uintDX10)
                    {
                        DDSHeaderDX10 ddsHeaderDx10 = new DDSHeaderDX10(br);
                    }

                    TextureFormat texFormat;
                    if (ddsHeader.ddspf.dwFourCC == DDSValues.uintDXT1)
                    {
                        texFormat = UnityEngine.TextureFormat.DXT1;
                    }
                    else if (ddsHeader.ddspf.dwFourCC == DDSValues.uintDXT3)
                    {
                        texFormat = UnityEngine.TextureFormat.DXT1 | UnityEngine.TextureFormat.Alpha8;
                    }
                    else if (ddsHeader.ddspf.dwFourCC == DDSValues.uintDXT5)
                    {
                        texFormat = UnityEngine.TextureFormat.DXT5;
                    }
                    else
                    {
                        throw new Exception("Unhandled DDS format!");
                    }

                    texture = new Texture2D((int)ddsHeader.dwWidth, (int)ddsHeader.dwHeight, texFormat, false);
                    texture.LoadRawTextureData(br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position)));
                    texture.Apply(false, true);
                }
                else
                {
                    throw new Exception(StringBuilderCache.Format("Couldn't find file for image '{0}'", url));
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(typeof(TextureUtil), "Couldn't create texture for '{0}'!", url);
                LoggingUtil.LogException(e);
                texture = null;
            }

            return texture;
        }
    }
}
