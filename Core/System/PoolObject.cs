using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.System
{
   /// <summary>
   /// Base representation of an object stored in an object pool.
   /// </summary>
   public abstract partial class PoolObject : Node2D
   {
      #region Properties

      /// <summary>
      /// Is this pool object available for use?
      /// </summary>
      public bool IsAvailable { get; set; } = true;

      /// <summary>
      /// Indicates if the pool object is marked for recycling.
      /// </summary>
      public bool MarkedForRecycling { get; set; } = false;

      #endregion

      #region Default Implementation

      public virtual void MarkUnavailable()
      {
         DebugLogger.Instance.Log($"{Name} unavailable.", LogLevel.Trace);

         IsAvailable = false;
         SetProcess(true);
      }

      #endregion

      #region Abstract Methods

      /// <summary>
      /// Makes the object unavailable and resets everything back to default in preparation for next use.
      /// </summary>
      public virtual void Recycle()
      {
         IsAvailable = true;
         SetProcess(false);
      }

      #endregion
   }
}
