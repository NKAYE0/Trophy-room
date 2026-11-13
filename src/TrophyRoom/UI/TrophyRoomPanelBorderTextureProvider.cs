using System;
using System.IO;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.TwoDimension;
using EngineTextureObject = TaleWorlds.Engine.Texture;

namespace TrophyRoom.UI
{
    // Supplies the golden woven border image (Assets/trophyroom_panel_border.png) to TextureWidgets in the trophy room.
    public class TrophyRoomPanelBorderTextureProvider : TextureProvider
    {
        private const string ModuleId = "TrophyRoom";
        private const string ImageFile = "trophyroom_panel_border.png";

        private static EngineTextureObject _engineTexture;
        private static bool _loadFailed;

        protected override Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
        {
            EngineTextureObject texture = LoadTexture();
            return texture != null ? new Texture(new EngineTexture(texture)) : null;
        }

        // Loads the image once and reuses it; logs and gives up quietly if the file is missing or unreadable.
        private static EngineTextureObject LoadTexture()
        {
            if (_engineTexture != null || _loadFailed)
            {
                return _engineTexture;
            }

            try
            {
                string path = Path.Combine(ModuleHelper.GetModuleFullPath(ModuleId), "Assets", ImageFile);
                _engineTexture = EngineTextureObject.CreateFromMemory(File.ReadAllBytes(path));
                _loadFailed = _engineTexture == null;
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                Debug.Print("[TrophyRoom] Could not load panel border image: " + ex.Message);
            }

            return _engineTexture;
        }
    }
}
