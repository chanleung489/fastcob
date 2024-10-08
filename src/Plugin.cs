using BepInEx;
using Expedition;
using MoreSlugcats;
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
    bool moveCamera = false;
    bool cobMoving = false;
    // bool traced = false;
    const int one_second = 40;
    int timer;
    int renderTries;
    int updateTries;
    // int drawCobCalls;
    // int drawCobTotal;

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        // On.RainWorldGame.Update += OnGameUpdate;
        On.RoomCamera.SpriteLeaser.Update += OnSpriteLeaserUpdate;
        // On.RoomCamera.MoveCamera2 += OnMoveCamera;
        // On.SeedCob.Update += OnSeedcobUpdate;
        // On.Room.AddObject += OnAddObject;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI(PLUGIN_GUID, new FastcobOptions());

        Logger.LogDebug("Options");
        Logger.LogDebug("skipRender: " + FastcobOptions.skipRendering.Value);
        Logger.LogDebug("skipUpdate: " + FastcobOptions.skipUpdate.Value);
        Logger.LogDebug("screenTransitionCheck: " + FastcobOptions.screenTransitionCheck.Value);
        Logger.LogDebug("movementCheck: " + FastcobOptions.movementCheck.Value);

        if (init) return;
        init = true;
        Logger.LogDebug("Init");
    }

    void OnGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        // if (!moveCamera) return;
        timer++;
        if (timer > 10)
        {

            // drawCobTotal += drawCobCalls;
            // Logger.LogDebug(drawCobTotal + " " + drawCobCalls);
            // drawCobCalls = 0;

            // UnityEngine.Debug.Log("movement");
            // UnityEngine.Debug.Log(FastcobOptions.movementCheck.Value && cobMoving);

            moveCamera = false;
            timer = 0;
        }
    }

    void OnMoveCamera(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera self, string roomName, int camPos)
    {
        moveCamera = true;
        timer = 0;
        orig(self, roomName, camPos);
    }

    void altSeedcobUpdate(SeedCob self, bool eu)
    {
        PhysicalObject seedcobObject = self as PhysicalObject;
        seedcobObject.Update(eu);
        if ((ModManager.MSC && self.room != null && self.room.roomSettings.DangerType == MoreSlugcatsEnums.RoomRainDangerType.Blizzard) || self.freezingCounter > 0f)
        {
            self.freezingCounter = Mathf.InverseLerp(self.AbstractCob.world.rainCycle.cycleLength, self.AllPlantsFrozenCycleTime, self.AbstractCob.world.rainCycle.timer);
            if (!self.AbstractCob.opened && self.freezingCounter >= 0.5f && UnityEngine.Random.value < 0.005f)
            {
                self.room.PlaySound(SoundID.Snail_Warning_Click, seedcobObject.bodyChunks[0], loop: false, 0.4f, UnityEngine.Random.Range(1.4f, 1.9f));
                seedcobObject.bodyChunks[0].vel += Custom.RNV() * self.freezingCounter;
            }
            if (!self.AbstractCob.opened && self.freezingCounter >= 1f && UnityEngine.Random.value < 0.002f)
            {
                self.spawnUtilityFoods();
                self.room.PlaySound(SoundID.Seed_Cob_Open, seedcobObject.firstChunk);
            }
        }
        seedcobObject.firstChunk.vel += (self.placedPos - seedcobObject.firstChunk.pos) / Custom.LerpMap(Vector2.Distance(self.placedPos, seedcobObject.firstChunk.pos), 5f, 100f, 2000f, 150f, 0.8f);
        seedcobObject.bodyChunks[1].vel += (self.placedPos + self.cobDir * self.bodyChunkConnections[0].distance - seedcobObject.bodyChunks[1].pos) / Custom.LerpMap(Vector2.Distance(self.placedPos + self.cobDir * self.bodyChunkConnections[0].distance, seedcobObject.bodyChunks[1].pos), 5f, 100f, 800f, 50f, 0.2f);
        if (!Custom.DistLess(seedcobObject.bodyChunks[1].pos, self.rootPos, self.stalkLength))
        {
            Vector2 vector = Custom.DirVec(seedcobObject.bodyChunks[1].pos, self.rootPos);
            float num = Vector2.Distance(seedcobObject.bodyChunks[1].pos, self.rootPos);
            seedcobObject.bodyChunks[1].pos += vector * (num - self.stalkLength) * 0.2f;
            seedcobObject.bodyChunks[1].vel += vector * (num - self.stalkLength) * 0.2f;
        }
        self.lastOpen = self.open;
        if (self.AbstractCob.opened)
        {
            self.open = Mathf.Lerp(self.open, 1f, Mathf.Lerp(0.01f, 0.0001f, self.open));
        }
        if (self.seedPopCounter > -1)
        {
            self.seedPopCounter--;
            if (self.seedPopCounter < 1)
            {
                for (int i = 0; i < self.seedsPopped.Length; i++)
                {
                    if (!self.seedsPopped[i])
                    {
                        self.seedsPopped[i] = true;
                        float num2 = (float)i / (float)(self.seedsPopped.Length - 1);
                        if (i == self.seedsPopped.Length - 1)
                        {
                            self.seedPopCounter = -1;
                        }
                        else
                        {
                            self.seedPopCounter = Mathf.RoundToInt(Mathf.Pow(1f - num2, 0.5f) * 20f * (0.5f + 0.5f * UnityEngine.Random.value));
                        }
                        Vector2 normalized = (Custom.PerpendicularVector(seedcobObject.bodyChunks[0].pos, seedcobObject.bodyChunks[1].pos) * self.seedPositions[i].x + Custom.RNV() * UnityEngine.Random.value).normalized;
                        seedcobObject.firstChunk.vel += normalized * 0.7f * self.seedPositions[i].y;
                        seedcobObject.firstChunk.pos += normalized * 0.7f * self.seedPositions[i].y;
                        seedcobObject.bodyChunks[1].vel += normalized * 0.7f * (1f - self.seedPositions[i].y);
                        seedcobObject.bodyChunks[1].pos += normalized * 0.7f * (1f - self.seedPositions[i].y);
                        Vector2 pos = Vector2.Lerp(seedcobObject.bodyChunks[1].pos, seedcobObject.bodyChunks[0].pos, self.seedPositions[i].y);
                        self.room.PlaySound(SoundID.Seed_Cob_Pop, pos);
                        self.room.AddObject(new WaterDrip(pos, (Vector2)Vector3.Slerp(Custom.PerpendicularVector(seedcobObject.bodyChunks[1].pos, seedcobObject.bodyChunks[0].pos) * self.seedPositions[i].x, Custom.DirVec(seedcobObject.bodyChunks[0].pos, seedcobObject.bodyChunks[1].pos), Mathf.Pow(num2, 2f) * 0.5f) * 11f + Custom.RNV() * 4f * UnityEngine.Random.value, waterColor: false));
                        break;
                    }
                }
            }
        }
        float num3 = Custom.AimFromOneVectorToAnother(seedcobObject.firstChunk.pos, seedcobObject.bodyChunks[1].pos);
        for (int j = 0; j < self.leaves.GetLength(0); j++)
        {
            self.leaves[j, 1] = self.leaves[j, 0];
            self.leaves[j, 0] += self.leaves[j, 2];
            self.leaves[j, 2] *= 0.9f;
            Vector2 vector2 = Custom.DirVec(self.leaves[j, 0], seedcobObject.bodyChunks[1].pos);
            float num4 = Vector2.Distance(self.leaves[j, 0], seedcobObject.bodyChunks[1].pos);
            self.leaves[j, 0] += vector2 * (num4 - self.leaves[j, 3].y);
            self.leaves[j, 2] += vector2 * (num4 - self.leaves[j, 3].y);
            self.leaves[j, 2] += Custom.DegToVec(num3 + Mathf.Lerp(-45f, 45f, (float)j / (float)(self.leaves.GetLength(0) - 1)));
        }
        if (self.delayedPush.HasValue)
        {
            if (self.pushDelay > 0)
            {
                self.pushDelay--;
            }
            else
            {
                seedcobObject.firstChunk.vel += self.delayedPush.Value;
                seedcobObject.bodyChunks[1].vel += self.delayedPush.Value;
                self.room.PlaySound(SoundID.Seed_Cob_Pick, seedcobObject.firstChunk.pos);
                self.delayedPush = null;
            }
        }
        if (self.AbstractCob.dead || !(self.open > 0.8f))
        {
            return;
        }
        for (int k = 0; k < (ModManager.MSC ? self.room.abstractRoom.creatures.Count : self.room.game.Players.Count); k++)
        {
            Player player;
            if (ModManager.MSC)
            {
                Creature realizedCreature = self.room.abstractRoom.creatures[k].realizedCreature;
                if (realizedCreature == null || !(realizedCreature is Player))
                {
                    continue;
                }
                player = realizedCreature as Player;
            }
            else
            {
                if (self.room.game.Players[k].realizedCreature == null)
                {
                    continue;
                }
                player = self.room.game.Players[k].realizedCreature as Player;
            }
            if (player.room != self.room || player.handOnExternalFoodSource.HasValue || player.eatExternalFoodSourceCounter >= 1 || player.dontEatExternalFoodSourceCounter >= 1 || player.FoodInStomach >= player.MaxFoodInStomach || (player.touchedNoInputCounter <= 5 && !player.input[0].pckp && (!ModManager.MSC || !(player.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC))) || (ModManager.MSC && !(player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear)) || player.FreeHand() <= -1)
            {
                continue;
            }
            Vector2 pos2 = player.mainBodyChunk.pos;
            Vector2 vector3 = Custom.ClosestPointOnLineSegment(seedcobObject.bodyChunks[0].pos, seedcobObject.bodyChunks[1].pos, pos2);
            if (Custom.DistLess(pos2, vector3, 25f))
            {
                player.handOnExternalFoodSource = vector3 + Custom.DirVec(pos2, vector3) * 5f;
                player.eatExternalFoodSourceCounter = 15;
                if (self.room.game.IsStorySession && player.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && self.room.game.GetStorySession.playerSessionRecords != null)
                {
                    self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(self);
                }
                self.delayedPush = Custom.DirVec(pos2, vector3) * 1.2f;
                self.pushDelay = 4;
                if (player.graphicsModule != null)
                {
                    (player.graphicsModule as PlayerGraphics).LookAtPoint(vector3, 100f);
                }
            }
        }
    }

    void OnSeedcobUpdate(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
    {
        updateTries++;
        if (updateTries < FastcobOptions.skipUpdate.Value) return;
        for (int i = 0; i < FastcobOptions.skipUpdate.Value; i++)
        {
            orig(self, eu);
        }
        updateTries = 0;
        cobMoving = self.bodyChunks[0].vel.x > 0.01f;
        if (cobMoving)
        {
            UnityEngine.Debug.Log("moving");
            UnityEngine.Debug.Log(cobMoving);
        }
    }

    void OnSpriteLeaserUpdate(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
    {

        RainWorld.CurrentlyDrawingObject = self.drawableObject;
        if (self.drawableObject is SeedCob)
        {
            renderTries++;
            if (renderTries < FastcobOptions.skipRendering.Value) return;
            renderTries = 0;
        }
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
}
