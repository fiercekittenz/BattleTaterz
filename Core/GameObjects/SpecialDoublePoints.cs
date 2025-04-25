using Godot;
using System;

//TODO - later when I figure out the whole VFX/particle/shader thing, we'll do this.
public partial class SpecialDoublePoints : Line2D
{
   public override void _Ready()
   {
      //ShaderMaterial shaderMaterial = GetChild<Material>(0) as ShaderMaterial;
      //if (shaderMaterial != null)
      //{
      //   var tween = GetTree().CreateTween();
      //   tween.SetProcessMode(Tween.TweenProcessMode.Physics);
      //   tween.SetParallel(true);

      //   // Tween up.
      //   tween.TweenMethod(Callable.From<float>(t => shaderMaterial.SetShaderParameter("GlowValue", t)), 0f, 0.5f, 1f);

      //   // Tween down.
      //   tween.TweenMethod(Callable.From<float>(t => shaderMaterial.SetShaderParameter("GlowValue", t)), 0.5f, 0.0f, 1f);

      //   //tween.tween_method(func(t): $vhs.material.set_shader_parameter("noiseIntensity", t), 0, noise_intensity, duration)
      //   tween.SetLoops();
      //}
   }
}
