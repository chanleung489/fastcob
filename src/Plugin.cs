using BepInEx;
using Expedition;
using RWCustom;
using System.Security.Permissions;
using UnityEngine;


// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Fastcob;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
sealed class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "znery.fastcob";
    public const string PLUGIN_NAME = "Fastcob";
    public const string PLUGIN_VERSION = "0.1";

    bool init;
    // bool traced = false;
    const int one_second = 40;
    int timer;
    int drawCobTries;
    int drawCobCalls;
    int drawCobTotal;

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        // On.RainWorldGame.Update += OnGameUpdate;
        On.RoomCamera.SpriteLeaser.Update += OnSpriteLeaserUpdate;
        // On.Room.AddObject += OnAddObject;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI(PLUGIN_GUID, new FastcobOptions());
        Logger.LogDebug("Options");
        Logger.LogDebug("skipRender: " + FastcobOptions.skipRendering.Value);

        if (init) return;
        init = true;
        Logger.LogDebug("Init");
    }

    void OnGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        timer++;
        if (timer > 40)
        {
            drawCobTotal += drawCobCalls;
            Logger.LogDebug(drawCobTotal + " " + drawCobCalls);
            drawCobCalls = 0;
            timer = 0;
        }
    }

    void OnSpriteLeaserUpdate(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
    {
        RainWorld.CurrentlyDrawingObject = self.drawableObject;
        if (self.drawableObject is SeedCob)
        {
            drawCobTries++;
            if (drawCobTries < FastcobOptions.skipRendering.Value) return;
            drawCobTries = 0;
        }
        self.drawableObject.DrawSprites(self, rCam, timeStacker, camPos);
        if (self.drawableObject is CosmeticSprite)
        {
            (self.drawableObject as CosmeticSprite).PausedDrawSprites(self, rCam, timeStacker, camPos);
        }
        if (ModManager.Expedition && RWCustom.Custom.rainWorld.ExpeditionMode && ExpeditionGame.egg != null)
        {
            self.rbUpdate(timeStacker);
        }
    }
}
