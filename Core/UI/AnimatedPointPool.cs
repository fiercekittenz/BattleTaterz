using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.System;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BattleTaterz.Core.UI
{
   /// <summary>
   /// Manages a pool of available labels that will represent animated/floating points.
   /// </summary>
   public partial class AnimatedPointPool : BaseObjectPool
   {
      #region Properties

      /// <summary>
      /// The number of objects managed by the pool.
      /// </summary>
      [Export]
      public override int PoolSize { get; protected set; } = 20;

      #endregion

      #region Public Methods

      /// <summary>
      /// Grabs an available node from the pool and plays the animation.
      /// </summary>
      /// <param name="globalPosition"></param>
      /// <param name="scoreUpdate"></param>
      public void Play(Godot.Vector2 globalPosition, ScoreUpdateResults scoreUpdate)
      {
         AnimatedPoint animatedPoint = Yoink() as AnimatedPoint;

         // Display the point node.
         animatedPoint.Animate(globalPosition, scoreUpdate);
      }

      #endregion

      #region Inherited Methods

      protected override PoolObject CreatePoolObject()
      {
         AnimatedPoint animatedPoint = new AnimatedPoint();
         animatedPoint.Name = $"AnimatedPoint{Guid.NewGuid}";

         return animatedPoint;
      }

      protected override void ConfigurePulledObject(PoolObject poolObject)
      {
         // Not needed for this class.
      }

      #endregion
   }
}
