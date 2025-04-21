using BattleTaterz.Core.Gameplay;
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
   public class AnimatedPointsManager
   {
      #region Public Methods

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="parent"></param>
      /// <param name="poolSize"></param>
      public AnimatedPointsManager(Godot.Node parent, int poolSize)
      {
         // Create the animated point pool.
         // A pool of objects is used to cycle through, because it is expensive to
         // create, attach, detach, and destroy individual nodes per frame.
         for (int i = 0; i < poolSize; ++i)
         {
            AnimatedPoint animatedPoint = new AnimatedPoint();
            animatedPoint.Name = $"AnimatedPoint{i}";
            parent.AddChild(animatedPoint);

            _animatedPointPool.Add(animatedPoint);
         }
      }

      /// <summary>
      /// Grabs an available node from the pool and plays the animation.
      /// </summary>
      /// <param name="globalPosition"></param>
      /// <param name="scoreUpdate"></param>
      public void Play(Godot.Vector2 globalPosition, ScoreUpdateResults scoreUpdate)
      {
         //TODO - handle losses

         // Spin until a point node is available for the request.
         AnimatedPoint pointNode = null;
         while (pointNode == null)
         {
            pointNode = _animatedPointPool.Where(p => p.IsAvailable).FirstOrDefault();
            Task.Delay(TimeSpan.FromMilliseconds(1)).Wait(); // just so it doesn't choke the CPU
         }

         // Display the point node.
         pointNode.Animate(globalPosition, scoreUpdate);
      }

      #endregion

      #region Private Members

      /// <summary>
      /// A list of AnimatedPoint objects that can be reused for displaying animated point gains or losses.
      /// </summary>
      private List<AnimatedPoint> _animatedPointPool = new List<AnimatedPoint>();

      #endregion
   }
}
