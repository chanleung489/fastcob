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
        On.RainWorldGame.Update += OnGameUpdate;
        On.SeedCob.DrawSprites += OnDrawCob;
        On.RoomCamera.SpriteLeaser.Update += OnSpriteLeaserUpdate;
        // On.Room.AddObject += OnAddObject;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI(PLUGIN_GUID, new FastcobOptions());
        Logger.LogDebug("Options");
        Logger.LogDebug("skipRender: " + FastcobOptions.skipRendering.Value);
        Logger.LogDebug("altRender: " + FastcobOptions.alternativeRendering.Value);

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

    void OnDrawCob(On.SeedCob.orig_DrawSprites orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        drawCobCalls++;
        if (FastcobOptions.alternativeRendering.Value)
        {

            if (ModManager.MSC && self.freezingCounter > 0f)
            {
                self.FreezingPaletteUpdate(sLeaser, rCam);
            }
            Vector2 vector = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.bodyChunks[1].lastPos, self.bodyChunks[1].pos, timeStacker);
            float num = 0.5f;
            Vector2 vector3 = self.rootPos;
            for (int i = 0; i < self.stalkSegments; i++)
            {
                float f = (float)i / (float)(self.stalkSegments - 1);
                Vector2 vector4 = Custom.Bezier(self.rootPos, self.rootPos + self.rootDir * Vector2.Distance(self.rootPos, self.placedPos) * 0.2f, vector2, vector2 + Custom.DirVec(vector, vector2) * Vector2.Distance(self.rootPos, self.placedPos) * 0.2f, f);
                Vector2 normalized = (vector3 - vector4).normalized;
                Vector2 vector5 = Custom.PerpendicularVector(normalized);
                float num2 = Vector2.Distance(vector3, vector4) / 5f;
                float num3 = Mathf.Lerp(self.bodyChunkConnections[0].distance / 14f, 1.5f, Mathf.Pow(Mathf.Sin(Mathf.Pow(f, 2f) * Mathf.PI), 0.5f));
                float num4 = 1f;
                Vector2 vector6 = default(Vector2);

                TriangleMesh tmpMesh = sLeaser.sprites[self.StalkSprite(0)] as TriangleMesh;
                Vector2 tmpVec1 = normalized * num2;
                Vector2 tmpVec2 = camPos + vector6;
                Vector2 tmpVec3 = vector3 - tmpVec1 - vector5 * (num3 + num) * 0.5f * num4;
                Vector2 tmpVec4 = vector3 - tmpVec1 + vector5 * (num3 + num) * 0.5f * num4;
                Vector2 tmpVec5 = vector4 + tmpVec1 - vector5 * num3 * num4;
                Vector2 tmpVec6 = vector4 + tmpVec1 + vector5 * num3 * num4;
                tmpMesh.MoveVertice(i * 4, tmpVec3 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 1, tmpVec4 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 2, tmpVec5 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 3, tmpVec6 - tmpVec2);
                num4 = 0.35f;
                vector6 += -rCam.room.lightAngle.normalized * num3 * 0.5f;

                tmpMesh = sLeaser.sprites[self.StalkSprite(1)] as TriangleMesh;
                tmpVec2 = camPos + vector6;
                tmpMesh.MoveVertice(i * 4, tmpVec3 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 1, tmpVec4 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 2, tmpVec5 - tmpVec2);
                tmpMesh.MoveVertice(i * 4 + 3, tmpVec6 - tmpVec2);

                vector3 = vector4;
                num = num3;
            }
            vector3 = vector2 + Custom.DirVec(vector, vector2);
            num = 2f;
            for (int k = 0; k < self.cobSegments; k++)
            {
                float t = (float)k / (float)(self.cobSegments - 1);
                Vector2 vector7 = Vector2.Lerp(vector2, vector, t);
                Vector2 normalized2 = (vector3 - vector7).normalized;
                Vector2 vector8 = Custom.PerpendicularVector(normalized2);
                float num5 = Vector2.Distance(vector3, vector7) / 5f;
                float num6 = 2f;
                TriangleMesh tmpMesh = sLeaser.sprites[self.CobSprite] as TriangleMesh;
                tmpMesh.MoveVertice(k * 4, vector3 - normalized2 * num5 - vector8 * 2f - camPos);
                tmpMesh.MoveVertice(k * 4 + 1, vector3 - normalized2 * num5 + vector8 * 2f - camPos);
                tmpMesh.MoveVertice(k * 4 + 2, vector7 + normalized2 * num5 - vector8 * 2f - camPos);
                tmpMesh.MoveVertice(k * 4 + 3, vector7 + normalized2 * num5 + vector8 * 2f - camPos);
                vector3 = vector7;
                num = num6;
            }
            float num7 = Mathf.Lerp(self.lastOpen, self.open, timeStacker);

            // float num8 = -1f + (float)0 * 2f;
            num = 2f;
            vector3 = vector + Custom.DirVec(vector2, vector) * 7f;
            float num9 = Custom.AimFromOneVectorToAnother(vector, vector2);
            Vector2 vector9 = vector;
            TriangleMesh tmpMesh1 = sLeaser.sprites[self.ShellSprite(0)] as TriangleMesh;
            for (int m = 0; m < self.cobSegments; m++)
            {
                float num10 = (float)m / (float)(self.cobSegments - 1);
                vector9 += Custom.DegToVec(num9 + -1f * Mathf.Pow(num7, Mathf.Lerp(1f, 0.1f, num10)) * 50f * Mathf.Pow(num10, 0.5f)) * (Vector2.Distance(vector, vector2) * 1.1f + 8f) / self.cobSegments;
                Vector2 normalized3 = (vector3 - vector9).normalized;
                Vector2 vector10 = Custom.PerpendicularVector(normalized3);
                float num11 = Vector2.Distance(vector3, vector9) / 5f;
                float num12 = Mathf.Lerp(2f, 6f, Mathf.Pow(Mathf.Sin(Mathf.Pow(num10, 0.5f) * Mathf.PI), 0.5f));
                tmpMesh1.MoveVertice(m * 4, vector3 - normalized3 * num11 - vector10 * (num12 + num) * 0.5f - camPos);
                tmpMesh1.MoveVertice(m * 4 + 1, vector3 - normalized3 * num11 - camPos);
                tmpMesh1.MoveVertice(m * 4 + 2, vector9 + normalized3 * num11 - vector10 * num12 - camPos);
                tmpMesh1.MoveVertice(m * 4 + 3, vector9 + normalized3 * num11 + - camPos);
                vector3 = new Vector2(vector9.x, vector9.y);
                num = num12;
                num9 = Custom.VecToDeg(-normalized3);
            }

            // num8 = -1f + (float)1 * 2f;
            num = 2f;
            vector3 = vector + Custom.DirVec(vector2, vector) * 7f;
            num9 = Custom.AimFromOneVectorToAnother(vector, vector2);
            vector9 = vector;
            tmpMesh1 = sLeaser.sprites[self.ShellSprite(1)] as TriangleMesh;
            for (int m = 0; m < self.cobSegments; m++)
            {
                float num10 = (float)m / (float)(self.cobSegments - 1);
                vector9 += Custom.DegToVec(num9 + Mathf.Pow(num7, Mathf.Lerp(1f, 0.1f, num10)) * 50f * Mathf.Pow(num10, 0.5f)) * (Vector2.Distance(vector, vector2) * 1.1f + 8f) / self.cobSegments;
                Vector2 normalized3 = (vector3 - vector9).normalized;
                Vector2 vector10 = Custom.PerpendicularVector(normalized3);
                float num11 = Vector2.Distance(vector3, vector9) / 5f;
                float num12 = Mathf.Lerp(2f, 6f, Mathf.Pow(Mathf.Sin(Mathf.Pow(num10, 0.5f) * Mathf.PI), 0.5f));
                tmpMesh1.MoveVertice(m * 4, vector3 - normalized3 * num11 - camPos);
                tmpMesh1.MoveVertice(m * 4 + 1, vector3 - normalized3 * num11 + vector10 * (num12 + num) * 0.5f - camPos);
                tmpMesh1.MoveVertice(m * 4 + 2, vector9 + normalized3 * num11 - camPos);
                tmpMesh1.MoveVertice(m * 4 + 3, vector9 + normalized3 * num11 + vector10 * num12 - camPos);
                vector3 = new Vector2(vector9.x, vector9.y);
                num = num12;
                num9 = Custom.VecToDeg(-normalized3);
            }

            if (num7 > 0f)
            {
                Vector2 vector11 = Custom.DirVec(vector2, vector);
                Vector2 vector12 = Custom.PerpendicularVector(vector11);
                for (int n = 0; n < self.seedPositions.Length; n++)
                {
                    Vector2 vector13 = vector2 + vector11 * self.seedPositions[n].y * (Vector2.Distance(vector2, vector) - 10f) + vector12 * self.seedPositions[n].x * 3f;
                    float num13 = 1f + Mathf.Sin((float)n / (float)(self.seedPositions.Length - 1) * Mathf.PI);
                    if (self.AbstractCob.dead)
                    {
                        num13 *= 0.5f;
                    }
                    FSprite tmpSprite0 = sLeaser.sprites[self.SeedSprite(n, 0)];
                    FSprite tmpSprite1 = sLeaser.sprites[self.SeedSprite(n, 1)];
                    FSprite tmpSprite2 = sLeaser.sprites[self.SeedSprite(n, 2)];
                    tmpSprite0.isVisible = true;
                    tmpSprite1.isVisible = self.seedsPopped[n];
                    tmpSprite2.isVisible = true;
                    tmpSprite0.scale = (self.seedsPopped[n] ? num13 : 0.35f);
                    tmpSprite0.x = vector13.x - camPos.x;
                    tmpSprite0.y = vector13.y - camPos.y;
                    Vector2 vector14 = default(Vector2);
                    if (self.seedsPopped[n])
                    {
                        vector14 = vector12 * Mathf.Pow(Mathf.Abs(self.seedPositions[n].x), Custom.LerpMap(num13, 1f, 2f, 1f, 0.5f)) * Mathf.Sign(self.seedPositions[n].x) * 3.5f * num13;
                        if (!self.AbstractCob.dead)
                        {
                            tmpSprite2.element = Futile.atlasManager.GetElementWithName("tinyStar");
                        }
                        tmpSprite2.rotation = Custom.VecToDeg(vector11);
                        tmpSprite2.scaleX = Mathf.Pow(1f - Mathf.Abs(self.seedPositions[n].x), 0.2f);
                    }
                    tmpSprite1.x = vector13.x + vector14.x * 0.35f - camPos.x;
                    tmpSprite1.y = vector13.y + vector14.y * 0.35f - camPos.y;
                    tmpSprite1.scale = (self.seedsPopped[n] ? num13 : 0.4f) * 0.5f;
                    tmpSprite2.x = vector13.x + vector14.x - camPos.x;
                    tmpSprite2.y = vector13.y + vector14.y - camPos.y;
                }
            }
            for (int num14 = 0; num14 < self.leaves.GetLength(0); num14++)
            {
                Vector2 vector15 = Vector2.Lerp(self.leaves[num14, 1], self.leaves[num14, 0], timeStacker);
                FSprite tmpSprite4 = sLeaser.sprites[self.LeafSprite(num14)];
                tmpSprite4.x = vector2.x - camPos.x;
                tmpSprite4.y = vector2.y - camPos.y;
                tmpSprite4.rotation = Custom.AimFromOneVectorToAnother(vector2, vector15);
                tmpSprite4.scaleY = Vector2.Distance(vector2, vector15) / 26f;
            }
            if (self.slatedForDeletetion || self.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }


            return;
        }
        orig(self, sLeaser, rCam, timeStacker, camPos);
        // if (traced) return;
        // traced = true;
        // Logger.LogDebug("Draw cob");
        // System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace(true);
        // Logger.LogDebug(t.ToString());
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
