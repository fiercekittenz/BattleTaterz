using Godot;
using System;

/// <summary>
/// Doing the "lord's" work and giving people the ability to adjust the volume.
/// </summary>
public partial class MasterVolumeSlider : HSlider
{
   #region Public Members and Properties

   [Export]
   public string AudioBusName { get; set; }

   public int AudioBusIndex { get; set; }

   #endregion

   #region Public Methods

   public override void _Ready()
   {
      AudioBusIndex = AudioServer.GetBusIndex(AudioBusName);
      ValueChanged += MasterVolumeSlider_ValueChanged;

      // For now, set the default volume to 25%
      Value = 0.2f;
   }

   #endregion

   #region Private Methods

   private void MasterVolumeSlider_ValueChanged(double value)
   {
      AudioServer.SetBusVolumeDb(AudioBusIndex, (float)Godot.Mathf.LinearToDb(value));
   }

   #endregion
}
