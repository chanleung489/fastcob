using BepInEx;
using Expedition;
using RWCustom;
using System;
using System.Collections.Generic;
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
    public const string PLUGIN_VERSION = "0.1.2";

    bool init;
    HashSet<float> seedcobsDrawn = new HashSet<float> ();

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        // On.SeedCob.DrawSprites += OnDrawSeedcob;
        On.RoomCamera.SpriteLeaser.Update += OnSpriteUpdate;
        On.RoomCamera.ApplyPositionChange += OnCameraMove;
        On.SeedCob.HitByWeapon += OnSeedcobHit;
    }

    private void OnSeedcobHit(On.SeedCob.orig_HitByWeapon orig, SeedCob self, Weapon weapon)
    {
        orig(self, weapon);
        seedcobsDrawn.Clear();
    }

    private void OnCameraMove(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        orig(self);
        seedcobsDrawn.Clear();
    }

    private void OnSpriteUpdate(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
    {
        if (self.drawableObject is SeedCob)
        {
            float seedcob_pos_x = (self.drawableObject as SeedCob).placedPos.x;
            if (!seedcobsDrawn.Contains(seedcob_pos_x))
            {
                seedcobsDrawn.Add(seedcob_pos_x);
            }
            else if (seedcobsDrawn.Contains(seedcob_pos_x))
            {
                return;
            }
        }
        RainWorld.CurrentlyDrawingObject = self.drawableObject;
        self.drawableObject.DrawSprites(self, rCam, timeStacker, camPos);
        if (self.drawableObject is CosmeticSprite)
        {
            (self.drawableObject as CosmeticSprite).PausedDrawSprites(self, rCam, timeStacker, camPos);
        }
        if (ModManager.Expedition && Custom.rainWorld.ExpeditionMode && ExpeditionGame.egg != null)
        {
            self.rbUpdate(timeStacker);
        }
    }

    private void OnDrawSeedcob(On.SeedCob.orig_DrawSprites orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        // orig(self, sLeaser, rCam, timeStacker, camPos);
        PhysicalObject basePhysicalObject = (self as PhysicalObject);

        if (ModManager.MSC && self.freezingCounter > 0f)
        {
            self.FreezingPaletteUpdate(sLeaser, rCam);
        }

        Vector2 vector = Vector2.Lerp(basePhysicalObject.firstChunk.lastPos, basePhysicalObject.firstChunk.pos, timeStacker);
        Vector2 vector2 = Vector2.Lerp(basePhysicalObject.bodyChunks[1].lastPos, basePhysicalObject.bodyChunks[1].pos, timeStacker);
        float num = 0.5f;
        Vector2 vector3 = self.rootPos;

        // stems

        for (int i = 0; i < self.stalkSegments; i++)
        {
            float num2 = (float)i / (float)(self.stalkSegments - 1);
            Vector2 vector4 = Custom.Bezier(self.rootPos, self.rootPos + self.rootDir * Vector2.Distance(self.rootPos, self.placedPos) * 0.2f, vector2, vector2 + Custom.DirVec(vector, vector2) * Vector2.Distance(self.rootPos, self.placedPos) * 0.2f, num2);
            Vector2 normalized = (vector3 - vector4).normalized;
            Vector2 vector5 = Custom.PerpendicularVector(normalized);
            float num3 = Vector2.Distance(vector3, vector4) / 5f;
            float num4 = Mathf.Lerp(self.bodyChunkConnections[0].distance / 14f, 1.5f, Mathf.Pow(Mathf.Sin(Mathf.Pow(num2, 2f) * 3.1415927f), 0.5f));
            float num5 = 1f;
            Vector2 vector6 = default(Vector2);
            for (int j = 0; j < 2; j++)
            {
                TriangleMesh mesh1 = (sLeaser.sprites[self.StalkSprite(j)] as TriangleMesh);
                mesh1.MoveVertice(i * 4, vector3 - normalized * num3 - vector5 * (num4 + num) * 0.5f * num5 - camPos + vector6);
                mesh1.MoveVertice(i * 4 + 1, vector3 - normalized * num3 + vector5 * (num4 + num) * 0.5f * num5 - camPos + vector6);
                mesh1.MoveVertice(i * 4 + 2, vector4 + normalized * num3 - vector5 * num4 * num5 - camPos + vector6);
                mesh1.MoveVertice(i * 4 + 3, vector4 + normalized * num3 + vector5 * num4 * num5 - camPos + vector6);
                num5 = 0.35f;
                vector6 += -rCam.room.lightAngle.normalized * num4 * 0.5f;
            }
            vector3 = vector4;
            num = num4;
        }

        vector3 = vector2 + Custom.DirVec(vector, vector2);
        num = 2f;

        // cob body box

        for (int k = 0; k < self.cobSegments; k++)
        {
            float num6 = (float)k / (float)(self.cobSegments - 1);
            Vector2 vector7 = Vector2.Lerp(vector2, vector, num6);
            Vector2 normalized2 = (vector3 - vector7).normalized;
            Vector2 vector8 = Custom.PerpendicularVector(normalized2);
            float num7 = Vector2.Distance(vector3, vector7) / 5f;
            float num8 = 2f;
            (sLeaser.sprites[self.CobSprite] as TriangleMesh).MoveVertice(k * 4, vector3 - normalized2 * num7 - vector8 * (num8 + num) * 0.5f - camPos);
            (sLeaser.sprites[self.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 1, vector3 - normalized2 * num7 + vector8 * (num8 + num) * 0.5f - camPos);
            (sLeaser.sprites[self.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 2, vector7 + normalized2 * num7 - vector8 * num8 - camPos);
            (sLeaser.sprites[self.CobSprite] as TriangleMesh).MoveVertice(k * 4 + 3, vector7 + normalized2 * num7 + vector8 * num8 - camPos);
            vector3 = vector7;
            num = num8;
        }
        float num9 = Mathf.Lerp(self.lastOpen, self.open, timeStacker);

        // cob body leaves

        for (int l = 0; l < 2; l++)
        {
            float num10 = -1f + (float)l * 2f;
            num = 2f;
            vector3 = vector + Custom.DirVec(vector2, vector) * 7f;
            float num11 = Custom.AimFromOneVectorToAnother(vector, vector2);
            Vector2 vector9 = vector;
            for (int m = 0; m < self.cobSegments; m++)
            {
                float num12 = (float)m / (float)(self.cobSegments - 1);
                vector9 += Custom.DegToVec(num11 + num10 * Mathf.Pow(num9, Mathf.Lerp(1f, 0.1f, num12)) * 50f * Mathf.Pow(num12, 0.5f)) * (Vector2.Distance(vector, vector2) * 1.1f + 8f) / (float)self.cobSegments;
                Vector2 normalized3 = (vector3 - vector9).normalized;
                Vector2 vector10 = Custom.PerpendicularVector(normalized3);
                float num13 = Vector2.Distance(vector3, vector9) / 5f;
                float num14 = Mathf.Lerp(2f, 6f, Mathf.Pow(Mathf.Sin(Mathf.Pow(num12, 0.5f) * 3.1415927f), 0.5f));
                (sLeaser.sprites[self.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4, vector3 - normalized3 * num13 - vector10 * (num14 + num) * 0.5f * (float)(1 - l) - camPos);
                (sLeaser.sprites[self.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 1, vector3 - normalized3 * num13 + vector10 * (num14 + num) * 0.5f * (float)l - camPos);
                (sLeaser.sprites[self.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 2, vector9 + normalized3 * num13 - vector10 * num14 * (float)(1 - l) - camPos);
                (sLeaser.sprites[self.ShellSprite(l)] as TriangleMesh).MoveVertice(m * 4 + 3, vector9 + normalized3 * num13 + vector10 * num14 * (float)l - camPos);
                vector3 = new Vector2(vector9.x, vector9.y);
                num = num14;
                num11 = Custom.VecToDeg(-normalized3);
            }
        }

        // cob fruits

        if (num9 > 0f)
        {
            Vector2 vector11 = Custom.DirVec(vector2, vector);
            Vector2 vector12 = Custom.PerpendicularVector(vector11);
            for (int n = 0; n < self.seedPositions.Length; n++)
            {
                Vector2 vector13 = vector2 + vector11 * self.seedPositions[n].y * (Vector2.Distance(vector2, vector) - 10f) + vector12 * self.seedPositions[n].x * 3f;
                float num15 = 1f + Mathf.Sin((float)n / (float)(self.seedPositions.Length - 1) * 3.1415927f);
                if (self.AbstractCob.dead)
                {
                    num15 *= 0.5f;
                }
                sLeaser.sprites[self.SeedSprite(n, 0)].isVisible = true;
                sLeaser.sprites[self.SeedSprite(n, 1)].isVisible = self.seedsPopped[n];
                sLeaser.sprites[self.SeedSprite(n, 2)].isVisible = true;
                sLeaser.sprites[self.SeedSprite(n, 0)].scale = (self.seedsPopped[n] ? num15 : 0.35f);
                sLeaser.sprites[self.SeedSprite(n, 0)].x = vector13.x - camPos.x;
                sLeaser.sprites[self.SeedSprite(n, 0)].y = vector13.y - camPos.y;
                Vector2 vector14 = default(Vector2);
                if (self.seedsPopped[n])
                {
                    vector14 = vector12 * Mathf.Pow(Mathf.Abs(self.seedPositions[n].x), Custom.LerpMap(num15, 1f, 2f, 1f, 0.5f)) * Mathf.Sign(self.seedPositions[n].x) * 3.5f * num15;
                    if (!self.AbstractCob.dead)
                    {
                        sLeaser.sprites[self.SeedSprite(n, 2)].element = Futile.atlasManager.GetElementWithName("tinyStar");
                    }
                    sLeaser.sprites[self.SeedSprite(n, 2)].rotation = Custom.VecToDeg(vector11);
                    sLeaser.sprites[self.SeedSprite(n, 2)].scaleX = Mathf.Pow(1f - Mathf.Abs(self.seedPositions[n].x), 0.2f);
                }
                sLeaser.sprites[self.SeedSprite(n, 1)].x = vector13.x + vector14.x * 0.35f - camPos.x;
                sLeaser.sprites[self.SeedSprite(n, 1)].y = vector13.y + vector14.y * 0.35f - camPos.y;
                sLeaser.sprites[self.SeedSprite(n, 1)].scale = (self.seedsPopped[n] ? num15 : 0.4f) * 0.5f;
                sLeaser.sprites[self.SeedSprite(n, 2)].x = vector13.x + vector14.x - camPos.x;
                sLeaser.sprites[self.SeedSprite(n, 2)].y = vector13.y + vector14.y - camPos.y;
            }
        }

        // stem leaves

        for (int num16 = 0; num16 < self.leaves.GetLength(0); num16++)
        {
            Vector2 vector15 = Vector2.Lerp(self.leaves[num16, 1], self.leaves[num16, 0], timeStacker);
            sLeaser.sprites[self.LeafSprite(num16)].x = vector2.x - camPos.x;
            sLeaser.sprites[self.LeafSprite(num16)].y = vector2.y - camPos.y;
            sLeaser.sprites[self.LeafSprite(num16)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector15);
            sLeaser.sprites[self.LeafSprite(num16)].scaleY = Vector2.Distance(vector2, vector15) / 26f;
        }
        if (basePhysicalObject.slatedForDeletetion || self.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }

    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI(PLUGIN_GUID, new FastcobOptions());
        Logger.LogDebug("Options");

        if (init) return;
        init = true;
        Logger.LogDebug("Init");
    }
}
