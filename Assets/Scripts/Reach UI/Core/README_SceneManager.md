# Scene Manager for Reach UI

The Scene Manager is a powerful scene management system built on the same architecture as the Panel Manager, providing seamless scene transitions with loading screens, animations, and event callbacks.

## Features

- **Scene Management**: Load scenes by name or index with smooth transitions
- **Loading Screens**: Customizable loading screens with animations
- **Event System**: Comprehensive event callbacks for scene changes
- **Button Integration**: Scene buttons that work with the UI system
- **Hotkey Support**: Keyboard and gamepad hotkey integration
- **Controller Support**: Full integration with the Controller Manager
- **Scene Data**: Pass custom data between scenes
- **Multiple Loading Modes**: Single and additive scene loading

## Setup

### 1. Add Scene Manager to Your Scene

1. Create an empty GameObject in your scene
2. Add the `SceneManager` component to it
3. Configure the scene list in the inspector

### 2. Configure Scene Items

Each scene item contains:
- **Scene Name**: Display name for the scene
- **Scene To Load**: Unity scene name (must be in Build Settings)
- **Scene Button**: Optional button component for UI navigation
- **First Selected**: UI element to select when scene loads
- **Custom Loading Screen**: Scene-specific loading screen
- **Scene Data**: Custom data to pass to the scene

### 3. Setup Loading Screen

1. Create a loading screen prefab with an Animator
2. Add "LoadingScreen_In" and "LoadingScreen_Out" animations
3. Assign the prefab to the Scene Manager's loading screen field

## Usage

### Basic Scene Loading

```csharp
// Get reference to Scene Manager
SceneManager sceneManager = FindObjectOfType<SceneManager>();

// Load scene by name
sceneManager.LoadScene("Main Menu");

// Load scene by index
sceneManager.LoadSceneByIndex(0);

// Load scene directly by Unity scene name
sceneManager.LoadSceneByName("GameScene");
```

### Navigation Methods

```csharp
// Navigate to next/previous scenes
sceneManager.NextScene();
sceneManager.PreviousScene();

// Reload current scene
sceneManager.ReloadCurrentScene();

// Load first scene
sceneManager.LoadFirstScene();

// Quit game
sceneManager.QuitGame();
```

### Event Callbacks

```csharp
// Subscribe to scene change events
sceneManager.onSceneChanged.AddListener((sceneIndex) => {
    Debug.Log($"Scene changed to index: {sceneIndex}");
});

sceneManager.onSceneLoadStart.AddListener(() => {
    Debug.Log("Scene loading started");
});

sceneManager.onSceneLoadComplete.AddListener(() => {
    Debug.Log("Scene loading completed");
});
```

### Scene Data

```csharp
// Set custom data for a scene
SceneItem sceneItem = sceneManager.GetSceneByName("GameScene");
sceneItem.sceneData.additionalData = "Level 1";
sceneItem.sceneData.resetProgress = true;
sceneItem.sceneData.customTransitionDuration = 2.0f;
```

## Scene Button Setup

### 1. Create Scene Button

1. Create a UI button GameObject
2. Add the `SceneButton` component
3. Configure the button appearance and behavior
4. Assign the button to a scene item in the Scene Manager

### 2. Button Configuration

- **Button Icon**: Sprite to display on the button
- **Button Text**: Text to display on the button
- **UI Navigation**: Configure gamepad/keyboard navigation
- **Localization**: Support for multiple languages
- **Sounds**: Audio feedback on interaction

## Loading Screen Configuration

### Animation Setup

1. Create an Animator Controller for your loading screen
2. Add "LoadingScreen_In" and "LoadingScreen_Out" animation clips
3. Set up transitions between the animations
4. The Scene Manager will automatically control the animations

### Custom Loading Screens

Each scene can have its own custom loading screen:
1. Create a loading screen prefab
2. Assign it to the scene item's "Custom Loading Screen" field
3. The Scene Manager will use this instead of the default loading screen

## Settings

### Scene Mode
- **Single**: Replace current scene (default)
- **Additive**: Load scene on top of current scene

### Update Mode
- **DeltaTime**: Use scaled time for transitions
- **UnscaledTime**: Use unscaled time for transitions

### Transition Speed
- Range: 0.75x to 2x
- Controls the speed of loading screen animations

### Options
- **Use Loading Screen**: Enable/disable loading screens
- **Initialize Buttons**: Automatically setup scene buttons
- **Use Cooldown For Hotkeys**: Prevent rapid hotkey presses
- **Bypass Loading On Enable**: Skip loading screen on scene start

## Integration with Controller Manager

The Scene Manager integrates with the Controller Manager for:
- Gamepad navigation support
- Hotkey management
- UI element selection
- Input device switching

## Example Scene Setup

```csharp
// Example scene configuration
SceneItem mainMenu = new SceneItem();
mainMenu.sceneName = "Main Menu";
mainMenu.sceneToLoad = "MainMenu";
mainMenu.firstSelected = mainMenuButton;
mainMenu.sceneData = new SceneData();

SceneItem gameScene = new SceneItem();
gameScene.sceneName = "Game Scene";
gameScene.sceneToLoad = "GameScene";
gameScene.customLoadingScreen = gameLoadingScreen;
gameScene.sceneData.additionalData = "Level 1";
```

## Best Practices

1. **Build Settings**: Ensure all scenes are added to Build Settings
2. **Scene Names**: Use consistent naming conventions
3. **Loading Screens**: Keep loading screens lightweight for fast transitions
4. **Event Cleanup**: Unsubscribe from events when components are destroyed
5. **Error Handling**: Check if scenes exist before loading
6. **Performance**: Use additive loading sparingly to manage memory

## Troubleshooting

### Common Issues

1. **Scene Not Found**: Check Build Settings and scene names
2. **Loading Screen Not Showing**: Verify Animator setup and animation names
3. **Buttons Not Working**: Ensure SceneButton components are properly configured
4. **Controller Issues**: Check Controller Manager integration

### Debug Tips

- Enable debug logging in the Scene Manager
- Check the console for error messages
- Verify scene names match exactly
- Test loading screens in isolation

## API Reference

### Public Methods

- `LoadScene(string sceneName)`: Load scene by display name
- `LoadSceneByIndex(int index)`: Load scene by index
- `LoadSceneByName(string sceneName)`: Load scene by Unity scene name
- `NextScene()`: Load next scene in list
- `PreviousScene()`: Load previous scene in list
- `ReloadCurrentScene()`: Reload current scene
- `QuitGame()`: Quit the application

### Public Properties

- `scenes`: List of scene items
- `currentSceneIndex`: Current scene index
- `useLoadingScreen`: Enable/disable loading screens
- `transitionSpeed`: Animation speed multiplier

### Events

- `onSceneChanged`: Called when scene changes
- `onSceneLoadStart`: Called when scene loading starts
- `onSceneLoadComplete`: Called when scene loading completes 