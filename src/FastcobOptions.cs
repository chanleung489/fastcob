using Menu.Remix.MixedUI;
using UnityEngine;

namespace Fastcob;

sealed class FastcobOptions : OptionInterface
{
    public static Configurable<int> skipRendering;
    public static Configurable<int> skipUpdate;
    public static Configurable<bool> screenTransitionCheck;
    public static Configurable<bool> movementCheck;

    public FastcobOptions()
    {

        skipRendering = this.config.Bind<int>(
            key: "skipRendering",
            defaultValue: 4,
            info: new ConfigurableInfo(
                description: "Only execute the sprite-drawing function every other # attempts",
                acceptable: new ConfigAcceptableRange<int>(1, 128)
            )
        );
        
        skipUpdate = this.config.Bind<int>(
            key: "skipUpdate",
            defaultValue: 4,
            info: new ConfigurableInfo(
                description: "Only execute the update function every other # attempts",
                acceptable: new ConfigAcceptableRange<int>(1, 128)
            )
        );
        
        screenTransitionCheck = this.config.Bind<bool>(
            key: "screenTransitionCheck",
            defaultValue: false,
            info: new ConfigurableInfo("Do not skip when the camera moves")
        );

        movementCheck = this.config.Bind<bool>(
            key: "movementCheck",
            defaultValue: false,
            info: new ConfigurableInfo("Do not skip when seedcobs are moving")
        );

    }

    public override void Initialize()
    {
        base.Initialize();

        float x = 20;
        float y = 600;

        Tabs = new OpTab[] { new OpTab(this) };

        UIelement[] uielements = new UIelement[]
        {
            new OpLabel(x, y -= 40, "Fastcob Options", true),
            new OpLabel(x + 10, y -= 30, "(Please disable and re-enable the mod for changes to take place)", false),

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Skip Rendering", FLabelAlignment.Left),
            new OpSlider(skipRendering, new Vector2(x + 110, y - 6), 400)
            {
                description = skipRendering.info.description
            },

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Skip Update", FLabelAlignment.Left),
            new OpSlider(skipUpdate, new Vector2(x + 110, y - 6), 400)
            {
                description = skipUpdate.info.description
            },

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Screen Transition Check", FLabelAlignment.Left),
            new OpCheckBox(screenTransitionCheck, new Vector2(x + 150, y - 4))
            {
                description = screenTransitionCheck.info.description
            },

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Movement Check", FLabelAlignment.Left),
            new OpCheckBox(movementCheck, new Vector2(x + 150, y - 4))
            {
                description = movementCheck.info.description
            },

        };

        Tabs[0].AddItems(uielements);

    }
}

