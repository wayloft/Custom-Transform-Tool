# Custom Transform Tool for Unity

The **Custom Transform Tool** is a Unity Editor extension that enhances your ability to manipulate objects in your scenes. This tool provides advanced features for snapping, aligning, and transforming objects, making it invaluable for level designers, game developers, and 3D artists.

## Features

- **Advanced Snapping**  
  Snap objects to a grid, vertices, or edges with precision. Configure snapping axes (X, Y, Z) and set custom grid sizes.

- **Effortless Alignment & Distribution**  
  Align objects along axes (Center, Min, Max) and distribute them evenly with a single click.

- **Parent/Child Management**  
  Reparent selected objects or center children to their parents for better hierarchy organization.

- **Transform Presets**  
  Save and load transform presets for reuse in future projects.

- **Transform History**  
  Keep a history of object transformations and revert to previous states when needed.

- **Customizable Gizmos**  
  Modify gizmo colors and sizes for better scene visualization.

## Installation

1. Download the script file: [CustomTransformTool.cs](./CustomTransformTool.cs).
2. Place the script in the `Editor` folder of your Unity project.  
   If the folder doesn't exist, create it.

## How to Use

1. Open the tool by navigating to `Tools > Custom Transform Tool` in the Unity Editor.
2. Use the intuitive UI to:
   - Adjust object position, rotation, and scale.
   - Enable and configure snapping options.
   - Save, load, and apply transform presets.
   - Manage parent/child relationships.
   - Align and distribute selected objects.
3. Customize gizmo appearance to suit your workflow.

## Key Functionalities

### Transform Settings
- **Position, Rotation, Scale**: Directly modify object transform values.
- **Snap Mode**: Choose from Grid, Vertex, or Edge snapping.
  - Grid snapping includes axis-based control (X, Y, Z) and adjustable grid sizes.

### Alignment & Distribution
- Align objects along axes with modes like Center, Min, or Max.
- Distribute objects evenly between the first and last selected objects.

### Presets & History
- Save frequently used transform settings as presets.
- Maintain a history of transforms for easy reversion.

### Gizmo Customization
- Configure gizmo colors and sizes for better visibility in the Scene view.

## Contribution
Feel free to contribute by submitting issues or pull requests. Suggestions for additional features are welcome!

## License
This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
