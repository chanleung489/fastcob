using Menu.Remix.MixedUI;
using UnityEngine;

namespace Fastcob;

sealed class FastcobOptions : OptionInterface
{
    // public static Configurable<int> skipRendering;
    public static Configurable<bool> LQStalks;

    public FastcobOptions()
    {
        // skipRendering = this.config.Bind<int>(
        //     key: "skiprendering",
        //     defaultValue: 4,
        //     info: new ConfigurableInfo(
        //         description: "Only execute the sprite-drawing function every other # attempts",
        //         acceptable: new ConfigAcceptableRange<int>(1, 128)
        //     )
        // );
        
        LQStalks = this.config.Bind<bool>(
            key: "lqstalks",
            defaultValue: false,
            info: new ConfigurableInfo("Replace the tip of seedcob stalks with a simple mesh to reduce mesh count")
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

            // new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Skip Rendering", FLabelAlignment.Left),
            // new OpSlider(skipRendering, new Vector2(x + 110, y - 6), 400)
            // {
            //     description = skipRendering.info.description
            // },

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Low Quality Stalks", FLabelAlignment.Left),
            new OpCheckBox(LQStalks, new Vector2(x + 150, y - 4))
            {
                description = LQStalks.info.description
            },

        };

        Tabs[0].AddItems(uielements);

    }
}

