using BepInEx;
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
    bool traced = false;
    int drawCobTries;

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        // On.SeedCob.DrawSprites += OnDrawCob;
        On.Room.AddObject += OnAddObject;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (init) return;
        init = true;
        Logger.LogDebug("Init");
    }

    void OnDrawCob(On.SeedCob.orig_DrawSprites orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (traced) return;
        traced = true;
        Logger.LogDebug("Draw cob");
        System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace(true);
        Logger.LogDebug(t.ToString());
    }

    void OnAddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
    {
		if (self.game == null)
		{
			return;
		}
		if (obj is DeafLoopHolder)
		{
			for (int i = 0; i < self.updateList.Count; i++)
			{
				if (self.updateList[i] is DeafLoopHolder)
				{
					(self.updateList[i] as DeafLoopHolder).muted = true;
				}
			}
		}
		self.updateList.Add(obj);
		obj.room = self;
		IDrawable drawable = null;
		if (obj is IDrawable)
		{
			drawable = obj as IDrawable;
		}
		if (obj is LightSource)
		{
			LightSource lightSource = obj as LightSource;
			if (ModManager.MMF && lightSource.noGameplayImpact)
			{
				self.cosmeticLightSources.Add(lightSource);
			}
			else
			{
				self.lightSources.Add(lightSource);
			}
		}
		if (obj is IAccessibilityModifier)
		{
			self.accessModifiers.Add(obj as IAccessibilityModifier);
		}
		if (obj is VisionObscurer)
		{
			self.visionObscurers.Add(obj as VisionObscurer);
		}
		if (obj is ZapCoil)
		{
			self.zapCoils.Add(obj as ZapCoil);
		}
		if (obj is MoreSlugcats.LightningMachine)
		{
			self.lightningMachines.Add(obj as MoreSlugcats.LightningMachine);
		}
		if (obj is MoreSlugcats.EnergySwirl)
		{
			self.energySwirls.Add(obj as MoreSlugcats.EnergySwirl);
		}
		if (obj is MoreSlugcats.SnowSource)
		{
			self.snowSources.Add(obj as MoreSlugcats.SnowSource);
			self.AddSnow();
		}
		if (ModManager.MSC && obj is MoreSlugcats.OEsphere)
		{
			self.oeSpheres.Add(obj as MoreSlugcats.OEsphere);
		}
		if (obj is MoreSlugcats.CellDistortion)
		{
			self.cellDistortions.Add(obj as MoreSlugcats.CellDistortion);
		}
		if (obj is MoreSlugcats.LocalBlizzard)
		{
			self.localBlizzards.Add(obj as MoreSlugcats.LocalBlizzard);
		}
		if (ModManager.MSC && obj is MoreSlugcats.IProvideWarmth)
		{
			self.blizzardHeatSources.Add(obj as MoreSlugcats.IProvideWarmth);
		}
		if (obj is PhysicalObject)
		{
			self.physicalObjects[(obj as PhysicalObject).collisionLayer].Add(obj as PhysicalObject);
			if (obj is OracleSwarmer)
			{
				self.SwarmerCount++;
			}
            if (obj is SeedCob)
            {
                drawCobTries++;
                if (drawCobTries <= 128) return;
                drawCobTries = 0;
            }
			if ((obj as PhysicalObject).graphicsModule != null)
			{
				drawable = (obj as PhysicalObject).graphicsModule;
			}
			else if (self.BeingViewed)
			{
				(obj as PhysicalObject).InitiateGraphicsModule();
				if ((obj as PhysicalObject).graphicsModule != null)
				{
					drawable = (obj as PhysicalObject).graphicsModule;
				}
			}
		}
		if (drawable != null)
		{
			self.drawableObjects.Add(drawable);
			for (int j = 0; j < self.game.cameras.Length; j++)
			{
				if (self.game.cameras[j].room == self)
				{
					self.game.cameras[j].NewObjectInRoom(drawable);
				}
			}
		}
    }
}
