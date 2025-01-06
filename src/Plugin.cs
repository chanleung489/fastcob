using BepInEx;
using System.Security.Permissions;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using RWCustom;
using System.Collections.Generic;

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
    public const string PLUGIN_VERSION = "0.1.99";

    public struct NonNullLeaser
    {
        public RoomCamera.SpriteLeaser sLeaser;
        public RoomCamera rCam;
    }

    bool init;
    SeedcobDrawSpriteParallel seedcobParallelInstance = new SeedcobDrawSpriteParallel();
    public static new BepInEx.Logging.ManualLogSource Logger;

    public void OnEnable()
    {
        Logger = base.Logger;
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        On.RoomCamera.DrawUpdate += OnDrawUpdate;
        On.SeedCob.PlaceInRoom += OnPlaceSeedcob;
    }

    private void OnPlaceSeedcob(On.SeedCob.orig_PlaceInRoom orig, SeedCob self, Room placeRoom)
    {
        orig(self, placeRoom);
		self.stalkSegments = 10;
    }

    private void OnDrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        if (self.hud != null)
        {
            self.hud.Draw(timeStacker);
        }
        if (self.room == null)
        {
            return;
        }
        if (self.blizzardGraphics != null && self.room.blizzardGraphics == null)
        {
            self.blizzardGraphics.lerpBypass = true;
            self.room.AddObject(self.blizzardGraphics);
            self.room.blizzardGraphics = self.blizzardGraphics;
            self.room.blizzard = true;
        }
        bool flag = false;
        flag = self.fullscreenSync != Screen.fullScreen;
        if (self.snowChange || flag)
        {
            if (self.room.snow)
            {
                self.UpdateSnowLight();
            }
            if (self.blizzardGraphics != null)
            {
                self.blizzardGraphics.TileTexUpdate();
            }
        }
        self.fullscreenSync = Screen.fullScreen;
        Vector2 vector = Vector2.Lerp(self.lastPos, self.pos, timeStacker);
        self.virtualMicrophone.DrawUpdate(timeStacker, timeSpeed);
        if (self.microShake > 0f)
        {
            vector += RWCustom.Custom.RNV() * 8f * self.microShake * UnityEngine.Random.value;
        }
        if (!self.voidSeaMode)
        {
            vector.x = Mathf.Clamp(vector.x, self.CamPos(self.currentCameraPosition).x + self.hDisplace + 8f - 20f, self.CamPos(self.currentCameraPosition).x + self.hDisplace + 8f + 20f);
            vector.y = Mathf.Clamp(vector.y, self.CamPos(self.currentCameraPosition).y + 8f - 7f - (self.splitScreenMode ? 192f : 0f), self.CamPos(self.currentCameraPosition).y + 33f + (self.splitScreenMode ? 192f : 0f));
            self.levelGraphic.isVisible = true;
            if (self.backgroundGraphic.isVisible)
            {
                self.backgroundGraphic.color = Color.Lerp(self.currentPalette.blackColor, self.currentPalette.fogColor, self.currentPalette.fogAmount);
            }
        }
        else
        {
            self.levelGraphic.isVisible = false;
            if (!ModManager.MSC || !self.room.waterInverted)
            {
                vector.y = Mathf.Min(vector.y, -528f);
            }
            else
            {
                vector.y = Mathf.Max(vector.y, self.room.PixelHeight + 128f);
            }
        }
        vector = new Vector2(Mathf.Floor(vector.x), Mathf.Floor(vector.y));
        vector.x -= 0.02f;
        vector.y -= 0.02f;
        vector += self.offset;
        vector += self.hardLevelGfxOffset;
        if (self.waterLight != null)
        {
            if (self.room.gameOverRoom)
            {
                self.waterLight.CleanOut();
            }
            else
            {
                self.waterLight.DrawUpdate(vector);
            }
        }

        List<RoomCamera.SpriteLeaser> leaserList = new List<RoomCamera.SpriteLeaser>();

        for (int num = self.spriteLeasers.Count - 1; num >= 0; num--)
        {
            if (self.spriteLeasers[num].drawableObject is SeedCob)
            {
                leaserList.Add(self.spriteLeasers[num]);
            }
            else
            {
                self.spriteLeasers[num].Update(timeStacker, self, vector);
            }
            if (self.spriteLeasers[num].deleteMeNextFrame)
            {
                self.spriteLeasers.RemoveAt(num);
            }
        }
        var leasers = new NativeArray<NonNullLeaser>(leaserList.Count, Allocator.Temp);
        for (int i = 0; i < leaserList.Count; i++)
        {
            leasers[i] = new NonNullLeaser() { sLeaser = leaserList[i], rCam = self };
        }
        JobHandle jobHandle = seedcobParallelInstance.Update(leasers, timeStacker, self, vector);

        for (int i = 0; i < self.singleCameraDrawables.Count; i++)
        {
            self.singleCameraDrawables[i].Draw(self, timeStacker, vector);
        }
        if (self.room.game.DEBUGMODE)
        {
            self.levelGraphic.x = 5000f;
        }
        else
        {
            self.levelGraphic.x = self.CamPos(self.currentCameraPosition).x - vector.x;
            self.levelGraphic.y = self.CamPos(self.currentCameraPosition).y - vector.y;
            self.backgroundGraphic.x = self.CamPos(self.currentCameraPosition).x - vector.x;
            self.backgroundGraphic.y = self.CamPos(self.currentCameraPosition).y - vector.y;
        }
        if (Futile.subjectToAspectRatioIrregularity)
        {
            int num2 = (int)(self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.PixelShift) * 8f);
            self.levelGraphic.x -= num2 % 3;
            self.backgroundGraphic.x -= num2 % 3;
            self.levelGraphic.y -= num2 / 3;
            self.backgroundGraphic.y -= num2 / 3;
        }
        self.shortcutGraphics.Draw(0f, vector);
        Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4((0f - vector.x - 0.5f + self.CamPos(self.currentCameraPosition).x) / self.sSize.x, (0f - vector.y + 0.5f + self.CamPos(self.currentCameraPosition).y) / self.sSize.y, (0f - vector.x - 0.5f + self.levelGraphic.width + self.CamPos(self.currentCameraPosition).x) / self.sSize.x, (0f - vector.y + 0.5f + self.levelGraphic.height + self.CamPos(self.currentCameraPosition).y) / self.sSize.y));
        Shader.SetGlobalVector(RainWorld.ShadPropCamInRoomRect, new Vector4(vector.x / self.room.PixelWidth, vector.y / self.room.PixelHeight, self.sSize.x / self.room.PixelWidth, self.sSize.y / self.room.PixelHeight));
        Shader.SetGlobalVector(RainWorld.ShadPropScreenSize, self.sSize);
        if (!self.room.abstractRoom.gate && !self.room.abstractRoom.shelter)
        {
            float num3 = 0f;
            if (self.room.waterObject != null)
            {
                num3 = self.room.waterObject.fWaterLevel + 100f;
            }
            else if (self.room.deathFallGraphic != null)
            {
                num3 = self.room.deathFallGraphic.height + (ModManager.MMF ? 80f : 180f);
            }
            Shader.SetGlobalFloat(RainWorld.ShadPropWaterLevel, Mathf.InverseLerp(self.sSize.y, 0f, num3 - vector.y));
        }
        else
        {
            Shader.SetGlobalFloat(RainWorld.ShadPropWaterLevel, 0f);
        }
        float num4 = 1f;
        if (self.room.roomSettings.DangerType != RoomRain.DangerType.None)
        {
            num4 = self.room.world.rainCycle.ShaderLight;
        }
        if (self.room.lightning != null)
        {
            if (!self.room.lightning.bkgOnly)
            {
                num4 = self.room.lightning.CurrentLightLevel(timeStacker);
            }
            self.paletteTexture.SetPixel(0, 7, self.room.lightning.CurrentBackgroundColor(timeStacker, self.currentPalette));
            self.paletteTexture.SetPixel(1, 7, self.room.lightning.CurrentFogColor(timeStacker, self.currentPalette));
            self.paletteTexture.Apply();
        }
        if (self.room.roomSettings.Clouds == 0f)
        {
            Shader.SetGlobalFloat(RainWorld.ShadPropLight1, 1f);
        }
        else
        {
            Shader.SetGlobalFloat(RainWorld.ShadPropLight1, Mathf.Lerp(Mathf.Lerp(num4, -1f, self.room.roomSettings.Clouds), -0.4f, self.ghostMode));
        }
        Shader.SetGlobalFloat(RainWorld.ShadPropDarkness, 1f - self.effect_darkness);
        Shader.SetGlobalFloat(RainWorld.ShadPropBrightness, self.effect_brightness);
        Shader.SetGlobalFloat(RainWorld.ShadPropContrast, 1f + self.effect_contrast * 2f);
        Shader.SetGlobalFloat(RainWorld.ShadPropSaturation, 1f - self.effect_desaturation);
        Shader.SetGlobalFloat(RainWorld.ShadPropHue, 360f * self.effect_hue);
        Shader.SetGlobalFloat(RainWorld.ShadPropCloudsSpeed, 1f + 3f * self.ghostMode);
        if (self.lightBloomAlphaEffect != RoomSettings.RoomEffect.Type.None)
        {
            self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(self.lightBloomAlphaEffect);
        }
        if (self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.VoidMelt && self.fullScreenEffect != null)
        {
            if (self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidSea) > 0f)
            {
                self.lightBloomAlpha *= self.voidSeaGoldFilter;
                self.fullScreenEffect.color = new Color(Mathf.InverseLerp(-1200f, -6000f, vector.y) * Mathf.InverseLerp(0.9f, 0f, self.screenShake), 0f, 0f);
                self.fullScreenEffect.isVisible = self.lightBloomAlpha > 0f;
            }
            else
            {
                self.fullScreenEffect.color = new Color(0f, 0f, 0f);
            }
        }
        if (self.fullScreenEffect != null)
        {
            if (self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Lightning)
            {
                self.fullScreenEffect.alpha = Mathf.InverseLerp(0f, 0.2f, self.lightBloomAlpha) * Mathf.InverseLerp(-0.7f, 0f, num4);
            }
            else if (self.lightBloomAlpha > 0f && (self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Bloom || self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyBloom || self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyAndLightBloom || self.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.LightBurn))
            {
                self.fullScreenEffect.alpha = self.lightBloomAlpha * Mathf.InverseLerp(-0.7f, 0f, num4);
            }
            else
            {
                self.fullScreenEffect.alpha = self.lightBloomAlpha;
            }
        }
        if (self.sofBlackFade > 0f && !self.voidSeaMode)
        {
            Shader.SetGlobalFloat(RainWorld.ShadPropDarkness, 1f - self.sofBlackFade);
        }

        jobHandle.Complete();
        leasers.Dispose();
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        // MachineConnector.SetRegisteredOI(PLUGIN_GUID, new FastcobOptions());
        // Logger.LogDebug("Options");
        // Logger.LogDebug("skipRender: " + FastcobOptions.skipRendering.Value);

        if (init) return;
        init = true;
        Logger.LogDebug("1Init");
    }
}

