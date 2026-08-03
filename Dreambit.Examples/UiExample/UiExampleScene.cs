using Dreambit.ECS;
using Dreambit.Examples.Particles;
using Dreambit.Examples.Pong;
using Dreambit.Examples.SpaceGame;
using Dreambit.UI;

namespace Dreambit.Examples.UiExample;

public class UiExampleScene : Scene<UiExampleScene>
{
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    protected override void OnInitialize()
    {
        InitializeSettings();

        var menu = CreateEntity("menu")
            .AttachComponent<UiFrame>();

        menu.LayoutPath = "Ui/menu.xml";

        menu.Layout.GetRequired<UiButton>("play-particles-button").Clicked += OnPlayParticlesClicked;
        menu.Layout.GetRequired<UiButton>("play-pong-button").Clicked += OnPlayPongClicked;
        menu.Layout.GetRequired<UiButton>("play-spacegame-button").Clicked += OnPlaySpaceGameClicked;
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

        var popup = layout.GetRequired<UiPopup>("demo-popup");
        layout.GetRequired<UiButton>("open-popup-button").Clicked +=
            _ => popup.Open();
        layout.GetRequired<UiButton>("close-popup-button").Clicked +=
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
    }

    private void OnPlayParticlesClicked(UiButton button)
    {
        Scene.SetNextScene<ParticlesScene>();
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
