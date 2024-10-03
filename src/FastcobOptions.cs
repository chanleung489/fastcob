using Menu.Remix.MixedUI;
using UnityEngine;

namespace Fastcob;

sealed class FastcobOptions : OptionInterface
{
    public static Configurable<int> skipRendering;
    public static Configurable<bool> alternativeRendering;

    public FastcobOptions()
    {
        skipRendering = this.config.Bind<int>(
            key: "skiprendering",
            defaultValue: 4,
            info: new ConfigurableInfo(
                description: "Amount of SeedCob rendering calls to skip",
                acceptable: new ConfigAcceptableRange<int>(1, 128)
            )
        );
        alternativeRendering = this.config.Bind<bool>(
            key: "alternativeRendering",
            defaultValue: false,
            info: new ConfigurableInfo("Use an alternative rendering method for SeedCob")
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
            new OpLabel(x + 10, y -= 30, "(Please re-apply the mod after changing settings)", false),

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Skip Rendering", FLabelAlignment.Left),
            new OpSlider(skipRendering, new Vector2(x + 110, y - 6), 400)
            {
                description = skipRendering.info.description
            },

            new OpLabel(new Vector2(x + 10, y -= 30), Vector2.zero, "Alternative Rendering", FLabelAlignment.Left),
            new OpCheckBox(alternativeRendering, new Vector2(x + 150, y - 4))
            {
                description = alternativeRendering.info.description
            },

        };

        Tabs[0].AddItems(uielements);

    }
}