class SeedcobDrawSpriteParallel : MonoBehaviour
{
    struct DrawJob : Unity.Jobs.IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Fastcob.Plugin.NonNullLeaser> leasers;

        public float timeStacker;
        public Vector2 camPos;

        public void Execute(int index)
        {
            Fastcob.Plugin.NonNullLeaser leaser = leasers[index];
            drawSeedCob((SeedCob)leaser.sLeaser.drawableObject, leaser.sLeaser, leaser.rCam, timeStacker, camPos);
        }
    }

    public JobHandle Update(NativeArray<Fastcob.Plugin.NonNullLeaser> leasers, float timeStacker, RoomCamera rCam, Vector2 camPos)
    {
        var job = new DrawJob()
        {
            timeStacker = timeStacker,
            camPos = camPos,
            leasers = leasers
        };
        return job.Schedule(leasers.Length, 4);
    }

    private static void drawSeedCob(SeedCob cob, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {

        PhysicalObject physicalCob = cob as PhysicalObject;
        if (ModManager.MSC && cob.freezingCounter > 0f)
        {
            cob.FreezingPaletteUpdate(sLeaser, rCam);
        }
        Vector2 vector = Vector2.Lerp(physicalCob.firstChunk.lastPos, physicalCob.firstChunk.pos, timeStacker);
        Vector2 vector2 = Vector2.Lerp(physicalCob.bodyChunks[1].lastPos, physicalCob.bodyChunks[1].pos, timeStacker);
        float num = 0.5f;
        Vector2 vector3 = cob.rootPos;
        for (int i = 0; i < cob.stalkSegments; i++)
        {
            float f = (float)i / (float)(cob.stalkSegments - 1);
            Vector2 vector4 = Custom.Bezier(cob.rootPos, cob.rootPos + cob.rootDir * Vector2.Distance(cob.rootPos, cob.placedPos) * 0.2f, vector2, vector2 + Custom.DirVec(vector, vector2) * Vector2.Distance(cob.rootPos, cob.placedPos) * 0.2f, f);
            Vector2 normalized = (vector3 - vector4).normalized;
            Vector2 vector5 = Custom.PerpendicularVector(normalized);
            float num2 = Vector2.Distance(vector3, vector4) / 5f;
            float num3 = Mathf.Lerp(cob.bodyChunkConnections[0].distance / 14f, 1.5f, Mathf.Pow(Mathf.Sin(Mathf.Pow(f, 2f) * (float)Mathf.PI), 0.5f));
            float num4 = 1f;
            Vector2 vector6 = default(Vector2);
            if (i + 2 >= cob.stalkSegments)
            {
                for (int j = 0; j < 2; j++)
                {
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4, vector3 - normalized * num2 - vector5 * (num3 + num) * 0.5f * num4 - camPos + vector6);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 1, vector3 - normalized * num2 + vector5 * (num3 + num) * 0.5f * num4 - camPos + vector6);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 2, vector4 + normalized * num2 - vector5 * num3 * num4 - camPos + vector6 + Vector2.up*25);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 3, vector4 + normalized * num2 + vector5 * num3 * num4 - camPos + vector6 + Vector2.up*20);
                    num4 = 0.35f;
                    vector6 += -rCam.room.lightAngle.normalized * num3 * 0.5f;
                }
            }
            else
            {
                for (int j = 0; j < 2; j++)
                {
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4, vector3 - normalized * num2 - vector5 * (num3 + num) * 0.5f * num4 - camPos + vector6);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 1, vector3 - normalized * num2 + vector5 * (num3 + num) * 0.5f * num4 - camPos + vector6);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 2, vector4 + normalized * num2 - vector5 * num3 * num4 - camPos + vector6);
                    (sLeaser.sprites[cob.StalkSprite(j)] as TriangleMesh).MoveVertice(i * 4 + 3, vector4 + normalized * num2 + vector5 * num3 * num4 - camPos + vector6);
                    num4 = 0.35f;
                    vector6 += -rCam.room.lightAngle.normalized * num3 * 0.5f;
                }
            }
            vector3 = vector4;
            num = num3;
        }
        vector3 = vector2 + Custom.DirVec(vector, vector2);
        num = 2f;
        for (int k = 0; k < cob.cobSegments; k++)
        {
            float t = (float)k / (float)(cob.cobSegments - 1);
            Vector2 vector7 = Vector2.Lerp(vector2, vector, t);
            Vector2 normalized2 = (vector3 - vector7).normalized;
            Vector2 vector8 = Custom.PerpendicularVector(normalized2);
            float num5 = Vector2.Distance(vector3, vector7) / 5f;
            float num6 = 2f;
            (sLeaser.sprites[cob.CobSprite] as TriangleMesh).MoveVertice(k * 4, vector3 - normalized2 * num5 - vector8 * (num6 + num) * 0.5f - camPos);
            (sLeaser.sprites[cob.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 1, vector3 - normalized2 * num5 + vector8 * (num6 + num) * 0.5f - camPos);
            (sLeaser.sprites[cob.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 2, vector7 + normalized2 * num5 - vector8 * num6 - camPos);
            (sLeaser.sprites[cob.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 3, vector7 + normalized2 * num5 + vector8 * num6 - camPos);
            vector3 = vector7;
            num = num6;
        }
        float num7 = Mathf.Lerp(cob.lastOpen, cob.open, timeStacker);
        for (int l = 0; l < 2; l++)
        {
            float num8 = -1f + (float)l * 2f;
            num = 2f;
            vector3 = vector + Custom.DirVec(vector2, vector) * 7f;
            float num9 = Custom.AimFromOneVectorToAnother(vector, vector2);
            Vector2 vector9 = vector;
            for (int m = 0; m < cob.cobSegments; m++)
            {
                float num10 = (float)m / (float)(cob.cobSegments - 1);
                vector9 += Custom.DegToVec(num9 + num8 * Mathf.Pow(num7, Mathf.Lerp(1f, 0.1f, num10)) * 50f * Mathf.Pow(num10, 0.5f)) * (Vector2.Distance(vector, vector2) * 1.1f + 8f) / cob.cobSegments;
                Vector2 normalized3 = (vector3 - vector9).normalized;
                Vector2 vector10 = Custom.PerpendicularVector(normalized3);
                float num11 = Vector2.Distance(vector3, vector9) / 5f;
                float num12 = Mathf.Lerp(2f, 6f, Mathf.Pow(Mathf.Sin(Mathf.Pow(num10, 0.5f) * (float)Mathf.PI), 0.5f));
                (sLeaser.sprites[cob.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4, vector3 - normalized3 * num11 - vector10 * (num12 + num) * 0.5f * (1 - l) - camPos);
                (sLeaser.sprites[cob.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 1, vector3 - normalized3 * num11 + vector10 * (num12 + num) * 0.5f * l - camPos);
                (sLeaser.sprites[cob.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 2, vector9 + normalized3 * num11 - vector10 * num12 * (1 - l) - camPos);
                (sLeaser.sprites[cob.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 3, vector9 + normalized3 * num11 + vector10 * num12 * l - camPos);
                vector3 = new Vector2(vector9.x, vector9.y);
                num = num12;
                num9 = Custom.VecToDeg(-normalized3);
            }
        }
        if (num7 > 0f)
        {
            Vector2 vector11 = Custom.DirVec(vector2, vector);
            Vector2 vector12 = Custom.PerpendicularVector(vector11);
            for (int n = 0; n < cob.seedPositions.Length; n++)
            {
                Vector2 vector13 = vector2 + vector11 * cob.seedPositions[n].y * (Vector2.Distance(vector2, vector) - 10f) + vector12 * cob.seedPositions[n].x * 3f;
                float num13 = 1f + Mathf.Sin((float)n / (float)(cob.seedPositions.Length - 1) * (float)Mathf.PI);
                if (cob.AbstractCob.dead)
                {
                    num13 *= 0.5f;
                }
                sLeaser.sprites[cob.SeedSprite(n, 0)].isVisible = true;
                sLeaser.sprites[cob.SeedSprite(n, 1)].isVisible = cob.seedsPopped[n];
                sLeaser.sprites[cob.SeedSprite(n, 2)].isVisible = true;
                sLeaser.sprites[cob.SeedSprite(n, 0)].scale = (cob.seedsPopped[n] ? num13 : 0.35f);
                sLeaser.sprites[cob.SeedSprite(n, 0)].x = vector13.x - camPos.x;
                sLeaser.sprites[cob.SeedSprite(n, 0)].y = vector13.y - camPos.y;
                Vector2 vector14 = default(Vector2);
                if (cob.seedsPopped[n])
                {
                    vector14 = vector12 * Mathf.Pow(Mathf.Abs(cob.seedPositions[n].x), Custom.LerpMap(num13, 1f, 2f, 1f, 0.5f)) * Mathf.Sign(cob.seedPositions[n].x) * 3.5f * num13;
                    if (!cob.AbstractCob.dead)
                    {
                        sLeaser.sprites[cob.SeedSprite(n, 2)].element = Futile.atlasManager.GetElementWithName("tinyStar");
                    }
                    sLeaser.sprites[cob.SeedSprite(n, 2)].rotation = Custom.VecToDeg(vector11);
                    sLeaser.sprites[cob.SeedSprite(n, 2)].scaleX = Mathf.Pow(1f - Mathf.Abs(cob.seedPositions[n].x), 0.2f);
                }
                sLeaser.sprites[cob.SeedSprite(n, 1)].x = vector13.x + vector14.x * 0.35f - camPos.x;
                sLeaser.sprites[cob.SeedSprite(n, 1)].y = vector13.y + vector14.y * 0.35f - camPos.y;
                sLeaser.sprites[cob.SeedSprite(n, 1)].scale = (cob.seedsPopped[n] ? num13 : 0.4f) * 0.5f;
                sLeaser.sprites[cob.SeedSprite(n, 2)].x = vector13.x + vector14.x - camPos.x;
                sLeaser.sprites[cob.SeedSprite(n, 2)].y = vector13.y + vector14.y - camPos.y;
            }
        }
        for (int num14 = 0; num14 < cob.leaves.GetLength(0); num14++)
        {
            Vector2 vector15 = Vector2.Lerp(cob.leaves[num14, 1], cob.leaves[num14, 0], timeStacker);
            sLeaser.sprites[cob.LeafSprite(num14)].x = vector2.x - camPos.x;
            sLeaser.sprites[cob.LeafSprite(num14)].y = vector2.y - camPos.y;
            sLeaser.sprites[cob.LeafSprite(num14)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector15);
            sLeaser.sprites[cob.LeafSprite(num14)].scaleY = Vector2.Distance(vector2, vector15) / 26f;
        }
        if (physicalCob.slatedForDeletetion || cob.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }

    }
}
