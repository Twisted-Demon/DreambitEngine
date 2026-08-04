using Dreambit.ECS;
using Dreambit.Examples.Particles;
using Dreambit.Examples.Pong;
using Dreambit.Examples.SpaceGame;
using Dreambit.UI;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.UiExample;

public class UiExampleScene : Scene<UiExampleScene>
{
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    protected override void OnInitialize()
    {
        InitializeSettings();

        var menu = CreateEntity("menu")
            .AttachComponent<UiFrame>()
            .WithLayout("Ui/menu.xml");

        // Runtime components are detached until added to a container. Their
        // prefixed IDs then become available through the destination layout.
        var runtimeBadge = menu.CreateComponent(
            "Ui/Components/runtime-badge.xml",
            "runtime.");
        menu.Layout
            .GetRequired<UiContainer>("runtime-component-host")
            .AddChild(runtimeBadge);

        menu.Layout
            .GetRequired<UiButton>("navigation.play-particles-button")
            .Clicked += OnPlayParticlesClicked;
        menu.Layout
            .GetRequired<UiButton>("navigation.play-pong-button")
            .Clicked += OnPlayPongClicked;
        menu.Layout
            .GetRequired<UiButton>("navigation.play-spacegame-button")
            .Clicked += OnPlaySpaceGameClicked;
        menu.Layout
            .GetRequired<UiButton>("navigation.play-basicscene-button")
            .Clicked += OnPlayBasicSceneClicked;
        WireControlGallery(menu.Layout);
    }

    private static void WireControlGallery(UiLayout layout)
    {
        var slider = layout.GetRequired<UiSlider>("volume-slider");
        var progress = layout.GetRequired<UiProgressBar>("volume-progress");
        var status = layout.GetRequired<UiText>("interaction-status");
        slider.ValueChanged += (_, value) =>
        {
            progress.Value = value;
            status.Text = $"Volume: {value:0}";
        };

        layout.GetRequired<UiListBox>("loadout-list").SelectionChanged +=
            (_, args) => status.Text = $"Loadout index: {args.NewIndex}";

        layout.GetRequired<UiComboBox>("resolution-combo").SelectionChanged +=
            (_, _, value) => status.Text = $"Resolution: {value}";
        
        layout.GetRequired<UiRadioButton>("difficulty-normal").CheckedChanged +=
            (_, args) =>
            {
                if (args)
                    status.Text = "Difficulty: Normal";
            };

        layout.GetRequired<UiRadioButton>("difficulty-hard").CheckedChanged +=
            (_, args) =>
            {
                if (args)
                    status.Text = "Difficulty: Hard";
            };

        var popup = layout.GetRequired<UiPopup>("demo-popup");
        layout.GetRequired<UiButton>("open-popup-button").Clicked +=
            _ => popup.Open();
        layout.GetRequired<UiButton>("popup.close-button").Clicked +=
            _ => popup.Close();

        var overlay = layout.GetRequired<UiOverlay>("demo-overlay");
        layout.GetRequired<UiButton>("show-overlay-button").Clicked += _ =>
        {
            overlay.IsVisible = true;
            layout.GetRequired<UiButton>("close-overlay-button").Focus();
        };
        layout.GetRequired<UiButton>("close-overlay-button").Clicked += _ =>
        {
            overlay.IsVisible = false;
            layout.GetRequired<UiButton>("show-overlay-button").Focus();
        };
    }


    private void InitializeSettings()
    {
        Window.SetSize(WindowWidth, WindowHeight);

        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(WindowHeight);

        UiCamera.PixelsPerUnit = 1;
        UiCamera.SetTargetVerticalResolution(WindowHeight);

        BackgroundColor = new Color(18, 20, 28);
    }

    private void OnPlayParticlesClicked(UiButton button)
    {
        Scene.SetNextScene<ParticlesScene>();
    }
    
    private void OnPlayBasicSceneClicked(UiButton buton)
    {
        SetNextScene<BasicScene>();
    }
    
    private void OnPlayPongClicked(UiButton button)
    {
        Scene.SetNextScene<PongScene>();
    }
    
    private void OnPlaySpaceGameClicked(UiButton button)
    {
        Scene.SetNextScene<SpaceGameScene>();
    }
}
